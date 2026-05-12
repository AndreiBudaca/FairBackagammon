using FairBackgammon.GameLogic.Enums;
using FairBackgammon.GameLogic.Game;

namespace FairBackgammon.GameLogic.Moves.Validation
{
  public interface IMoveValidator
  {
    List<(int, int)[]> LoadValidMoves(Board board, (int, int) dice, CheckerType player);
    bool IsMoveValid((int, int)[] move);
  }
}