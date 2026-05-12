using FairBackgammon.GameLogic.Constants;
using FairBackgammon.GameLogic.Enums;
using FairBackgammon.GameLogic.Game;
using FairBackgammon.GameLogic.Moves.Simulation;

namespace FairBackgammon.GameLogic.Moves.Validation
{
  public class ClassicMoveValidator : IMoveValidator
  {
    private readonly HashSet<ulong> validMoves = [];

    public bool IsMoveValid((int, int)[] move, CheckerType player)
    {
      var hash = HashMove([.. SortMoveSteps(move, player)]);
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

      var depth = 0;
      do
      {
        foreach (var move in moves)
        {
          foreach (var moveCombination in GenerateMoveCombinations(simulator, move, depth, player))
          {
            validMoves.Add(HashMove([.. SortMoveSteps(moveCombination, player)]));
          }
        }

        depth++;
      } while (depth < 4 && validMoves.Count == 0); // If we have valid moves, we don't need to consider shorter move combinations

      return [.. validMoves.Select(UnhashMove)];
    }

    private static IEnumerable<IEnumerable<(int, int)>> GenerateMoveCombinations(BoardMoveSimulator simulator, int[] pointsToMove, int depth, CheckerType player)
    {
      if (depth >= pointsToMove.Length)
      {
        yield break;
      }

      var firstPointMoves = GetPossibleMoves(simulator.Board, pointsToMove[depth], player);
      foreach (var move in firstPointMoves)
      {
        var checkpoint = simulator.CreateCheckpoint();
        try
        {
          simulator.SimulateMove(move, player);
        }
        catch
        {
          simulator.UndoToCheckpoint(checkpoint);
          continue; // Invalid move, skip to the next one
        }

        if (pointsToMove.Length == depth + 1)
        {
          yield return [move];
        }
        else
        {
          foreach (var subsequentMoves in GenerateMoveCombinations(simulator, pointsToMove, depth + 1, player))
          {
            yield return subsequentMoves.Append(move);
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
      
      foreach (var onBoardMove in GetPossibleMovesOnBoard(board, pointsToMove, player))
      {
        yield return onBoardMove;
      }

      foreach (var bearoffMove in GetPossibleMovesToBearOff(board, pointsToMove, player))
      {
        yield return bearoffMove;
      }
    }

    private static IEnumerable<(int, int)> GetPossibleMovesFromBar(Board board, int pointsToMove, CheckerType player)
    {
      var entryPointIndex = player == CheckerType.White ?
        BoardConstants.TOTAL_POINTS - pointsToMove + 1 :
        pointsToMove;

      if (board.Points[entryPointIndex - 1].Count <= 1 || board.Points[entryPointIndex - 1].CheckerType == player)
      {
        return [(BoardConstants.BAR_INDEX, entryPointIndex)];
      }

      return [];
    }

    private static IEnumerable<(int, int)> GetPossibleMovesToBearOff(Board board, int pointsToMove, CheckerType player)
    {
      var homeBoardStart = player == CheckerType.White ? 1 : BoardConstants.TOTAL_POINTS - 5;

      var checkersInHomeBoard = board.Points.Skip(homeBoardStart - 1).Take(6)
        .Where(p => p.CheckerType == player).Sum(p => p.Count);

      if (checkersInHomeBoard == 0 || checkersInHomeBoard + board.Bearoff[(int)player].Count < BoardConstants.MAX_CHECKERS_PER_POINT)
      {
        yield break; // Can't bear off if not all checkers are in the home board
      }

      var targetPointIndex = player == CheckerType.White ? pointsToMove : BoardConstants.TOTAL_POINTS - pointsToMove + 1;
      if (board.Points[targetPointIndex - 1].CheckerType == player && board.Points[targetPointIndex - 1].Count > 0)
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
        yield return (furthestCheckerPointIndex + 1, BoardConstants.BEAROFF_INDEX);
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
          yield return (index + 1, targetPointIndex + 1);
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
        CheckerType.White => move.OrderByDescending(m => SortPointValue(m.Item1, checkerType)).ThenByDescending(m => SortPointValue(m.Item2, checkerType)),
        CheckerType.Black => move.OrderBy(m => SortPointValue(m.Item1, checkerType)).ThenBy(m => SortPointValue(m.Item2, checkerType)),
        _ => throw new ArgumentException("Invalid checker type."),
      };
    }

    public static int SortPointValue(int point, CheckerType checkerType)
    {
      return point switch
      {
        BoardConstants.BAR_INDEX => checkerType == CheckerType.White ? int.MaxValue : int.MinValue,
        _ => point
      };
    }
  }
}