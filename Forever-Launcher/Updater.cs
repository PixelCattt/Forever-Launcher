using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace ForeverLauncher
{
    public static class Updater
    {
        public const string Repo = "PixelCattt/Forever-Launcher";
        public const string ExeAsset = "Forever-Launcher.exe";
        public const string UpdatedArg = "--updated";
        private const string LatestReleaseApi = "https://api.github.com/repos/PixelCattt/Forever-Launcher/releases/latest";

        public static string ExePath => Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
        private static string NewPath => ExePath + ".new";
        private static string OldPath => ExePath + ".old";

        public static bool AutoUpdate
        {
            get
            {
                try { return File.ReadAllText(Path.Combine(Settings.Dir, "autoupdate.txt")).Trim() != "0"; }
                catch { return true; }
            }
        }

        private static WebClient Client()
        {
            WebClient client = new WebClient();
            client.Headers[HttpRequestHeader.UserAgent] = "Forever-Launcher/" + App.Version;
            client.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
            return client;
        }

        public static LauncherRelease Latest()
        {
            try
            {
                string json;
                using (WebClient client = Client())
                    json = client.DownloadString(LatestReleaseApi);
                Dictionary<string, object> release = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
                string tag = release.ContainsKey("tag_name") ? release["tag_name"] as string : null;
                Match match = Regex.Match(tag ?? "", "^v(\\d+\\.\\d+\\.\\d+)$");
                if (!match.Success)
                    return null;

                LauncherRelease result = new LauncherRelease();
                result.Version = match.Groups[1].Value;
                result.PageUrl = release.ContainsKey("html_url") ? release["html_url"] as string : null;
                ArrayList assets = release.ContainsKey("assets") ? release["assets"] as ArrayList : null;
                if (assets != null)
                {
                    foreach (object value in assets)
                    {
                        Dictionary<string, object> asset = value as Dictionary<string, object>;
                        if (asset == null || !(asset.ContainsKey("name") && string.Equals(asset["name"] as string, ExeAsset, StringComparison.Ordinal)))
                            continue;
                        result.ExeUrl = asset.ContainsKey("browser_download_url") ? asset["browser_download_url"] as string : null;
                        result.Sha256 = asset.ContainsKey("digest") ? asset["digest"] as string : null;
                        break;
                    }
                }
                return string.IsNullOrEmpty(result.ExeUrl) ? null : result;
            }
            catch { return null; }
        }

        public static bool IsNewer(LauncherRelease release)
        {
            Version remote, local;
            return release != null && Version.TryParse(release.Version, out remote) && Version.TryParse(App.Version, out local) && remote > local;
        }

        public static async Task Download(LauncherRelease release, Action<int> progress)
        {
            byte[] executable;
            using (WebClient client = Client())
            {
                client.DownloadProgressChanged += (s, e) => progress(e.ProgressPercentage);
                executable = await client.DownloadDataTaskAsync(release.ExeUrl);
            }
            if (!string.IsNullOrEmpty(release.Sha256))
            {
                string[] digest = release.Sha256.Split(':');
                if (digest.Length != 2 || !string.Equals(digest[0], "sha256", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("unsupported release asset digest");
                using (SHA256 sha = SHA256.Create())
                {
                    string actual = BitConverter.ToString(sha.ComputeHash(executable)).Replace("-", "").ToLowerInvariant();
                    if (!string.Equals(actual, digest[1], StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("release asset SHA-256 mismatch");
                }
            }
            File.WriteAllBytes(NewPath, executable);
        }

        public static void InstallAndRestart(string version)
        {
            try { if (File.Exists(OldPath)) File.Delete(OldPath); } catch { }
            File.Move(ExePath, OldPath);
            try { File.Move(NewPath, ExePath); }
            catch { File.Move(OldPath, ExePath); throw; }
            Process.Start(new ProcessStartInfo(ExePath, UpdatedArg + " " + version)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(ExePath)
            });
        }

        public static void Cleanup()
        {
            Task.Run(async () =>
            {
                for (int i = 0; i < 20; i++)
                {
                    try
                    {
                        if (File.Exists(OldPath)) File.Delete(OldPath);
                        if (!File.Exists(NewPath)) break;
                        File.Delete(NewPath);
                        break;
                    }
                    catch { await Task.Delay(500); }
                }
            });
        }

        public static bool CanWriteHere()
        {
            try
            {
                string probe = Path.Combine(Path.GetDirectoryName(ExePath), ".update-probe");
                File.WriteAllText(probe, "");
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }
    }
}
