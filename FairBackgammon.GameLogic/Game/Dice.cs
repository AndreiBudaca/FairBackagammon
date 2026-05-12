namespace FairBackgammon.GameLogic.Game
{
  public class Dice
  {
    private readonly Random _random = new();

    public (int, int) Current { get; private set; }

    public (int, int) Roll()
    {
      Current = (_random.Next(1, 7), _random.Next(1, 7));
      return Current;
    }
  }
}