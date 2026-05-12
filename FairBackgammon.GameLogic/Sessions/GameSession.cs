using FairBackgammon.GameLogic.BoardSetup;
using FairBackgammon.GameLogic.Enums;
using FairBackgammon.GameLogic.Game;
using FairBackgammon.GameLogic.Moves.Validation;
using FairBackgammon.GameLogic.Sessions.State;

namespace FairBackgammon.GameLogic.Sessions
{
  public class GameSession(IBoardSetup boardSetup, IMoveValidator moveValidator)
  {
    private readonly IMoveValidator moveValidator = moveValidator;
    private readonly Board _board = new(boardSetup);
    private readonly Dice _dice = new();
    private bool _playerRolled = false;

    public CheckerType CurrentPlayer { get; private set; } = CheckerType.White;
    public BoardState BoardState 
    {
      get => new()
      {
        Dice = _playerRolled ? [_dice.Current.Item1, _dice.Current.Item2] : [],
        Points = _board.Points.Select((p, i) => new PointState { Index = i, Count = p.Count, Type = p.CheckerType }),
        Bar = _board.Bar.Select(b => new HolderState { Count = b.Count, Type = b.CheckerType }),
        Off = _board.Bearoff.Select(b => new HolderState { Count = b.Count, Type = b.CheckerType }),
        CurrentPlayer = (int)CurrentPlayer,
        Winner = null // TODO: Implement winner logic
      };
    }

    public SessionState Roll()
    {
      if (_playerRolled)
      {
        throw new InvalidOperationException("Player has already rolled. Please end the turn before rolling again.");
      }

      var diceValue = _dice.Roll();
      _playerRolled = true;

      return new SessionState
      {
        Dice = diceValue,
        ValidMoves = moveValidator.LoadValidMoves(_board, diceValue, CurrentPlayer)
      };
    }

    public bool MakeMove((int, int)[] move)
    {
      if (!_playerRolled)
      {
        throw new InvalidOperationException("Player must roll before making a move.");
      }

      var isValidMove = moveValidator.IsMoveValid(move);
      if (!isValidMove) return false;

      var moveMade = _board.TryMakeMove(move, CurrentPlayer);
      if (!moveMade) return false;

      _playerRolled = false;
      CurrentPlayer = CurrentPlayer.Opponent();
      
      return true;
    }
  }
}