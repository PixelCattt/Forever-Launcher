using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

#nullable disable
namespace ForeverLauncher
{
  internal static class GameLocator
  {
    public static string Find()
    {
      List<string> stringList = new List<string>();
      string directoryName1 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      stringList.Add(directoryName1);
      stringList.Add(Path.Combine(directoryName1, "_classic_beta_"));
      stringList.Add(Path.Combine(Path.GetDirectoryName(directoryName1) ?? directoryName1, "_classic_beta_"));
      string[] strArray1 = new string[2]
      {
        "SOFTWARE\\WOW6432Node\\Blizzard Entertainment\\World of Warcraft",
        "SOFTWARE\\Blizzard Entertainment\\World of Warcraft"
      };
      foreach (string name1 in strArray1)
      {
        try
        {
          using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(name1))
          {
            if (registryKey != null)
            {
              string[] strArray2 = new string[2]
              {
                "InstallPath",
                "GamePath"
              };
              foreach (string name2 in strArray2)
              {
                string directoryName2 = registryKey.GetValue(name2) as string;
                if (!string.IsNullOrEmpty(directoryName2))
                {
                  if (File.Exists(directoryName2))
                    directoryName2 = Path.GetDirectoryName(directoryName2);
                  string str = directoryName2.TrimEnd('\\');
                  string directoryName3 = Path.GetDirectoryName(str);
                  if (directoryName3 != null)
                    stringList.Add(Path.Combine(directoryName3, "_classic_beta_"));
                  stringList.Add(Path.Combine(str, "_classic_beta_"));
                }
              }
            }
          }
        }
        catch
        {
        }
      }
      foreach (DriveInfo driveInfo in ((IEnumerable<DriveInfo>) DriveInfo.GetDrives()).Where<DriveInfo>((Func<DriveInfo, bool>) (d =>
      {
        try
        {
          return d.DriveType == DriveType.Fixed && d.IsReady;
        }
        catch
        {
          return false;
        }
      })))
      {
        string fullName = driveInfo.RootDirectory.FullName;
        string[] strArray3 = new string[8]
        {
          "World of Warcraft",
          "Program Files (x86)\\World of Warcraft",
          "Program Files\\World of Warcraft",
          "Games\\World of Warcraft",
          "Juegos\\World of Warcraft",
          "Battle.net\\World of Warcraft",
          "Blizzard\\World of Warcraft",
          "Program Files (x86)\\Battle.net\\World of Warcraft"
        };
        foreach (string path2 in strArray3)
          stringList.Add(Path.Combine(fullName, path2, "_classic_beta_"));
      }
      foreach (string str in stringList)
      {
        try
        {
          if (File.Exists(Path.Combine(str, "Wow.exe")))
            return Path.GetFullPath(str);
        }
        catch
        {
        }
      }
      return (string) null;
    }
  }
}
