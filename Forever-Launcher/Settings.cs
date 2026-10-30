using System;
using System.IO;

#nullable disable
namespace ForeverLauncher
{
  internal static class Settings
  {
    public static string Dir
    {
      get
      {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ForeverLauncher");
      }
    }

    private static string FilePath => Path.Combine(Settings.Dir, "gamedir.txt");

    private static string ServerFile => Path.Combine(Settings.Dir, "server.txt");

    public static string LoadServer()
    {
      try
      {
        return File.ReadAllText(Settings.ServerFile).Trim();
      }
      catch
      {
        return (string) null;
      }
    }

    public static void SaveServer(string name)
    {
      try
      {
        Directory.CreateDirectory(Settings.Dir);
        File.WriteAllText(Settings.ServerFile, name ?? "");
      }
      catch
      {
      }
    }

    public static string LoadGameDir()
    {
      try
      {
        string path1 = File.ReadAllText(Settings.FilePath).Trim();
        return File.Exists(Path.Combine(path1, "Wow.exe")) ? path1 : (string) null;
      }
      catch
      {
        return (string) null;
      }
    }

    public static void SaveGameDir(string dir)
    {
      try
      {
        Directory.CreateDirectory(Path.GetDirectoryName(Settings.FilePath));
        File.WriteAllText(Settings.FilePath, dir);
      }
      catch
      {
      }
    }

    private static string NoFogFile => Path.Combine(Settings.Dir, "nofog.txt");

    public static bool LoadRemoveFog()
    {
      try
      {
        return File.ReadAllText(Settings.NoFogFile).Trim() == "1";
      }
      catch
      {
        return false;
      }
    }

    public static void SaveRemoveFog(bool on)
    {
      try
      {
        Directory.CreateDirectory(Settings.Dir);
        File.WriteAllText(Settings.NoFogFile, on ? "1" : "0");
      }
      catch
      {
      }
    }

    private static string SoftwareRenderFile => Path.Combine(Settings.Dir, "softwarerender.txt");

    public static bool LoadSoftwareRendering()
    {
      try
      {
        return File.ReadAllText(Settings.SoftwareRenderFile).Trim() == "1";
      }
      catch
      {
        return false;
      }
    }

    public static void SaveSoftwareRendering()
    {
      try
      {
        Directory.CreateDirectory(Settings.Dir);
        File.WriteAllText(Settings.SoftwareRenderFile, "1");
      }
      catch
      {
      }
    }
  }
}
