using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

#nullable disable
namespace ForeverLauncher
{
  public static class Servers
  {
    public const string FileName = "servers.json";
    public static readonly System.Collections.Generic.List<ServerEntry> List = new System.Collections.Generic.List<ServerEntry>();
    public static readonly System.Collections.Generic.List<string> Errors = new System.Collections.Generic.List<string>();

    public static string FilePath { get; private set; }

    private static string PortablePath
    {
      get
      {
        return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "servers.json");
      }
    }

    private static string UserPath => Path.Combine(Settings.Dir, "servers.json");

    public static void Load()
    {
      Servers.List.Clear();
      Servers.Errors.Clear();
      Servers.FilePath = Servers.PortablePath;
      Servers.EnsureConfigFile();
      object[] objArray;
      try
      {
        object source;
        objArray = !(new JavaScriptSerializer().DeserializeObject(File.ReadAllText(Servers.FilePath, Encoding.UTF8)) is Dictionary<string, object> dictionary) || !dictionary.TryGetValue("servers", out source) || !(source is IEnumerable) ? new object[0] : ((IEnumerable) source).Cast<object>().ToArray<object>();
      }
      catch (Exception ex)
      {
        Servers.Errors.Add(ex.Message);
        return;
      }
      foreach (object obj1 in objArray)
      {
        if (obj1 is Dictionary<string, object> d1)
        {
          ServerEntry serverEntry = new ServerEntry()
          {
            Name = Servers.Str(d1, "name") ?? "",
            Portal = (Servers.Str(d1, "portal") ?? "").Trim(),
            PublicKey = (Servers.Str(d1, "publicKey") ?? "02596F0D0C061A8B30745988FD72C59E29EC367FB0F341F28E0F08D037BAFC69").Trim().ToUpperInvariant(),
            RegionGroup = Servers.Int(d1, "regionGroup", 8),
            LoginPort = Servers.Int(d1, "loginPort", 1119),
            WorldPort = Servers.Int(d1, "worldPort", 8085),
            StatusUrl = Servers.Str(d1, "statusUrl") ?? "",
            Website = Servers.Str(d1, "website") ?? "",
            SupportHost = (Servers.Str(d1, "supportHost") ?? "").Trim()
          };
          object obj2;
          if (d1.TryGetValue("realms", out obj2) && obj2 is IEnumerable && !(obj2 is string))
          {
            foreach (object obj3 in (IEnumerable) obj2)
            {
              Dictionary<string, object> d = obj3 as Dictionary<string, object>;
              int num = d != null ? Servers.Int(d, "port", 0) : 0;
              if (d != null && num > 0 && num < 65536)
                serverEntry.Realms.Add(new RealmEntry()
                {
                  Name = Servers.Str(d, "name") ?? "Port " + (object) num,
                  Port = num,
                  Kind = (Servers.Str(d, "kind") ?? "").ToLowerInvariant()
                });
            }
          }
          if (serverEntry.Realms.Count == 0)
            serverEntry.Realms = ServerEntry.DefaultRealms(serverEntry.WorldPort);
          string str = serverEntry.Validate();
          if (str != null)
            Servers.Errors.Add((serverEntry.Name.Length > 0 ? serverEntry.Name : "server #" + (object) (Servers.List.Count + Servers.Errors.Count + 1)) + ": " + str);
          else
            Servers.List.Add(serverEntry);
        }
      }
    }

    public static ServerEntry Find(string name)
    {
      return Servers.List.FirstOrDefault<ServerEntry>((Func<ServerEntry, bool>) (s => s.Name == name)) ?? Servers.List.FirstOrDefault<ServerEntry>();
    }

    private static void EnsureConfigFile()
    {
      try
      {
        if (File.Exists(Servers.PortablePath) && Servers.IsExampleConfig(Servers.PortablePath))
          File.Delete(Servers.PortablePath);

        if (File.Exists(Servers.PortablePath))
          return;

        Directory.CreateDirectory(Path.GetDirectoryName(Servers.PortablePath));
        if (File.Exists(Servers.UserPath) && !Servers.IsExampleConfig(Servers.UserPath))
          File.Copy(Servers.UserPath, Servers.PortablePath, false);
        else
          Servers.WriteBundledConfig(Servers.PortablePath);
      }
      catch (Exception ex)
      {
        Servers.Errors.Add("Could not create servers.json beside the launcher: " + ex.Message);
        try
        {
          Directory.CreateDirectory(Path.GetDirectoryName(Servers.UserPath));
          if (!File.Exists(Servers.UserPath) || Servers.IsExampleConfig(Servers.UserPath))
          {
            if (File.Exists(Servers.UserPath))
              File.Delete(Servers.UserPath);
            Servers.WriteBundledConfig(Servers.UserPath);
          }
          Servers.FilePath = Servers.UserPath;
        }
        catch (Exception fallbackError)
        {
          Servers.Errors.Add("Could not create a user server config: " + fallbackError.Message);
        }
      }
    }
    private static bool IsExampleConfig(string path)
    {
      try
      {
        string json = File.ReadAllText(path, Encoding.UTF8);
        return json.IndexOf("logon.example.com", StringComparison.OrdinalIgnoreCase) >= 0;
      }
      catch
      {
        return false;
      }
    }
    private static void WriteBundledConfig(string path)
    {
      using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ForeverLauncher.config.servers.json"))
      {
        if (stream == null)
          throw new InvalidOperationException("The bundled server configuration is missing.");

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (FileStream file = File.Create(path))
          stream.CopyTo(file);
      }
    }
    private static string Str(Dictionary<string, object> d, string k)
    {
      object obj;
      return !d.TryGetValue(k, out obj) || obj == null ? (string) null : obj.ToString();
    }

    private static int Int(Dictionary<string, object> d, string k, int def)
    {
      object obj;
      int result;
      return !d.TryGetValue(k, out obj) || obj == null || !int.TryParse(obj.ToString(), out result) ? def : result;
    }
  }
}
