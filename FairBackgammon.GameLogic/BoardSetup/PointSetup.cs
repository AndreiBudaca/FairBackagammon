using FairBackgammon.GameLogic.Enums;

namespace FairBackgammon.GameLogic.BoardSetup
{
  public class PointSetup
  {
    public required int PointIndex { get; set; }
    public required int InitialCheckers { get; set; }
    public required CheckerType CheckerType { get; set; }
  }
}