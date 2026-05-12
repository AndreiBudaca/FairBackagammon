using FairBackgammon.GameLogic.Constants;
using FairBackgammon.GameLogic.Enums;
using FairBackgammon.GameLogic.Game;

namespace FairBackgammon.GameLogic.Moves.Winning
{
  public class ClassicWinChecker : IWinChecker
  {
    public int IsWinning(Board board, CheckerType player)
    {
      if (board.Bearoff[(int)player].Count < BoardConstants.MAX_CHECKERS_PER_POINT) return 0;

      // Openent checkers on the bar or home points -> backgammon
      if (board.Bar[(int)player.Opponent()].Count > 0) return 3;
      if (OpenentHasCheckersOnHomePoints(board, player)) return 3;

      if (board.Bearoff[(int)player.Opponent()].Count == 0) return 2;

      return 1;
    }

    protected static bool OpenentHasCheckersOnHomePoints(Board board, CheckerType player)
    {
      var homePoints = GetHomePoints(board, player);
      foreach (var point in homePoints)
      {
        if (point.Count > 0 && point.CheckerType == player.Opponent()) return true;
      }

      return false;
    } 

    protected static Span<CheckerHolder> GetHomePoints(Board board, CheckerType player)
    {
      return player == CheckerType.White ? board.Points.AsSpan(0, 6) : board.Points.AsSpan(18, 6);
    }
  }
}