namespace FairBackgammon.GameLogic.Game
{
  public class Dice
  {
    public (int, int) Current { get; private set; }

    public (int, int) Roll()
    {
      Current = (Random.Shared.Next(1, 7), Random.Shared.Next(1, 7));
      return Current;
    }
  }
}