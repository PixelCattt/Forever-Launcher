using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

#nullable disable
namespace ForeverLauncher
{
  internal static class Settings
  {
    private const string ConfigFileName = "config.json";

    public static string Dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Forever-Launcher");

    public static string LogsDir => Path.Combine(Settings.Dir, "logs");

    public static string LogFile => Path.Combine(Settings.LogsDir, "log.txt");

    private static string ConfigPath => Path.Combine(Settings.Dir, Settings.ConfigFileName);

    public static string LoadLanguage()
    {
      string value = Settings.ReadString("lang");
      return string.IsNullOrWhiteSpace(value) ? "en" : value;
    }

    public static void SaveLanguage(string language) => Settings.Write("lang", language ?? "en");

    public static string LoadServer() => Settings.ReadString("last_server");

    public static void SaveServer(string name) => Settings.Write("last_server", name ?? "");

    public static string LoadGameDir()
    {
      string dir = Settings.ReadString("game_dir");
      return !string.IsNullOrWhiteSpace(dir) && File.Exists(Path.Combine(dir, "Wow.exe")) ? dir : null;
    }

    public static void SaveGameDir(string dir) => Settings.Write("game_dir", dir ?? "");

    public static bool LoadRemoveFog() => Settings.ReadBool("remove_fog");

    public static void SaveRemoveFog(bool on) => Settings.Write("remove_fog", on);

    public static bool LoadSoftwareRendering() => Settings.ReadBool("software_rendering");

    public static void SaveSoftwareRendering() => Settings.Write("software_rendering", true);

    private static string ReadString(string key)
    {
      try
      {
        object value;
        Dictionary<string, object> config = Settings.ReadConfig();
        return config.TryGetValue(key, out value) && value != null ? value.ToString() : null;
      }
      catch
      {
        return null;
      }
    }

    private static bool ReadBool(string key)
    {
      try
      {
        object value;
        Dictionary<string, object> config = Settings.ReadConfig();
        return config.TryGetValue(key, out value) && value is bool enabled && enabled;
      }
      catch
      {
        return false;
      }
    }

    private static Dictionary<string, object> ReadConfig()
    {
      try
      {
        if (File.Exists(Settings.ConfigPath))
        {
          string original = File.ReadAllText(Settings.ConfigPath);
          Dictionary<string, object> loaded = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(original);
          Dictionary<string, object> config = Settings.DefaultConfig();
          if (loaded != null)
            foreach (KeyValuePair<string, object> setting in loaded)
              config[setting.Key] = setting.Value;
          object oldLanguage;
          if (loaded != null && !loaded.ContainsKey("lang") && loaded.TryGetValue("language", out oldLanguage))
            config["lang"] = oldLanguage;
          config.Remove("language");
          string formatted = Settings.FormatConfig(config);
          if (original != formatted)
            File.WriteAllText(Settings.ConfigPath, formatted, new UTF8Encoding(false));
          return config;
        }
      }
      catch
      {
      }
      Dictionary<string, object> defaults = Settings.DefaultConfig();
      try
      {
        Directory.CreateDirectory(Settings.Dir);
        File.WriteAllText(Settings.ConfigPath, Settings.FormatConfig(defaults), new UTF8Encoding(false));
      }
      catch
      {
      }
      return defaults;
    }

    private static Dictionary<string, object> DefaultConfig()
    {
      return new Dictionary<string, object>()
      {
        { "lang", "en" },
        { "game_dir", "" },
        { "last_server", "" },
        { "remove_fog", false },
        { "software_rendering", false }
      };
    }

    private static void Write(string key, object value)
    {
      try
      {
        Directory.CreateDirectory(Settings.Dir);
        Dictionary<string, object> config = Settings.ReadConfig();
        config[key] = value;
        File.WriteAllText(Settings.ConfigPath, Settings.FormatConfig(config), new UTF8Encoding(false));
      }
      catch
      {
      }
    }

    private static string FormatConfig(Dictionary<string, object> config)
    {
      JavaScriptSerializer serializer = new JavaScriptSerializer();
      string[] keys = { "lang", "game_dir", "last_server", "remove_fog", "software_rendering" };
      List<KeyValuePair<string, object>> entries = new List<KeyValuePair<string, object>>();
      foreach (string key in keys)
      {
        object value;
        if (config.TryGetValue(key, out value))
          entries.Add(new KeyValuePair<string, object>(key, value));
      }
      foreach (string key in config.Keys.Where(k => !keys.Contains(k)).OrderBy(k => k, StringComparer.Ordinal))
        entries.Add(new KeyValuePair<string, object>(key, config[key]));

      StringBuilder json = new StringBuilder();
      json.AppendLine("{");
      for (int i = 0; i < entries.Count; i++)
      {
        if (entries[i].Key == "software_rendering")
          json.AppendLine();
        json.Append("  ").Append(serializer.Serialize(entries[i].Key)).Append(": ").Append(serializer.Serialize(entries[i].Value));
        if (i < entries.Count - 1)
          json.Append(',');
        json.AppendLine();
      }
      json.Append('}');
      return json.ToString();
    }
  }
}
