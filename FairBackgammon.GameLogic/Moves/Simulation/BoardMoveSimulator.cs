using FairBackgammon.GameLogic.Constants;
using FairBackgammon.GameLogic.Enums;
using FairBackgammon.GameLogic.Game;

namespace FairBackgammon.GameLogic.Moves.Simulation
{
  public class BoardMoveSimulator(Board board)
  {
    private readonly List<Action> _undoActions = [];
    private int _movesMade = 0;

    public Board Board { get; } = board;

    public void SimulateMove((int from, int to) move, CheckerType player)
    {
      var fromHolder = HolderForIndex(move.from, player);
      var toHolder = HolderForIndex(move.to, player);

      var (checker, undoRemoveAction) = fromHolder.Remove();
      _undoActions.Add(undoRemoveAction);
      ++_movesMade;

      if (checker.Type != player)
      {
        throw new InvalidOperationException("Cannot move a checker that does not belong to the current player.");
      }

      var (capturedChecker, undoAddAction) = toHolder.Add(checker);
      _undoActions.Add(undoAddAction);
      ++_movesMade;

      if (capturedChecker != null)
      {
        var (_, undoBar) = Board.Bar[(int)capturedChecker.Type].Add(capturedChecker);
        _undoActions.Add(undoBar);
        ++_movesMade;
      }
    }

    public int CreateCheckpoint() => _movesMade;

    public void UndoToCheckpoint(int checkpointMoveCount)
    {
      ArgumentOutOfRangeException.ThrowIfNegative(checkpointMoveCount);
      ArgumentOutOfRangeException.ThrowIfGreaterThan(checkpointMoveCount, _movesMade);

      for (int i = _movesMade - 1; i >= checkpointMoveCount; i--)
      {
        _undoActions[i]();
      }

      _movesMade = checkpointMoveCount;
      if (_undoActions.Count > checkpointMoveCount)
      {
        _undoActions.RemoveRange(checkpointMoveCount, _undoActions.Count - checkpointMoveCount);
      }
    }

    public void UndoAllMoves()
    {
      for (int i = _undoActions.Count - 1; i >= 0; i--)
      {
        _undoActions[i]();
      }

      _undoActions.Clear();
      _movesMade = 0;
    }

    public void CommitMoves()
    {
      _undoActions.Clear();
      _movesMade = 0;
    }

    private CheckerHolder HolderForIndex(int index, CheckerType player)
    {
      if (index <= BoardConstants.BAR_INDEX)
      {
        return Board.Bar[(int)player];
      }
      else if (index >= BoardConstants.BEAROFF_INDEX)
      {
        return Board.Bearoff[(int)player];
      }
      else
      {
        return Board.Points[index - 1];
      }
    }
  }
}