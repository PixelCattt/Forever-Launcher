using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#nullable disable
namespace ForeverLauncher
{
  public sealed class ServerEntry
  {
    public const string TrinityCorePublicKey = "02596F0D0C061A8B30745988FD72C59E29EC367FB0F341F28E0F08D037BAFC69";
    public string Name = "";
    public string Portal = "";
    public string PublicKey = "02596F0D0C061A8B30745988FD72C59E29EC367FB0F341F28E0F08D037BAFC69";
    public int RegionGroup = 8;
    public int LoginPort = 1119;
    public int WorldPort = 8085;
    public string StatusUrl = "";
    public string Website = "";
    public string SupportHost = "";
    public List<RealmEntry> Realms = new List<RealmEntry>();

    public static List<RealmEntry> DefaultRealms(int worldPort)
    {
      return new List<RealmEntry>()
      {
        new RealmEntry()
        {
          Name = "Normal",
          Port = worldPort,
          Kind = "normal"
        },
        new RealmEntry()
        {
          Name = "PvP",
          Port = worldPort + 10,
          Kind = "pvp"
        },
        new RealmEntry()
        {
          Name = "RP",
          Port = worldPort + 20,
          Kind = "rp"
        },
        new RealmEntry()
        {
          Name = "Hardcore",
          Port = worldPort + 30,
          Kind = "hardcore"
        }
      };
    }

    public string Validate()
    {
      if (string.IsNullOrWhiteSpace(this.Name))
        return "missing \"name\"";
      if (string.IsNullOrWhiteSpace(this.Portal) || this.Portal.Contains(" ") || this.Portal.Contains("/"))
        return "\"portal\" must be a host name";
      if (!Regex.IsMatch(this.PublicKey ?? "", "^[0-9A-Fa-f]{64}$"))
        return "\"publicKey\" must be 64 hex characters";
      if (this.RegionGroup < 2 || this.RegionGroup > 12 || this.RegionGroup == 9)
        return "\"regionGroup\" must be 2-12 (not 9)";
      if (!string.IsNullOrEmpty(this.StatusUrl) && !this.StatusUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        return "\"statusUrl\" must start with https://";
      if (!string.IsNullOrEmpty(this.SupportHost) && !Regex.IsMatch(this.SupportHost, "^[A-Za-z0-9.-]+$"))
        return "\"supportHost\" must be a host name (no https://, port or path)";
      return !string.IsNullOrEmpty(this.SupportHost) && this.SupportHost.Length > Patcher.SupportHostMaxLength ? "\"supportHost\" is too long (at most " + (object) Patcher.SupportHostMaxLength + " characters)" : (string) null;
    }
  }
}
