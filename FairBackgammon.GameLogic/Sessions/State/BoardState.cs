using FairBackgammon.GameLogic.Enums;

namespace FairBackgammon.GameLogic.Sessions.State
{
  public class BoardState
  {
    public required IEnumerable<PointState> Points { get; set; }
    public required IEnumerable<HolderState> Bar { get; set; }
    public required IEnumerable<HolderState> Off { get; set; }
    public required int[] Dice { get; set; }
    public int CurrentPlayer { get; set; }
    public int? Winner { get; set; }
  }

  public class HolderState
  {
    public required int Count { get; set; }
    public required CheckerType Type { get; set; }
  }

  public class PointState : HolderState
  {
    public required int Index { get; set; }
  }
}