using System.Collections.Generic;
using FairBackgammon.GameLogic.Enums;
using FairBackgammon.GameLogic.Sessions;
using FairBackgammon.GameLogic.Sessions.State;

namespace FairBackgammon.Api.Connectors
{
  public class ActiveGame
  {
    public required Dictionary<string, CheckerType> Players { get; init; }

    public required Dictionary<string, int> Score { get; init; }

    public required List<PointState> InitialBoard { get; init; }

    public HashSet<string> RematchRequests { get; } = [];

    public GameSession? GetGameSession { get; set; }
  }
}