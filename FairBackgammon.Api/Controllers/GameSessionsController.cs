using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using FairBackgammon.Api.Connectors;
using FairBackgammon.GameLogic;
using FairBackgammon.GameLogic.BoardSetup;
using FairBackgammon.GameLogic.Enums;
using FairBackgammon.GameLogic.Sessions;
using FairBackgammon.GameLogic.Sessions.State;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairBackgammon.Api.Controllers
{
  [ApiController]
  [Route("api/game-sessions")]
  [Authorize]
  public sealed class GameSessionsController : ControllerBase
  {
    [HttpPost]
    public ActionResult CreateGameSession([FromBody] List<PointState> initialBoard)
    {
      var gameId = Guid.NewGuid().ToString();
      initialBoard ??= [];

      var gameCreated = GameConnector.ActiveGames.TryAdd(gameId, new ActiveGame
      {
        Players = User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).ToDictionary(c => c.Value, c => CheckerType.White),
        Score = User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).ToDictionary(c => c.Value, c => 0),
        InitialBoard = initialBoard,
      });

      if (!gameCreated)
      {
        return StatusCode(500, "Failed to create game session.");
      }

      return Ok(new { id = gameId });
    }

    [HttpPost("{gameId}/join")]
    public ActionResult JoinGameSession(string gameId)
    {
      if (!GameConnector.ActiveGames.TryGetValue(gameId, out var activeGame))
      {
        return NotFound("Game session not found.");
      }

      if (activeGame.Players.Count >= 2)
      {
        return BadRequest("Game session is full.");
      }

      string userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("User ID claim is missing.");

      if (activeGame.Players.ContainsKey(userId))
      {
        return BadRequest("User already joined the game session.");
      }

      activeGame.Players[userId] = CheckerType.Black;
      activeGame.Score[userId] = 0;
      activeGame.GetGameSession = NewGame(activeGame.InitialBoard);

      return Ok();
    }

    [HttpGet("{gameId}")]
    public ActionResult GetGameSession(string gameId)
    {
      if (!GameConnector.ActiveGames.TryGetValue(gameId, out var activeGame))
      {
        return NotFound("Game session not found.");
      }

      return GameStateResponse(activeGame);
    }

    [HttpPost("{gameId}/roll")]
    public ActionResult Roll(string gameId)
    {
      if (!GameConnector.ActiveGames.TryGetValue(gameId, out var activeGame))
      {
        return NotFound("Game session not found.");
      }

      if (activeGame.Players.Count < 2 || activeGame.GetGameSession == null)
      {
        return BadRequest("Game didn't start yet. Waiting for another player to join.");
      }

      if (activeGame.GetGameSession.BoardState.Winner != null)
      {
        return BadRequest("Game is already over.");
      }

      var gameSession = activeGame.GetGameSession;

      var rollResult = gameSession.Roll();

      return Ok(new
      {
        Dice = new int[] { rollResult.Dice.Item1, rollResult.Dice.Item2 },
        ValidMoves = rollResult.ValidMoves.Select(m => m.Select(move => new int[] { move.Item1, move.Item2 }).ToArray()).ToArray()
      });
    }

    [HttpPost("{gameId}/move")]
    public ActionResult MakeMove(string gameId, [FromBody] int[][] move)
    {
      if (!GameConnector.ActiveGames.TryGetValue(gameId, out var activeGame))
      {
        return NotFound("Game session not found.");
      }

      if (move == null || move.Length == 0 || move.Any(m => m.Length != 2))
      {
        return BadRequest("Invalid move format.");
      }

      if (activeGame.Players.Count < 2 || activeGame.GetGameSession == null)
      {
        return BadRequest("Game didn't start yet. Waiting for another player to join.");
      }

      if (activeGame.GetGameSession.BoardState.Winner != null)
      {
        return BadRequest("Game is already over.");
      }

      var gameSession = activeGame.GetGameSession;      

      var playerColor = activeGame.Players.FirstOrDefault(p => p.Key == User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value).Value;
      if (playerColor != gameSession.CurrentPlayer)
      {
        return BadRequest("It's not the player's turn.");
      }

      var validMove = gameSession.MakeMove([.. move.Select(m => (m[0], m[1]))]);
      if (!validMove)
      {
        return BadRequest("Invalid move.");
      }
      
      if (gameSession.BoardState.Winner != null)
      {
        var winningColor = gameSession.BoardState.Winner.Value > 0 ? CheckerType.White : CheckerType.Black;
        activeGame.Score[activeGame.Players.FirstOrDefault(p => p.Value == winningColor).Key]+= Math.Abs(gameSession.BoardState.Winner.Value);
      }

      return GameStateResponse(activeGame);
    }

    [HttpPost("{gameId}/rematch")]
    public ActionResult RequestRematch(string gameId)
    {
      if (!GameConnector.ActiveGames.TryGetValue(gameId, out var activeGame))
      {
        return NotFound("Game session not found.");
      }

      if (activeGame.GetGameSession == null)
      {
        return BadRequest("Game session has not started yet.");
      }

      if (activeGame.RematchRequests.Count >= 2)
      {
        return BadRequest("Rematch already requested by both players.");
      }

      if (activeGame.GetGameSession.BoardState.Winner == null)
      {
        return BadRequest("Game is not finished yet.");
      }

      string userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("User ID claim is missing.");
      activeGame.RematchRequests.Add(userId);

      lock (activeGame)
      {
        if (activeGame.RematchRequests.Count == 2)
        {
          activeGame.Players[activeGame.RematchRequests.ElementAt(0)] = activeGame.Players[activeGame.RematchRequests.ElementAt(0)].Opponent();
          activeGame.Players[activeGame.RematchRequests.ElementAt(1)] = activeGame.Players[activeGame.RematchRequests.ElementAt(1)].Opponent();
          
          activeGame.GetGameSession = NewGame(activeGame.InitialBoard);
          activeGame.RematchRequests.Clear();
        }
      }

      return GameStateResponse(activeGame);
    }

    private OkObjectResult GameStateResponse(ActiveGame activeGame)
    {
      return Ok(new
      {
        Players = activeGame.Players,
        Board = activeGame.GetGameSession?.BoardState,
        activeGame.RematchRequests
      });
    }

    private static GameSession NewGame(List<PointState> initialBoard)
    {
      var boardInitializer = initialBoard.Count > 0 ? 
        new CustomBoardSetup(initialBoard.Select(p => new PointSetup { PointIndex = p.Index, InitialCheckers = p.Count, CheckerType = p.Type })) :
        null;

      return Backgammon.StartNewGame(boardSetup: boardInitializer);
    }
  }
}
