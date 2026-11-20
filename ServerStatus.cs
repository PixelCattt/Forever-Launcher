using System.Collections.Generic;

#nullable disable
namespace ForeverLauncher
{
  public sealed class ServerStatus
  {
    public bool LoginUp;
    public bool WorldUp;
    public List<RealmStatus> Realms = new List<RealmStatus>();
    public long LoginMs = -1;
    public bool FeedOk;
    public bool Maintenance;
    public List<FeedEntry> News = new List<FeedEntry>();
    public List<FeedEntry> Links = new List<FeedEntry>();
    public List<string> ClientBuilds = new List<string>();
  }
}
