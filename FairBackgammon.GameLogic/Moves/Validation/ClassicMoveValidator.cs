using FairBackgammon.GameLogic.Constants;
using FairBackgammon.GameLogic.Enums;
using FairBackgammon.GameLogic.Game;
using FairBackgammon.GameLogic.Moves.Simulation;

namespace FairBackgammon.GameLogic.Moves.Validation
{
  public class ClassicMoveValidator : IMoveValidator
  {
    private readonly HashSet<ulong> validMoves = [];

    public bool IsMoveValid((int, int)[] move)
    {
      var hash = HashMove(move);
      return validMoves.Contains(hash);
    }

    public List<(int, int)[]> LoadValidMoves(Board board, (int, int) dice, CheckerType player)
    {
      validMoves.Clear();
      var simulator = new BoardMoveSimulator(board);

      var moves = new List<int[]>();

      // Double moves
      if (dice.Item1 == dice.Item2)
      {
        moves.Add([dice.Item1, dice.Item1, dice.Item1, dice.Item1]);
      }
      else
      {
        moves.Add([dice.Item1, dice.Item2]);
        moves.Add([dice.Item2, dice.Item1]);
      }

      foreach (var move in moves)
      {
        foreach (var moveCombination in GenerateMoveCombinations(simulator, move, player))
        {
          validMoves.Add(HashMove([.. SortMoveSteps(moveCombination, player)]));
        }
      }

      if (validMoves.Count == 0)
      {
        if (dice.Item1 == dice.Item2)
        {
          var movesToTake = 4;
          var allMoves = new int[] { dice.Item1, dice.Item1, dice.Item1, dice.Item1 };
          do
          {
            --movesToTake;
            foreach (var moveCombination in GenerateMoveCombinations(simulator, [.. allMoves.Take(movesToTake)], player))
            {
              validMoves.Add(HashMove([.. SortMoveSteps(moveCombination, player)]));
            }

          } while (movesToTake > 0 && validMoves.Count == 0);

          return [.. validMoves.Select(UnhashMove)];
        }
        else
        {
          foreach (var move in new int[][] { [dice.Item1], [dice.Item2] })
          {
            foreach (var moveCombination in GenerateMoveCombinations(simulator, move, player))
            {
              validMoves.Add(HashMove([.. SortMoveSteps(moveCombination, player)]));
            }
          }

          return [.. validMoves.Select(UnhashMove)];
        }
      }

      return [.. validMoves.Select(UnhashMove)];
    }

    private static IEnumerable<IEnumerable<(int, int)>> GenerateMoveCombinations(BoardMoveSimulator simulator, int[] pointsToMove, CheckerType player)
    {
      if (pointsToMove.Length == 0)
      {
        yield break;
      }

      var firstPointMoves = GetPossibleMoves(simulator.Board, pointsToMove[0], player);
      foreach (var move in firstPointMoves)
      {
        var checkpoint = simulator.CreateCheckpoint();
        simulator.SimulateMove(move, player);

        if (pointsToMove.Length == 1)
        {
          yield return [move];
        }
        else
        {
          foreach (var subsequentMoves in GenerateMoveCombinations(simulator, [.. pointsToMove.Skip(1)], player))
          {
            yield return new[] { move }.Concat(subsequentMoves);
          }
        }

        simulator.UndoToCheckpoint(checkpoint);
      }
    }

    private static IEnumerable<(int, int)> GetPossibleMoves(Board board, int pointsToMove, CheckerType player)
    {
      var hasBarCheckers = board.Bar[(int)player].Count > 0;
      if (hasBarCheckers)
      {
        foreach (var barMove in GetPossibleMovesFromBar(board, pointsToMove, player))
        {
          yield return barMove;
        }

        // Bar moves are mandatory, so if we have any, we don't consider other moves
        yield break;
      }

      foreach (var bearoffMove in GetPossibleMovesToBearOff(board, pointsToMove, player))
      {
        yield return bearoffMove;
      }

      foreach (var onBoardMove in GetPossibleMovesOnBoard(board, pointsToMove, player))
      {
        yield return onBoardMove;
      }
    }

    private static IEnumerable<(int, int)> GetPossibleMovesFromBar(Board board, int pointsToMove, CheckerType player)
    {
      var entryPointIndex = player == CheckerType.White ?
        BoardConstants.TOTAL_POINTS - pointsToMove :
        pointsToMove - 1;

      if (board.Points[entryPointIndex].Count <= 1 || board.Points[entryPointIndex].CheckerType == player)
      {
        return [(BoardConstants.BAR_INDEX, entryPointIndex)];
      }

      return [];
    }

    private static IEnumerable<(int, int)> GetPossibleMovesToBearOff(Board board, int pointsToMove, CheckerType player)
    {
      var homeBoardStart = player == CheckerType.White ? 18 : 0;

      var checkersInHomeBoard = board.Points.Skip(homeBoardStart).Take(6)
        .Where(p => p.CheckerType == player).Sum(p => p.Count);

      if (checkersInHomeBoard + board.Bearoff[(int)player].Count < BoardConstants.MAX_CHECKERS_PER_POINT)
      {
        yield break; // Can't bear off if not all checkers are in the home board
      }

      var targetPointIndex = player == CheckerType.White ? pointsToMove - 1 : BoardConstants.TOTAL_POINTS - pointsToMove;
      if (board.Points[targetPointIndex].CheckerType == player && board.Points[targetPointIndex].Count > 0)
      {
        yield return (targetPointIndex, BoardConstants.BEAROFF_INDEX);
        yield break; // If we can bear off from the target point, we don't consider other bear off moves
      }

      var furthestCheckerPointIndex = player == CheckerType.White ?
        Array.FindLastIndex(board.Points, p => p.CheckerType == player && p.Count > 0) :
        Array.FindIndex(board.Points, homeBoardStart, p => p.CheckerType == player && p.Count > 0);

      if ((player == CheckerType.White && furthestCheckerPointIndex < targetPointIndex) ||
          (player == CheckerType.Black && furthestCheckerPointIndex > targetPointIndex))
      {
        yield return (furthestCheckerPointIndex, BoardConstants.BEAROFF_INDEX);
      }
    }

    private static IEnumerable<(int, int)> GetPossibleMovesOnBoard(Board board, int pointsToMove, CheckerType player)
    {
      var pointsWithPlayerCheckers = board.Points
        .Select((point, index) => (point, index))
        .Where(p => p.point.CheckerType == player && p.point.Count > 0);

      foreach (var (point, index) in pointsWithPlayerCheckers)
      {
        var targetPointIndex = player == CheckerType.White ?
          index - pointsToMove :
          index + pointsToMove;

        if (targetPointIndex < 0 || targetPointIndex >= BoardConstants.TOTAL_POINTS)
        {
          continue; // Can't move off the board
        }

        if (board.Points[targetPointIndex].Count <= 1 || board.Points[targetPointIndex].CheckerType == player)
        {
          yield return (index, targetPointIndex);
        }
      }
    }

    public static ulong HashMove((int, int)[] move)
    {
      if (move.Length > 4)
      {
        throw new ArgumentException("Move cannot have more than 4 steps.");
      }

      var hash = 0UL;
      for (int i = 0; i < move.Length; i++)
      {
        hash |= (ulong)(move[i].Item1 & 0x7F) << (i * 16);
        hash |= (ulong)(move[i].Item2 & 0x7F) << (i * 16 + 8);
      }

      return hash;
    }

    public static (int, int)[] UnhashMove(ulong hash)
    {
      var move = new List<(int, int)>();
      for (int i = 0; i < 4; i++)
      {
        var from = (int)((hash >> (i * 16)) & 0x7F);
        var to = (int)((hash >> (i * 16 + 8)) & 0x7F);
        if (from == 0 && to == 0)
        {
          break; // No more steps
        }
        move.Add((from, to));
      }

      return [.. move];
    }

    public static IEnumerable<(int, int)> SortMoveSteps(IEnumerable<(int, int)> move, CheckerType checkerType)
    {
      return checkerType switch
      {
        CheckerType.White => move.OrderBy(m => m.Item1).ThenBy(m => m.Item2),
        CheckerType.Black => move.OrderByDescending(m => m.Item1).ThenByDescending(m => m.Item2),
        _ => throw new ArgumentException("Invalid checker type."),
      };
    }
  }
}