namespace FairBackgammon.GameLogic.BoardSetup
{
  public class ClassicBoardSetup : IBoardSetup
  {
    public IEnumerable<PointSetup> Setup()
    {
      return 
      [
        new PointSetup { PointIndex = 23, InitialCheckers = 2, CheckerType = Enums.CheckerType.White },
        new PointSetup { PointIndex = 12, InitialCheckers = 5, CheckerType = Enums.CheckerType.White },
        new PointSetup { PointIndex = 7, InitialCheckers = 3, CheckerType = Enums.CheckerType.White },
        new PointSetup { PointIndex = 5, InitialCheckers = 5, CheckerType = Enums.CheckerType.White },
        
        new PointSetup { PointIndex = 0, InitialCheckers = 2, CheckerType = Enums.CheckerType.Black },
        new PointSetup { PointIndex = 11, InitialCheckers = 5, CheckerType = Enums.CheckerType.Black },
        new PointSetup { PointIndex = 16, InitialCheckers = 3, CheckerType = Enums.CheckerType.Black },
        new PointSetup { PointIndex = 18, InitialCheckers = 5, CheckerType = Enums.CheckerType.Black }
      ];
    }
  }
}