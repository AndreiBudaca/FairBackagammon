using System.Collections.Concurrent;

namespace FairBackgammon.Api.Connectors
{
  public static class GameConnector
  {
    public static ConcurrentDictionary<string, ActiveGame> ActiveGames { get; } = [];
  }
}