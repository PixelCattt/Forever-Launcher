using System.Collections.Generic;

#nullable disable
namespace ForeverLauncher
{
  public sealed class FeedEntry
  {
    private readonly Dictionary<string, object> d;

    public FeedEntry(Dictionary<string, object> d)
    {
      this.d = d ?? new Dictionary<string, object>();
    }

    public string Get(string key)
    {
      object obj;
      return this.d.TryGetValue(key + "_" + Localization.Lang, out obj) && obj != null && obj.ToString().Length > 0 || this.d.TryGetValue(key, out obj) && obj != null ? obj.ToString() : (string) null;
    }
  }
}
