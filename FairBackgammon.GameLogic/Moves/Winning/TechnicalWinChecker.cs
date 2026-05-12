using FairBackgammon.GameLogic.Enums;
using FairBackgammon.GameLogic.Game;

namespace FairBackgammon.GameLogic.Moves.Winning
{
  public class TechnicalWinChecker : ClassicWinChecker, IWinChecker 
  {
    public new int IsWinning(Board board, CheckerType player)
    {
      var classicWin = base.IsWinning(board, player);
      if (classicWin != 0) return classicWin;
      
      if (TechnicalBigWin(board, player))
      {
        if (OpenentHasCheckersOnHomePoints(board, player)) return 3;
        return 2;
      }

      if (TechnicalSmallWin(board, player))
      {
        if (OpenentHasCheckersOnHomePoints(board, player)) return 3;
        return 1;
      }

      return 0;
    }

    private static bool TechnicalSmallWin(Board board, CheckerType player)
    {
      foreach (var point in GetHomePoints(board, player))
      {
        if (point.Count != 1 || point.CheckerType != player) return false;
      }

      return true;
    }

    private static bool TechnicalBigWin(Board board, CheckerType player)
    {
      foreach (var point in GetHomePoints(board, player))
      {
        if (point.Count != 2 || point.CheckerType != player) return false;
      }

      return true;
    }
  }
}