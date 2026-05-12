namespace FairBackgammon.GameLogic.Enums
{
  public enum CheckerType
  {
    White = 0,
    Black = 1
  }

  public static class CheckerTypeExtensions
  {
    public static CheckerType Opponent(this CheckerType checkerType)
    {
      return checkerType == CheckerType.White ? CheckerType.Black : CheckerType.White;
    }
  }
}