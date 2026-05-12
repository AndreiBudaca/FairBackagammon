using System.Collections.Generic;
using FairBackgammon.GameLogic.Enums;
using FairBackgammon.GameLogic.Sessions;

namespace FairBackgammon.Api.Connectors
{
  public class ActiveGame
  {
    public required Dictionary<string, CheckerType> Players { get; init; }

    public HashSet<string> RematchRequests { get; } = [];

    public GameSession? GetGameSession { get; set; }
  }
}