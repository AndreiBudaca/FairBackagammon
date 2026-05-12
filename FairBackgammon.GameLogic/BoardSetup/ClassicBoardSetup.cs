namespace FairBackgammon.GameLogic.BoardSetup
{
  public class ClassicBoardSetup : IBoardSetup
  {
    public IEnumerable<PointSetup> Setup()
    {
      return 
      [
        new PointSetup { PointIndex = 24, InitialCheckers = 2, CheckerType = Enums.CheckerType.White },
        new PointSetup { PointIndex = 13, InitialCheckers = 5, CheckerType = Enums.CheckerType.White },
        new PointSetup { PointIndex = 8, InitialCheckers = 3, CheckerType = Enums.CheckerType.White },
        new PointSetup { PointIndex = 6, InitialCheckers = 5, CheckerType = Enums.CheckerType.White },
        
        new PointSetup { PointIndex = 1, InitialCheckers = 2, CheckerType = Enums.CheckerType.Black },
        new PointSetup { PointIndex = 12, InitialCheckers = 5, CheckerType = Enums.CheckerType.Black },
        new PointSetup { PointIndex = 17, InitialCheckers = 3, CheckerType = Enums.CheckerType.Black },
        new PointSetup { PointIndex = 19, InitialCheckers = 5, CheckerType = Enums.CheckerType.Black }
      ];
    }
  }
}