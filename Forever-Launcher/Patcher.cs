using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

#nullable disable
namespace ForeverLauncher
{
  public sealed class Patcher
  {
    public const string ConfigName = "ForeverLauncher.wtf";
    public const string ExeName = "Wow.exe";
    public const string ExpectedVersion = "1.60.1.70532";
    private const int EntrySize = 40;
    private const int EntryCount = 12;
    private const int BlockSize = 480;
    private const int CertKeysFromBuild = 70425;
    private const string AllowSuffix = "([:/\\?].*|\\n|$)";
    private const string AllowPrefix = "https://wauth.teledit.com/Danal";
    private const string AllowEntryHost = "wauth\\.teledit\\.com/Danal";
    private const string NewestFogBuild = "1.60.1.70425";
    public static readonly string[] SupportedVersions = new string[16]
    {
      "1.60.1.69913",
      "1.60.1.69977",
      "1.60.1.70009",
      "1.60.1.70058",
      "1.60.1.70124",
      "1.60.1.70170",
      "1.60.1.70205",
      "1.60.1.70235",
      "1.60.1.70245",
      "1.60.1.70291",
      "1.60.1.70334",
      "1.60.1.70338",
      "1.60.1.70374",
      "1.60.1.70425",
      "1.60.1.70450",
      "1.60.1.70532"
    };
    public static volatile string[] FeedVersions = new string[0];
    private static readonly string[] KnownKeys = new string[12]
    {
      "9B0671C815DFF513BFD4A2B26AE1F84EC9106841B2FB620DB65F6ADE7C21AD06",
      "112451B9843C2799E4750AA3D9B9AFABF536A645B863C4ADB2078B932B354E04",
      "A6B858485748BF38BE193517AB90F8DE169EFF0989EA9360DB346A378B0FFE15",
      "50FFDEB807F8FF73A299A16000AA6575C5945BE875ADFC8795374ED3415249AC",
      "9F842D078755647C008E5FE3E12B839A981DE02A1F520CB2545CC04CD2323E0E",
      "B8CC75864D8EF461D8DD6AA7425E2C63C24D3982066B8D773A1549BFE24E05EE",
      "76BB7BBDD9F34E124A4573C3AA227E3C87CC603506697D054F6FDEC342E56EBF",
      "1FD6DD8FA0EC30D39E3F72E755B8A045BDE0F70449DC71008B767C2EAA89FB9F",
      "15D618BD7DB577BD9A8D45769C59E4FC631633BF447398A4B489B4C26FBC03AD",
      "B34EC592D2AC4993F5FFC85B15C3DA9078694051CB224345592AF7136D796C99",
      "9E91586DD4B115AB0568B21959E181F6545910BC5E2F30B8975CA09D7BC3EFCE",
      "769C824A1DADD19AEEFA420414D1732DAF44537F98AA229FC5477BCA55F9D59A"
    };
    private static readonly byte[] ExpectedFlags = new byte[12]
    {
      (byte) 1,
      (byte) 1,
      (byte) 1,
      (byte) 1,
      (byte) 1,
      (byte) 1,
      (byte) 1,
      (byte) 1,
      (byte) 0,
      (byte) 1,
      (byte) 1,
      (byte) 1
    };
    private readonly HashSet<long> certCopies = new HashSet<long>();
    private static readonly string[][] HelpUrls = new string[3][]
    {
      new string[2]
      {
        "https://%s.battle.net/support/%s/help/home",
        "/kb"
      },
      new string[2]
      {
        "https://%s.battle.net/support/%s/ticket/submit?loc",
        "/help/ticket?loc"
      },
      new string[2]
      {
        "https://%s.battle.net/support/%s/ticket/status?loc",
        "/help/tickets?loc"
      }
    };
    public static volatile bool RemoveFog;
    private static readonly Dictionary<string, string> FogDensityPallets = new Dictionary<string, string>()
    {
      {
        "1.60.1.70205",
        "000014420000A142000008420000000000000042CDCCCC3D0000A0410AD7233E0000003F0AD7A33CCDCC4C3D9A99193F9A99993F8FC2F53C000000400000803F0AD7A33FA4703D3FCDCC4C3F0AD7233D0000003ECDCC4C3E0AD7233CCDCCCC3ECDCCCC3C9A99193E3333333F9A99993E8EBEAC3EFEBAA43EB95F973E36788E3E0000803E6666E63ECDCC0C3F0AD7A33D3333333E295C0F3E0000C03F295C8F3D8FC2753D00004040CDCC8C3F0000C04000000041000060406666663E000020403333B33E0000403F0000A040BCBBBB3E00008040CDCC8C3EABAAAA3E2ED8823E0000A03F000070419A99593F00002041333333406666A63FF628DC3E9A99993D8FC2F53EB81E053E14AE473EEC51383EA4703D3E022B073E0000D03F713DAA3F5555B53FC771DC3F5655E53FEC51B83D9A99493E3333433E9A99393ECDCC2C3E0000403E1111513E6666563ECDCC9C3E3333A33ECDCCAC3E3433B33E9A99B93E6666A63E5655953E0000903EE32B3E3E843A283E00008241EFEEAE3E610BB63E0000C03E5655D53EDEDDDD3E5555D53E8988C83E9A99193DBCBB3B3DEFEE6E3D8988883D388EE33CA8AAAA3C8FC2F53DDDDDDD3DE27A143E000040413BA8033E532EE53DAE47E13D6666263F7B142E3EF6285C3FC3F5283F6666E63F6EDBB63EE22B3E3F295C8F3E3333F33F9A99D93FCDCCCC3F3333D33F5C8F423E"
      },
      {
        "1.60.1.70235",
        (string) null
      },
      {
        "1.60.1.70245",
        (string) null
      },
      {
        "1.60.1.70291",
        "000014420000A142000008420000000000000042CDCCCC3D0000A0410AD7233E0000003F0AD7A33CCDCC4C3D9A99193F9A99993F8FC2F53C000000400000803F0AD7A33FA4703D3FCDCC4C3F0AD7233D0000003ECDCC4C3E0AD7233CCDCCCC3ECDCCCC3C9A99193E3333333F9A99993E8EBEAC3EFEBAA43EB95F973E36788E3E0000803E6666E63ECDCC0C3F0AD7A33D3333333E295C0F3E0000C03F295C8F3D8FC2753D00004040CDCC8C3F0000C04000000041000060406666663E000020403333B33E0000403F0000A040BCBBBB3E00008040CDCC8C3EABAAAA3E2ED8823E0000A03F000070419A99593F00002041333333406666A63FF628DC3E9A99993D8FC2F53EB81E053E14AE473E8FC2F53DEC51B83DEC51383EA4703D3E022B073E0000D03F713DAA3F5555B53FC771DC3F5655E53F9A99493E3333433E9A99393ECDCC2C3E0000403E1111513E6666563ECDCC9C3E3333A33ECDCCAC3E3433B33E9A99B93E6666A63E5655953E0000903EE32B3E3E843A283E00008241EFEEAE3E610BB63E0000C03E5655D53EDEDDDD3E5555D53E8988C83E9A99193DBCBB3B3DEFEE6E3D8988883D388EE33CA8AAAA3CDDDDDD3DE27A143E000040413BA8033E532EE53DAE47E13D6666263F7B142E3EF6285C3FC3F5283F6666E63F6EDBB63EE22B3E3F295C8F3E3333F33F9A99D93FCDCCCC3F3333D33F5C8F423E5839B43CA69BC43D"
      },
      {
        "1.60.1.70334",
        "=1.60.1.70291"
      },
      {
        "1.60.1.70338",
        "=1.60.1.70291"
      },
      {
        "1.60.1.70374",
        "=1.60.1.70291"
      },
      {
        "1.60.1.70425",
        "000014420000A142000008420000000000000042CDCCCC3D0000A0410AD7233E0000003F0AD7A33CCDCC4C3D9A99193F9A99993F8FC2F53C000000400000803F0AD7A33FA4703D3FCDCC4C3F0AD7233D0000003ECDCC4C3E0AD7233CCDCCCC3ECDCCCC3C9A99193E3333333F9A99993E8EBEAC3EFEBAA43EB95F973E36788E3E0000803E6666E63ECDCC0C3F0AD7A33D3333333E295C0F3E0000C03F295C8F3D8FC2753D00004040CDCC8C3F8FC2F53D0000C04000000041000060406666663E000020403333B33E0000403F0000A040BCBBBB3E00008040CDCC8C3EABAAAA3E2ED8823E0000A03F000070419A99593F00002041333333406666A63FF628DC3E9A99993D8FC2F53EB81E053E14AE473EEC51B83DEC51383EA4703D3E022B073E0000D03F713DAA3F5555B53FC771DC3F5655E53F9A99493E3333433E9A99393ECDCC2C3E0000403E1111513E6666563ECDCC9C3E3333A33ECDCCAC3E3433B33E9A99B93E6666A63E5655953E0000903EE32B3E3E843A283E00008241EFEEAE3E610BB63E0000C03E5655D53EDEDDDD3E5555D53E8988C83E9A99193DBCBB3B3DEFEE6E3D8988883D388EE33CA8AAAA3CDDDDDD3DE27A143E000040413BA8033E532EE53DAE47E13D6666263F7B142E3EC3F5283F6666E63FF6285C3F6EDBB63EE22B3E3F295C8F3E3333F33F9A99D93FCDCCCC3F3333D33F5C8F423E5839B43CA69BC43D25491240DCB62D4000001C4066662640713D0A3E0000503F5555453F5655353FAAAA2A3F0000203F5555353FC7715C3F5655653F6666663F"
      },
      {
        "1.60.1.70450",
        "=1.60.1.70425"
      },
      {
        "1.60.1.70532",
        "=1.60.1.70425"
      }
    };
    private readonly string gameDir;
    private readonly string exePath;
    private readonly string logFile;
    private readonly string Portal;
    private readonly string NewKeyHex;
    private readonly string ServerName;
    private readonly string SupportHost;
    private readonly int TargetGroup;
    private volatile bool stopRequested;
    private string lastScan = "";
    private bool supportDone;
    private string clientVersion;
    private readonly List<long> fogPatched = new List<long>();
    private readonly Stopwatch fogClock = new Stopwatch();
    private bool fogMissingLogged;
    public bool StartNew;
    private static readonly object logLock = new object();

    public static bool IsSupported(string version)
    {
      return Array.IndexOf<string>(Patcher.SupportedVersions, version) >= 0 || Array.IndexOf<string>(Patcher.FeedVersions, version) >= 0;
    }

    public static string SupportedList()
    {
      List<string> stringList = new List<string>((IEnumerable<string>) Patcher.SupportedVersions);
      foreach (string feedVersion in Patcher.FeedVersions)
      {
        if (!stringList.Contains(feedVersion))
          stringList.Add(feedVersion);
      }
      return string.Join(", ", stringList.ToArray());
    }

    private static bool UsesCertificates(string version)
    {
      string[] strArray = (version ?? "").Split('.');
      int result;
      return int.TryParse(strArray[strArray.Length - 1], out result) && result >= 70425;
    }

    public static int SupportHostMaxLength => "wauth\\.teledit\\.com/Danal".Length;

    private static string PrefixFor(string host)
    {
      int num = "https://wauth.teledit.com/Danal".Length - "https://".Length;
      return host.Length <= num ? "https://" + new string('*', num - host.Length) + host : "https://*" + host.Substring(host.Length - (num - 1));
    }

    private static List<string[]> SupportPatches(string host)
    {
      List<string[]> strArrayList = new List<string[]>();
      foreach (string[] helpUrl in Patcher.HelpUrls)
        strArrayList.Add(new string[2]
        {
          helpUrl[0],
          "https://" + host + helpUrl[1]
        });
      string str = Patcher.PrefixFor(host);
      if (str != null)
        strArrayList.Add(new string[2]
        {
          "https://wauth.teledit.com/Danal",
          str
        });
      return strArrayList;
    }

    private static string[] SupportRegexPatch(string host)
    {
      string str = "";
      int num = "wauth\\.teledit\\.com/Danal".Length - host.Length;
      if (num % 2 == 1)
      {
        str = "(|)";
        num -= 3;
      }
      for (; num > 0; num -= 2)
        str += "()";
      return new string[2]
      {
        "^https://wauth\\.teledit\\.com/Danal([:/\\?].*|\\n|$)",
        "^https://" + host + str + "([:/\\?].*|\\n|$)"
      };
    }

    public event Action<StatusMessage> Message;

    public event Action<PatchPhase> PhaseChanged;

    public Patcher(string gameDir, ServerEntry server)
    {
      this.gameDir = gameDir;
      this.Portal = server.Portal;
      this.NewKeyHex = server.PublicKey.ToUpperInvariant();
      this.TargetGroup = server.RegionGroup;
      this.ServerName = server.Name;
      this.SupportHost = server.SupportHost;
      this.exePath = Path.GetFullPath(Path.Combine(gameDir, "Wow.exe"));
      this.logFile = Path.Combine(gameDir, "Logs", "launcher.log");
    }

    public string LogFile => this.logFile;

    public static StatusMessage CheckGameDir(string dir, out string version)
    {
      version = (string) null;
      if (string.IsNullOrEmpty(dir))
        return new StatusMessage(MessageKind.Warn, "dir.choose", new object[0]);
      string str = Path.Combine(dir, "Wow.exe");
      if (!File.Exists(str))
        return new StatusMessage(MessageKind.Warn, "dir.noexe", new object[0]);
      version = FileVersionInfo.GetVersionInfo(str).FileVersion;
      return (StatusMessage) null;
    }

    public static StatusMessage UntestedNotice(string version)
    {
      if (string.IsNullOrEmpty(version) || Patcher.IsSupported(version))
        return (StatusMessage) null;
      string[] parts = version.Split('.');
      int build;
      if (parts.Length == 0 || !int.TryParse(parts[parts.Length - 1], out build) || build <= 70685)
        return (StatusMessage) null;
      return new StatusMessage(MessageKind.Warn, "dir.untested", new object[1]
      {
        (object) version
      });
    }

    public void Stop() => this.stopRequested = true;

    private void Log(string text)
    {
      try
      {
        lock (Patcher.logLock)
        {
          Directory.CreateDirectory(Path.GetDirectoryName(this.logFile));
          File.AppendAllText(this.logFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + text + Environment.NewLine);
        }
      }
      catch
      {
      }
    }

    private void Say(MessageKind kind, string key, params object[] args)
    {
      StatusMessage msg = new StatusMessage(kind, key, args);
      this.Log(msg.ToString("en"));
      Action<StatusMessage> message = this.Message;
      if (message == null)
        return;
      message(msg);
    }

    private void Say(StatusMessage m)
    {
      this.Log(m.ToString("en"));
      Action<StatusMessage> message = this.Message;
      if (message == null)
        return;
      message(m);
    }

    private void Phase(PatchPhase p)
    {
      Action<PatchPhase> phaseChanged = this.PhaseChanged;
      if (phaseChanged == null)
        return;
      phaseChanged(p);
    }

    public void EnsureConfig()
    {
      string str1 = Path.Combine(this.gameDir, "WTF");
      string str2 = Path.Combine(str1, "ForeverLauncher.wtf");
      Directory.CreateDirectory(str1);
      string str3 = Path.Combine(str1, "Config.wtf");
      if (!File.Exists(str2) && File.Exists(str3))
      {
        File.Copy(str3, str2);
        this.Say(MessageKind.Dim, "p.wtfcreated", (object) "ForeverLauncher.wtf");
      }
      List<string> stringList = File.Exists(str2) ? ((IEnumerable<string>) File.ReadAllLines(str2)).ToList<string>() : new List<string>();
      string wanted = "SET portal \"" + this.Portal + "\"";
      bool flag1 = false;
      if (!stringList.Any<string>((Func<string, bool>) (l => l.StartsWith("SET portal "))))
      {
        stringList.Add(wanted);
        flag1 = true;
      }
      else if (!stringList.Contains(wanted))
      {
        stringList = stringList.Select<string, string>((Func<string, string>) (l => !l.StartsWith("SET portal ") ? l : wanted)).ToList<string>();
        flag1 = true;
      }
      Tuple<List<string>, List<string>> tuple = Patcher.InstalledLocales(this.gameDir);
      string str5 = Patcher.PickLocale(Localization.Lang, tuple.Item1);
      string str6 = Patcher.PickLocale(Localization.Lang, tuple.Item2);
      if (tuple.Item1.Count == 0)
      {
        if (!stringList.Any<string>((Func<string, bool>) (l => l.StartsWith("SET textLocale "))))
        {
          stringList.Add("SET textLocale \"enUS\"");
          flag1 = true;
        }
      }
      else
      {
        if (str5 == null)
        {
          str5 = tuple.Item1.Contains("enUS") || tuple.Item1.Count == 0 ? "enUS" : tuple.Item1[0];
          if (Localization.Lang != "en")
            this.Say(MessageKind.Warn, "p.nolocale", (object) Patcher.LocaleCodes(Localization.Lang)[0]);
        }
        if (str6 == null)
          str6 = tuple.Item2.Contains(str5) ? str5 : (tuple.Item2.Contains("enUS") ? "enUS" : (tuple.Item2.Count > 0 ? tuple.Item2[0] : str5));
        flag1 = flag1 | Patcher.SetLine(stringList, "textLocale", str5) | Patcher.SetLine(stringList, "audioLocale", str6);
      }
      if (!stringList.Any<string>((Func<string, bool>) (l => l.StartsWith("SET scriptErrors "))))
      {
        stringList.Add("SET scriptErrors \"0\"");
        flag1 = true;
      }
      if (!flag1)
        return;
      File.WriteAllLines(str2, (IEnumerable<string>) stringList);
      this.Say(MessageKind.Dim, "p.wtfupdated", (object) "ForeverLauncher.wtf", (object) this.Portal);
    }

    private static bool SetLine(List<string> lines, string cvar, string value)
    {
      string str = "SET " + cvar + " \"" + value + "\"";
      int index = lines.FindIndex((Predicate<string>) (l => l.StartsWith("SET " + cvar + " ")));
      if (index >= 0 && lines[index] == str)
        return false;
      if (index >= 0)
        lines[index] = str;
      else
        lines.Add(str);
      return true;
    }

    private static string[] LocaleCodes(string lang)
    {
      switch (lang)
      {
        case "es":
          return new string[2]{ "esES", "esMX" };
        case "de":
          return new string[1]{ "deDE" };
        case "ru":
          return new string[1]{ "ruRU" };
        default:
          return new string[2]{ "enUS", "enGB" };
      }
    }

    private static string PickLocale(string lang, List<string> installed)
    {
      foreach (string localeCode in Patcher.LocaleCodes(lang))
      {
        if (installed.Contains(localeCode))
          return localeCode;
      }
      return (string) null;
    }

    public static string LocaleName(string code)
    {
      switch (code)
      {
        case "enUS":
          return "English (US)";
        case "enGB":
          return "English (UK)";
        case "deDE":
          return "Deutsch";
        case "frFR":
          return "Français";
        case "esES":
          return "Español (España)";
        case "esMX":
          return "Español (México)";
        case "itIT":
          return "Italiano";
        case "ptBR":
          return "Português (Brasil)";
        case "ptPT":
          return "Português (Portugal)";
        case "ruRU":
          return "Русский";
        case "koKR":
          return "한국어";
        case "zhTW":
          return "繁體中文";
        case "zhCN":
          return "简体中文";
        default:
          return code;
      }
    }

    public static Tuple<List<string>, List<string>> InstalledLocales(string gameDir)
    {
      List<string> stringList1 = new List<string>();
      List<string> stringList2 = new List<string>();
      try
      {
        string str1 = File.Exists(Path.Combine(gameDir, ".flavor.info")) ? ((IEnumerable<string>) File.ReadAllLines(Path.Combine(gameDir, ".flavor.info"))).LastOrDefault<string>((Func<string, bool>) (l => l.Trim().Length > 0 && !l.Contains("!"))) : (string) null;
        string[] source = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(gameDir).TrimEnd('\\')), ".build.info"));
        string[] array = source[0].Split('|');
        int index1 = Array.FindIndex<string>(array, (Predicate<string>) (h => h.StartsWith("Tags!")));
        int index2 = Array.FindIndex<string>(array, (Predicate<string>) (h => h.StartsWith("Product!")));
        foreach (string str2 in ((IEnumerable<string>) source).Skip<string>(1))
        {
          string[] strArray = str2.Split('|');
          if (index1 >= 0 && index1 < strArray.Length && (index2 < 0 || index2 >= strArray.Length || !(strArray[index2] != (str1 ?? "wow_classic_beta"))))
          {
            foreach (Match match in Regex.Matches(strArray[index1], "\\b([a-z]{2}[A-Z]{2}) (text|speech)\\?").Cast<Match>())
              (match.Groups[2].Value == "text" ? stringList1 : stringList2).Add(match.Groups[1].Value);
          }
        }
      }
      catch
      {
      }
      return Tuple.Create<List<string>, List<string>>(stringList1, stringList2);
    }

    public void EnsureAddons()
    {
      const string resourcePrefix = "ForeverLauncher.AddonAssets/";
      Assembly executingAssembly = Assembly.GetExecutingAssembly();
      string addonsDirectory = Path.GetFullPath(Path.Combine(this.gameDir, "Interface", "AddOns"));
      string addonsPrefix = addonsDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

      foreach (string name in executingAssembly.GetManifestResourceNames().Where<string>((Func<string, bool>) (resourceName => resourceName.StartsWith(resourcePrefix, StringComparison.Ordinal))))
      {
        string relativePath = name.Substring(resourcePrefix.Length).Replace('/', Path.DirectorySeparatorChar);
        string path = Path.GetFullPath(Path.Combine(addonsDirectory, relativePath));
        if (!path.StartsWith(addonsPrefix, StringComparison.OrdinalIgnoreCase))
          continue;

        try
        {
          using (Stream resource = executingAssembly.GetManifestResourceStream(name))
          using (MemoryStream destination = new MemoryStream())
          {
            if (resource == null)
              continue;
            resource.CopyTo(destination);
            byte[] contents = destination.ToArray();
            if (File.Exists(path) && File.ReadAllBytes(path).SequenceEqual<byte>(contents))
              continue;

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, contents);
            string addonName = relativePath.Split(Path.DirectorySeparatorChar)[0];
            this.Say(MessageKind.Dim, "p.addon", (object) addonName);
          }
        }
        catch (Exception ex)
        {
          this.Log("addon " + name + ": " + ex.Message);
        }
      }
    }
    public void Run()
    {
      try
      {
        this.RunCore();
      }
      catch (Exception ex)
      {
        this.Say(MessageKind.Error, "p.error", (object) ex.Message);
        this.Log("----- end (error) " + (object) ex);
        this.Phase(PatchPhase.Failed);
      }
    }

    private List<Process> FindClients()
    {
      List<Process> clients = new List<Process>();
      foreach (Process process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension("Wow.exe")))
      {
        try
        {
          if (string.Equals(Path.GetFullPath(process.MainModule.FileName), this.exePath, StringComparison.OrdinalIgnoreCase))
            clients.Add(process);
        }
        catch
        {
        }
      }
      return clients;
    }

    private static bool Alive(int pid)
    {
      try
      {
        return !Process.GetProcessById(pid).HasExited;
      }
      catch
      {
        return false;
      }
    }

    private void RunCore()
    {
      this.Log("----- start (launcher 2.4.2, server " + this.ServerName + ", portal " + this.Portal + ", slot " + (object) this.TargetGroup + ")");
      string version;
      StatusMessage msg = Patcher.CheckGameDir(this.gameDir, out version);
      if (msg != null)
      {
        this.Say(new StatusMessage(MessageKind.Error, msg.Key, msg.Args));
        this.Phase(PatchPhase.Failed);
      }
      else
      {
        if (version != "1.60.1.70532")
          this.Log("WARNING: build " + version + "; the key store was measured on 1.60.1.70532.");
        StatusMessage m = Patcher.UntestedNotice(version);
        if (m != null)
        {
          this.Log("build " + version + " is not in this launcher's list (" + Patcher.SupportedList() + "): trying it anyway");
          this.Say(m);
        }
        this.clientVersion = version;
        this.Phase(PatchPhase.Starting);
        this.EnsureConfig();
        this.EnsureAddons();
        List<Process> clients = this.FindClients();
        if (!this.StartNew && clients.Count > 1)
        {
          this.Say(MessageKind.Error, "p.multi");
          this.Phase(PatchPhase.Failed);
        }
        else
        {
          Process process;
          if (!this.StartNew && clients.Count == 1)
          {
            process = clients[0];
            this.Say(MessageKind.Info, "p.attached");
          }
          else
          {
            HashSet<int> before = new HashSet<int>(clients.Select<Process, int>((Func<Process, int>) (c => c.Id)));
            this.Say(MessageKind.Info, this.StartNew ? "p.openinganother" : "p.opening");
            Process.Start(new ProcessStartInfo(this.exePath, "-config ForeverLauncher.wtf")
            {
              WorkingDirectory = this.gameDir,
              UseShellExecute = false
            });
            process = (Process) null;
            for (int index = 0; index < 60 && process == null && !this.stopRequested; ++index)
            {
              Thread.Sleep(1000);
              List<Process> list = this.FindClients().Where<Process>((Func<Process, bool>) (c => !before.Contains(c.Id))).ToList<Process>();
              if (list.Count >= 1)
                process = list.OrderBy<Process, DateTime>((Func<Process, DateTime>) (c =>
                {
                  try
                  {
                    return c.StartTime;
                  }
                  catch
                  {
                    return DateTime.MinValue;
                  }
                })).Last<Process>();
            }
            if (process == null)
            {
              this.Say(MessageKind.Error, "p.nostart");
              this.Phase(PatchPhase.Failed);
              return;
            }
          }
          int id = process.Id;
          this.Log("PID " + (object) id);
          this.Phase(PatchPhase.WaitingLogin);
          HashSet<long> source1 = new HashSet<long>();
          bool flag1 = Patcher.UsesCertificates(version);
          this.Log(flag1 ? "key mode: certificates (every copy of the 12 keys)" : "key mode: key store (region group " + (object) this.TargetGroup + ")");
          bool everPatched = false;
          bool flag2 = false;
          bool flag3 = false;
          DateTime dateTime1 = DateTime.MinValue;
          HashSet<string> connections = NativeMethods.TcpConnections(id);
          DateTime dateTime2 = DateTime.MinValue;
          Stopwatch stopwatch1 = Stopwatch.StartNew();
          Stopwatch stopwatch2 = new Stopwatch();
          while (!this.stopRequested)
          {
            if (!Patcher.Alive(id))
            {
              this.Say(MessageKind.Dim, "p.closed");
              this.Log("----- end (game closed)");
              this.Phase(PatchPhase.Closed);
              return;
            }
            HashSet<string> source2 = NativeMethods.TcpConnections(id);
            bool flag4 = source2.Any<string>((Func<string, bool>) (c => !connections.Contains(c)));
            connections = source2;
            if (flag4)
            {
              if (!everPatched && dateTime1 == DateTime.MinValue)
                dateTime1 = DateTime.UtcNow;
              dateTime2 = DateTime.UtcNow.AddSeconds(15.0);
              if (everPatched)
                this.Log("new connection: scanning for a new key store");
            }
            bool flag5 = DateTime.UtcNow < dateTime2;
            bool flag6 = false;
            if (!everPatched || flag5 || stopwatch1.Elapsed.TotalSeconds >= 30.0)
            {
              stopwatch1.Restart();
              if (flag1)
              {
                try
                {
                  flag6 = this.CertPass(id, ref everPatched);
                }
                catch (Exception ex)
                {
                  this.Say(MessageKind.Warn, "p.patchfail", (object) ex.Message);
                }
              }
              else
              {
                foreach (KeyValuePair<long, Patcher.StoreState> store in this.FindStores(id))
                {
                  long key = store.Key;
                  bool flag7 = source1.Add(key);
                  if (flag7)
                    this.Log(string.Format("key store at 0x{0:X} ({1}); {2}", (object) key, (object) store.Value, (object) this.lastScan));
                  if (store.Value == Patcher.StoreState.Patched)
                  {
                    if (flag7 && !everPatched)
                      this.Say(MessageKind.Good, "p.already");
                    everPatched = true;
                  }
                  else
                  {
                    this.Say(MessageKind.Info, everPatched ? "p.restored" : "p.found");
                    try
                    {
                      this.Patch(id, key);
                      flag6 = true;
                      everPatched = true;
                    }
                    catch (Exception ex)
                    {
                      this.Say(MessageKind.Warn, "p.patchfail", (object) ex.Message);
                    }
                  }
                }
              }
              if (flag5 && flag6 && !flag1)
                dateTime2 = DateTime.UtcNow.AddSeconds(2.0);
            }
            else if (flag1)
            {
              try
              {
                flag6 = this.CertCheck(id);
              }
              catch (Exception ex)
              {
                this.Say(MessageKind.Warn, "p.patchfail", (object) ex.Message);
              }
            }
            else
            {
              foreach (long array in source1.ToArray<long>())
              {
                switch (this.GetState(id, array))
                {
                  case Patcher.StoreState.Invalid:
                    source1.Remove(array);
                    break;
                  case Patcher.StoreState.Original:
                    this.Say(MessageKind.Info, "p.restored");
                    try
                    {
                      this.Patch(id, array);
                      flag6 = true;
                      break;
                    }
                    catch (Exception ex)
                    {
                      this.Say(MessageKind.Warn, "p.patchfail", (object) ex.Message);
                      break;
                    }
                }
              }
            }
            if (everPatched && !string.IsNullOrEmpty(this.SupportHost) && (!stopwatch2.IsRunning || flag4 || stopwatch2.Elapsed.TotalSeconds >= (this.supportDone ? 60.0 : 5.0)))
            {
              stopwatch2.Restart();
              this.EnsureSupport(id);
            }
            if (!this.fogClock.IsRunning || this.fogClock.Elapsed.TotalSeconds >= (!Patcher.RemoveFog || this.fogPatched.Count <= 0 ? 5.0 : 30.0))
            {
              this.fogClock.Restart();
              try
              {
                this.EnsureFog(id);
              }
              catch (Exception ex)
              {
                this.Log("fog: " + ex.Message);
              }
            }
            if (m != null && !everPatched && !flag3 && dateTime1 != DateTime.MinValue && (DateTime.UtcNow - dateTime1).TotalSeconds >= 20.0)
            {
              this.Say(MessageKind.Error, "p.nostore", (object) version);
              this.Log("no key store found 20 s after the first connection of untested build " + version + "; " + this.lastScan);
              flag3 = true;
            }
            if (flag6 || everPatched && !flag2)
            {
              this.Ready();
              flag2 = true;
            }
            else if (!everPatched && !flag2)
            {
              this.Say(MessageKind.Dim, "p.waiting");
              this.Log("scan: " + this.lastScan);
              flag2 = true;
            }
            Thread.Sleep(!everPatched || flag5 ? 100 : 250);
          }
          this.Log("----- end (launcher closed)");
        }
      }
    }

    private void Ready()
    {
      this.Say(MessageKind.Good, "p.ready");
      this.Phase(PatchPhase.Ready);
    }

    private Patcher.StoreState GetState(int pid, long array)
    {
      byte[] b;
      try
      {
        b = NativeMethods.ReadHeapBlock(pid, array, 480);
      }
      catch
      {
        return Patcher.StoreState.Invalid;
      }
      if (b == null)
        return Patcher.StoreState.Invalid;
      for (int index = 0; index < 12; ++index)
      {
        int startIndex = index * 40;
        if (BitConverter.ToInt32(b, startIndex) != index + 1)
          return Patcher.StoreState.Invalid;
        string str = Patcher.Hex(b, startIndex + 4, 32);
        if (!(index == this.TargetGroup - 1 ? str == Patcher.KnownKeys[index] || str == this.NewKeyHex : str == Patcher.KnownKeys[index]) || (int) b[startIndex + 36] != (int) Patcher.ExpectedFlags[index] || b[startIndex + 37] != (byte) 127 || b[startIndex + 38] != (byte) 0 || b[startIndex + 39] != (byte) 0)
          return Patcher.StoreState.Invalid;
      }
      return !(Patcher.Hex(b, (this.TargetGroup - 1) * 40 + 4, 32) == this.NewKeyHex) ? Patcher.StoreState.Original : Patcher.StoreState.Patched;
    }

    private List<KeyValuePair<long, Patcher.StoreState>> FindStores(int pid)
    {
      Stopwatch stopwatch = Stopwatch.StartNew();
      long[] numArray = NativeMethods.Scan(pid, Patcher.FromHex(Patcher.KnownKeys[0]));
      List<KeyValuePair<long, Patcher.StoreState>> stores = new List<KeyValuePair<long, Patcher.StoreState>>();
      foreach (long num1 in numArray)
      {
        long num2 = num1 - 4L;
        Patcher.StoreState state = this.GetState(pid, num2);
        if (state != Patcher.StoreState.Invalid)
          stores.Add(new KeyValuePair<long, Patcher.StoreState>(num2, state));
      }
      this.lastScan = string.Format("{0:N0} MB in {1} regions, {2:N1} s, {3} copies of the anchor key, {4} valid store(s)", (object) (NativeMethods.LastBytesScanned / 1048576L), (object) NativeMethods.LastRegionsScanned, (object) stopwatch.Elapsed.TotalSeconds, (object) numArray.Length, (object) stores.Count);
      return stores;
    }

    private void Patch(int pid, long array)
    {
      long a = array + (long) ((this.TargetGroup - 1) * 40) + 4L;
      int num = 1;
      while (true)
      {
        try
        {
          NativeMethods.Write32(pid, a, Patcher.FromHex(Patcher.KnownKeys[this.TargetGroup - 1]), Patcher.FromHex(this.NewKeyHex));
          break;
        }
        catch (TargetBytesChangedException)
        {
          if (num >= 20)
            throw;
          else
            Thread.Sleep(300);
        }
        ++num;
      }
      if (this.GetState(pid, array) != Patcher.StoreState.Patched)
        throw new Exception("after writing, the key store does not validate as patched");
      this.Log(string.Format("region group {0} key written at 0x{1:X} (store 0x{2:X})", (object) this.TargetGroup, (object) a, (object) array));
    }

    private bool CertPass(int pid, ref bool everPatched)
    {
      Stopwatch stopwatch = Stopwatch.StartNew();
      byte[][] needles = new byte[Patcher.KnownKeys.Length + 1][];
      for (int index = 0; index < Patcher.KnownKeys.Length; ++index)
        needles[index] = Patcher.FromHex(Patcher.KnownKeys[index]);
      needles[Patcher.KnownKeys.Length] = Patcher.FromHex(this.NewKeyHex);
      List<KeyValuePair<long, int>> keyValuePairList = NativeMethods.ScanAny(pid, needles, 1);
      int num1 = 0;
      int num2 = 0;
      int num3 = 0;
      foreach (KeyValuePair<long, int> keyValuePair in keyValuePairList)
      {
        if (keyValuePair.Value == Patcher.KnownKeys.Length)
        {
          ++num2;
          this.certCopies.Add(keyValuePair.Key);
        }
        else
        {
          ++num1;
          if (this.WriteCertKey(pid, keyValuePair.Key, keyValuePair.Value))
            ++num3;
        }
      }
      this.lastScan = string.Format("{0:N0} MB in {1} regions, {2:N1} s, {3} original key copies, {4} of ours", (object) (NativeMethods.LastBytesScanned / 1048576L), (object) NativeMethods.LastRegionsScanned, (object) stopwatch.Elapsed.TotalSeconds, (object) num1, (object) num2);
      if (num3 > 0)
      {
        if (!everPatched)
          this.Say(MessageKind.Info, "p.found");
        this.Log(string.Format("{0} certificate key copies written; {1}", (object) num3, (object) this.lastScan));
        everPatched = true;
        return true;
      }
      if (!everPatched && num2 > 0 && num1 == 0)
      {
        this.Say(MessageKind.Good, "p.already");
        this.Log("certificate keys already ours; " + this.lastScan);
        everPatched = true;
      }
      return false;
    }

    private bool CertCheck(int pid)
    {
      bool flag = false;
      foreach (long a in this.certCopies.ToArray<long>())
      {
        byte[] b;
        try
        {
          b = NativeMethods.ReadHeapBlock(pid, a, 32);
        }
        catch
        {
          b = (byte[]) null;
        }
        if (b == null)
        {
          this.certCopies.Remove(a);
        }
        else
        {
          string str = Patcher.Hex(b, 0, 32);
          if (!(str == this.NewKeyHex))
          {
            int key = Array.IndexOf<string>(Patcher.KnownKeys, str);
            if (key < 0)
              this.certCopies.Remove(a);
            else if (this.WriteCertKey(pid, a, key))
            {
              this.Log(string.Format("certificate key {0} restored at 0x{1:X}: written again", (object) (key + 1), (object) a));
              flag = true;
            }
          }
        }
      }
      return flag;
    }

    private bool WriteCertKey(int pid, long a, int key)
    {
      try
      {
        NativeMethods.WriteHeap(pid, a, Patcher.FromHex(Patcher.KnownKeys[key]), Patcher.FromHex(this.NewKeyHex));
      }
      catch (TargetBytesChangedException)
      {
        return false;
      }
      this.certCopies.Add(a);
      return true;
    }

    private void EnsureSupport(int pid)
    {
      int num = 0;
      List<string[]> strArrayList = Patcher.SupportPatches(this.SupportHost);
      foreach (string[] strArray in strArrayList)
      {
        if (this.ReplaceHeapString(pid, strArray[0], strArray[1], !this.supportDone))
          ++num;
      }
      if (!this.supportDone && num == strArrayList.Count)
      {
        this.supportDone = true;
        this.Say(MessageKind.Good, "p.support", (object) ("https://" + this.SupportHost));
      }
      string[] strArray1 = Patcher.SupportRegexPatch(this.SupportHost);
      this.ReplaceHeapString(pid, strArray1[0], strArray1[1], false);
    }

    private bool ReplaceHeapString(
      int pid,
      string original,
      string replacement,
      bool checkAlready)
    {
      byte[] bytes = Encoding.ASCII.GetBytes(original + "\0");
      byte[] numArray1 = new byte[bytes.Length];
      Encoding.ASCII.GetBytes(replacement, 0, replacement.Length, numArray1, 0);
      long[] numArray2 = NativeMethods.Scan(pid, bytes, 1);
      foreach (long a in numArray2)
      {
        try
        {
          NativeMethods.WriteHeap(pid, a, bytes, numArray1);
          this.Log(string.Format("support: 0x{0:X} {1} -> {2}", (object) a, (object) original, (object) replacement));
        }
        catch (Exception ex)
        {
          this.Log("support: 0x" + a.ToString("X") + " " + ex.Message);
        }
      }
      if (numArray2.Length > 0)
        return true;
      return checkAlready && NativeMethods.Scan(pid, Encoding.ASCII.GetBytes(replacement + "\0"), 1).Length > 0;
    }

    private void EnsureFog(int pid)
    {
      string h;
      if (!Patcher.FogDensityPallets.TryGetValue(this.clientVersion ?? "", out h))
        h = "=1.60.1.70425";
      else if (h == null)
        h = Patcher.FogDensityPallets["1.60.1.70205"];
      while (h != null && h.StartsWith("="))
        h = Patcher.FogDensityPallets[h.Substring(1)];
      if (h == null)
      {
        if (Patcher.RemoveFog && !this.fogMissingLogged)
          this.Log("fog: no fog data for client build " + this.clientVersion + ", fog left as it is");
        this.fogMissingLogged = true;
      }
      else
      {
        byte[] numArray1 = Patcher.FromHex(h);
        byte[] numArray2 = new byte[numArray1.Length];
        if (Patcher.RemoveFog)
        {
          foreach (long a in NativeMethods.Scan(pid, numArray1))
          {
            NativeMethods.WriteHeap(pid, a, numArray1, numArray2);
            if (!this.fogPatched.Contains(a))
              this.fogPatched.Add(a);
            this.Log(string.Format("fog: density list at 0x{0:X} set to 0", (object) a));
          }
        }
        else
        {
          foreach (long a in this.fogPatched.ToArray())
          {
            try
            {
              NativeMethods.WriteHeap(pid, a, numArray2, numArray1);
              this.Log(string.Format("fog: density list at 0x{0:X} restored", (object) a));
            }
            catch (Exception ex)
            {
              this.Log("fog: 0x" + a.ToString("X") + " not restored: " + ex.Message);
            }
            this.fogPatched.Remove(a);
          }
        }
      }
    }

    private static string Hex(byte[] b, int offset, int count)
    {
      return BitConverter.ToString(b, offset, count).Replace("-", "");
    }

    private static byte[] FromHex(string h)
    {
      byte[] numArray = new byte[h.Length / 2];
      for (int index = 0; index < numArray.Length; ++index)
        numArray[index] = Convert.ToByte(h.Substring(index * 2, 2), 16);
      return numArray;
    }

    private enum StoreState
    {
      Invalid,
      Original,
      Patched,
    }
  }
}
