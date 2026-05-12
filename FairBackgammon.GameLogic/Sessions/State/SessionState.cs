using FairBackgammon.GameLogic.Enums;

namespace FairBackgammon.GameLogic.Sessions.State
{
  public class SessionState
  {
    public required (int, int) Dice { get; set; }
    public required IEnumerable<(int, int)[]> ValidMoves { get; set; }
  }
}