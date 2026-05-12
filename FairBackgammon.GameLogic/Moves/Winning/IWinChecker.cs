using FairBackgammon.GameLogic.Enums;
using FairBackgammon.GameLogic.Game;

namespace FairBackgammon.GameLogic.Moves.Winning
{
  public interface IWinChecker
  {
    int IsWinning(Board board, CheckerType player);
  }
}