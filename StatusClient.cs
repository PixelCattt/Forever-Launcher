using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

#nullable disable
namespace ForeverLauncher
{
  public static class StatusClient
  {
    public static async Task<ServerStatus> FetchAsync(ServerEntry server)
    {
      ServerStatus s = new ServerStatus();
      Task<long> login = StatusClient.ProbeAsync(server.Portal, server.LoginPort);
      System.Collections.Generic.List<Task<long>> realms = new System.Collections.Generic.List<Task<long>>();
      foreach (RealmEntry realm in server.Realms)
        realms.Add(StatusClient.ProbeAsync(server.Portal, realm.Port));
      Task feed = string.IsNullOrEmpty(server.StatusUrl) ? (Task) Task.FromResult<int>(0) : StatusClient.FeedAsync(s, server.StatusUrl);
      s.LoginMs = await login;
      s.LoginUp = s.LoginMs >= 0L;
      for (int i = 0; i < realms.Count; ++i)
      {
        RealmStatus rs = new RealmStatus()
        {
          Realm = server.Realms[i],
          Ms = await realms[i]
        };
        s.Realms.Add(rs);
        if (rs.Up)
          s.WorldUp = true;
      }
      await feed;
      return s;
    }

    private static async Task<long> ProbeAsync(string host, int port)
    {
      TcpClient c = new TcpClient();
      try
      {
        Stopwatch sw = Stopwatch.StartNew();
        Task connect = c.ConnectAsync(host, port);
        if (await Task.WhenAny(connect, Task.Delay(3000)) != connect)
          return -1;
        await connect;
        return sw.ElapsedMilliseconds;
      }
      catch
      {
        return -1;
      }
      finally
      {
        try
        {
          c.Close();
        }
        catch
        {
        }
      }
    }

    private static async Task FeedAsync(ServerStatus s, string feedUrl)
    {
      try
      {
        string json;
        using (WebClient wc = new WebClient())
        {
          wc.Encoding = Encoding.UTF8;
          wc.Headers[HttpRequestHeader.CacheControl] = "no-cache";
          json = await wc.DownloadStringTaskAsync(feedUrl + (feedUrl.Contains("?") ? (object) "&t=" : (object) "?t=") + (object) (DateTime.UtcNow.Ticks / 600000000L));
        }
        if (!(new JavaScriptSerializer().DeserializeObject(json) is Dictionary<string, object> root))
          return;
        s.FeedOk = true;
        s.Maintenance = StatusClient.Bool(root, "maintenance");
        foreach (object obj in StatusClient.List(root, "news"))
        {
          if (obj is Dictionary<string, object> d)
            s.News.Add(new FeedEntry(d));
        }
        foreach (object obj in StatusClient.List(root, "links"))
        {
          if (obj is Dictionary<string, object> d && StatusClient.Str(d, "label") != null && StatusClient.Str(d, "url") != null)
            s.Links.Add(new FeedEntry(d));
        }
        foreach (object obj in StatusClient.List(root, "clientBuilds"))
        {
          if (obj is string input && Regex.IsMatch(input, "^1\\.60\\.\\d+\\.\\d{5}$"))
            s.ClientBuilds.Add(input);
        }
      }
      catch
      {
      }
    }

    private static string Str(Dictionary<string, object> d, string k)
    {
      object obj;
      return !d.TryGetValue(k, out obj) || obj == null ? (string) null : obj.ToString();
    }

    private static bool Bool(Dictionary<string, object> d, string k)
    {
      object obj;
      return d.TryGetValue(k, out obj) && obj is bool flag && flag;
    }

    private static IEnumerable List(Dictionary<string, object> d, string k)
    {
      object obj;
      return !d.TryGetValue(k, out obj) || !(obj is IEnumerable) || obj is string ? (IEnumerable) new object[0] : (IEnumerable) obj;
    }

  }
}
