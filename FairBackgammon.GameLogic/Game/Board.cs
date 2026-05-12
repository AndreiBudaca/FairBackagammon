using FairBackgammon.GameLogic.BoardSetup;
using FairBackgammon.GameLogic.Constants;
using FairBackgammon.GameLogic.Enums;
using FairBackgammon.GameLogic.Moves.Simulation;

namespace FairBackgammon.GameLogic.Game
{
  public class Board
  {
    private readonly BoardMoveSimulator _moveSimulator;

    public CheckerHolder[] Points { get; private set; } = new CheckerHolder[BoardConstants.TOTAL_POINTS];
    public CheckerHolder[] Bar { get; private set; } = [new CheckerHolder(0, CheckerType.White), new CheckerHolder(0, CheckerType.Black)];
    public CheckerHolder[] Bearoff { get; private set; } = [new CheckerHolder(0, CheckerType.White), new CheckerHolder(0, CheckerType.Black)];

    public Board(IBoardSetup boardSetup)
    {
      _moveSimulator = new BoardMoveSimulator(this);

      for (var i = 0; i < Points.Length; i++)
      {
        Points[i] = new CheckerHolder(0, CheckerType.White);
      }

      foreach (var pointSetup in boardSetup.Setup())
      {
        ArgumentOutOfRangeException.ThrowIfNegative(pointSetup.PointIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pointSetup.PointIndex, BoardConstants.TOTAL_POINTS);

        Points[pointSetup.PointIndex - 1] = new CheckerHolder(pointSetup.InitialCheckers, pointSetup.CheckerType);
      }
    }

    public bool TryMakeMove((int, int)[] move, CheckerType player)
    {
      foreach (var (from, to) in move)
      {
        try
        {
          _moveSimulator.SimulateMove((from, to), player);
        }
        catch
        {
          _moveSimulator.UndoAllMoves();
          return false;
        }
      }

      _moveSimulator.CommitMoves();
      return true;
    }
  }
}