namespace FairBackgammon.GameLogic.BoardSetup
{
  public class CustomBoardSetup(IEnumerable<PointSetup> points) : IBoardSetup
  {
    private readonly IEnumerable<PointSetup> _points = points;

    public IEnumerable<PointSetup> Setup()
    {
      return _points;
    }
  }
}