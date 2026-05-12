using System;
using System.Linq;
using System.Security.Claims;
using FairBackgammon.Api.Connectors;
using FairBackgammon.GameLogic;
using FairBackgammon.GameLogic.Enums;
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
    public ActionResult CreateGameSession()
    {
      var gameId = Guid.NewGuid().ToString();

      var gameCreated = GameConnector.ActiveGames.TryAdd(gameId, new ActiveGame
      {
        Players = User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).ToDictionary(c => c.Value, c => CheckerType.White)
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
      activeGame.GetGameSession = Backgammon.StartNewGame();

      return Ok();
    }

    [HttpGet("{gameId}")]
    public ActionResult GetGameSession(string gameId)
    {
      if (!GameConnector.ActiveGames.TryGetValue(gameId, out var activeGame))
      {
        return NotFound("Game session not found.");
      }

      return Ok(
        new
        {
          Players = activeGame.Players,
          Board = activeGame.GetGameSession?.BoardState
        });
    }

    [HttpPost("{gameId}/roll")]
    public ActionResult Roll(string gameId)
    {
      if (!GameConnector.ActiveGames.TryGetValue(gameId, out var activeGame))
      {
        return NotFound("Game session not found.");
      }

      var gameSession = activeGame.GetGameSession ?? throw new InvalidOperationException("Game session has not started yet.");

      var rollResult = gameSession.Roll();

      return Ok(new
      {
        Dice = new int[] { rollResult.Dice.Item1, rollResult.Dice.Item2 },
        ValidMoves = rollResult.ValidMoves.Select(m => m.Select(move => new int[] { move.Item1, move.Item2 }).ToArray()).ToArray()
      });
    }

    [HttpPost("{gameId}/move")]
    public ActionResult MakeMove(string gameId, [FromBody] (int from, int to)[] move)
    {
      if (!GameConnector.ActiveGames.TryGetValue(gameId, out var activeGame))
      {
        return NotFound("Game session not found.");
      }

      var gameSession = activeGame.GetGameSession ?? throw new InvalidOperationException("Game session has not started yet.");
      var playerColor = activeGame.Players.FirstOrDefault(p => p.Key == User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value).Value;

      var validMove = gameSession.MakeMove(move);
      if (!validMove)
      {
        return BadRequest("Invalid move.");
      }

      return Ok(
       new
       {
         Players = activeGame.Players,
         Board = activeGame.GetGameSession?.BoardState
       });
    }
  }
}
