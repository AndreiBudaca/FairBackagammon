using FairBackgammon.GameLogic.Constants;
using FairBackgammon.GameLogic.Enums;
using FairBackgammon.GameLogic.Game;

namespace FairBackgammon.GameLogic.Moves.Simulation
{
  public class BoardMoveSimulator(Board board)
  {
    private readonly List<Action> _undoActions = [];
    private readonly Dictionary<Guid, int> _checkpoints = [];
    private int _movesMade = 0;

    public Board Board { get; } = board;

    public void SimulateMove((int from, int to) move, CheckerType player)
    {
      var fromHolder = HolderForIndex(move.from, player);
      var toHolder = HolderForIndex(move.to, player);

      var (checker, undoRemoveAction) = fromHolder.Remove();
      _undoActions.Insert(_movesMade, undoRemoveAction);
      ++_movesMade;

      if (checker.Type != player)
      {
        throw new InvalidOperationException("Cannot move a checker that does not belong to the current player.");
      }

      var (capturedChecker, undoAddAction) = toHolder.Add(checker);
      _undoActions.Insert(_movesMade, undoAddAction);
      ++_movesMade;

      if (capturedChecker != null)
      {
        var (_, undoBar) = Board.Bar[(int)capturedChecker.Type].Add(capturedChecker);
        _undoActions.Insert(_movesMade, undoBar);
        ++_movesMade;
      }
    }

    public Guid CreateCheckpoint()
    {
      var checkpointId = Guid.NewGuid();
      _checkpoints[checkpointId] = _movesMade;
      return checkpointId;
    }

    public void UndoToCheckpoint(Guid checkpointId)
    {
      if (!_checkpoints.ContainsKey(checkpointId))
      {
        throw new ArgumentException("Invalid checkpoint ID.");
      }

      var targetMoveCount = _checkpoints[checkpointId];
      for (int i = _movesMade - 1; i >= targetMoveCount; i--)
      {
        _undoActions[i]();
      }

      _movesMade = targetMoveCount;
      _checkpoints.Remove(checkpointId);
    }

    public void UndoAllMoves()
    {
      for (int i = _undoActions.Count - 1; i >= 0; i--)
      {
        _undoActions[i]();
      }

      _checkpoints.Clear();
      _undoActions.Clear();
      _movesMade = 0;
    }

    public void CommitMoves()
    {
      _checkpoints.Clear();
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
        return Board.Points[index];
      }
    }
  }
}