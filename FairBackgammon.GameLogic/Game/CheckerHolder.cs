using System.Net.Sockets;
using FairBackgammon.GameLogic.Constants;
using FairBackgammon.GameLogic.Enums;

namespace FairBackgammon.GameLogic.Game
{
  public class CheckerHolder
  {
    public Checker[] Checkers { get; private set; } = new Checker[BoardConstants.MAX_CHECKERS_PER_POINT];
    public CheckerType CheckerType { get; private set; }
    public int Count { get; private set; }

    public CheckerHolder(int intialCheckers, CheckerType type)
    {
      ArgumentOutOfRangeException.ThrowIfNegative(intialCheckers);
      ArgumentOutOfRangeException.ThrowIfGreaterThan(intialCheckers, BoardConstants.MAX_CHECKERS_PER_POINT);

      for (int i = 0; i < intialCheckers; i++)
      {
        Checkers[i] = new Checker { Type = type };
      }

      Count = intialCheckers;
      CheckerType = type;
    }

    public (Checker?, Action) Add(Checker checker)
    {
      Checker? capturedChecker = null;
      Action? undoCapture = null;

      if (Count >= BoardConstants.MAX_CHECKERS_PER_POINT)
      {
        throw new InvalidOperationException("Cannot add more checkers to this holder.");
      }

      if (Count > 1 && checker.Type != CheckerType)
      {
        throw new InvalidOperationException("Cannot add a checker of a different type to this holder.");
      }

      if (Count == 1 && checker.Type != CheckerType)
      {
        (capturedChecker, undoCapture) = Remove();
      }

      Checkers[Count] = checker;
      Count++;

      if (Count == 1)
      {
        CheckerType = checker.Type;
      }

      return (capturedChecker, () => {
        Remove();
        undoCapture?.Invoke();
      });
    }

    public (Checker, Action) Remove()
    {
      if (Count == 0)
      {
        throw new InvalidOperationException("No checkers to remove from this holder.");
      }

      var checker = Checkers[Count - 1];
      Count--;
      return (checker, () => Add(checker));
    }
  }
}