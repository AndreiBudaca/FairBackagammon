namespace FairBackgammon.GameLogic.BoardSetup
{
  public interface IBoardSetup
  {
    IEnumerable<PointSetup> Setup();
  }
}