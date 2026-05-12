using FairBackgammon.GameLogic.BoardSetup;
using FairBackgammon.GameLogic.Moves.Validation;
using FairBackgammon.GameLogic.Moves.Winning;
using FairBackgammon.GameLogic.Sessions;

namespace FairBackgammon.GameLogic
{
  public static class Backgammon
  {
    public static GameSession StartNewGame(
      IBoardSetup? boardSetup = null,
      IMoveValidator? moveValidator = null,
      IWinChecker? winChecker = null)
    {
      return new GameSession(
        boardSetup ?? new ClassicBoardSetup(),
        moveValidator ?? new ClassicMoveValidator(),
        winChecker ?? new TechnicalWinChecker()
      );
    }
  }
}