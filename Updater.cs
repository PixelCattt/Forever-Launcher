using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Net;
using System.Text;
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
        private static string NewPath => Path.Combine(Settings.Dir, "update.new");
        private static string InstallNewPath => ExePath + ".new";
        private static string OldPath => ExePath + ".old";

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
            Directory.CreateDirectory(Settings.Dir);
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

        public static void InstallAndRestart(string version, bool canWriteHere)
        {
            if (!canWriteHere)
            {
                RequestElevatedInstall(version);
                return;
            }

            try { if (File.Exists(OldPath)) File.Delete(OldPath); } catch { }
            File.Copy(NewPath, InstallNewPath, true);
            File.Move(ExePath, OldPath);
            try { File.Move(InstallNewPath, ExePath); }
            catch { File.Move(OldPath, ExePath); throw; }
            Process.Start(new ProcessStartInfo(ExePath, UpdatedArg + " " + version)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(ExePath)
            });
        }

        private static void RequestElevatedInstall(string version)
        {
            string script =
                "$ErrorActionPreference = 'Stop'\n" +
                "$exe = " + PowerShellQuote(ExePath) + "\n" +
                "$staged = " + PowerShellQuote(NewPath) + "\n" +
                "$version = " + PowerShellQuote(version) + "\n" +
                "$log = " + PowerShellQuote(Settings.LogFile) + "\n" +
                "$old = $exe + '.old'\n" +
                "$next = $exe + '.new'\n" +
                "try {\n" +
                "  $parent = Get-Process -Id " + Process.GetCurrentProcess().Id + " -ErrorAction SilentlyContinue\n" +
                "  if ($parent) { $parent.WaitForExit() }\n" +
                "  if (Test-Path -LiteralPath $old) { Remove-Item -LiteralPath $old -Force }\n" +
                "  Move-Item -LiteralPath $exe -Destination $old\n" +
                "  try {\n" +
                "    Copy-Item -LiteralPath $staged -Destination $next -Force\n" +
                "    Move-Item -LiteralPath $next -Destination $exe\n" +
                "  } catch {\n" +
                "    if (Test-Path -LiteralPath $old) { Move-Item -LiteralPath $old -Destination $exe -Force }\n" +
                "    throw\n" +
                "  }\n" +
                "  Start-Process -FilePath $exe -ArgumentList @('--updated', $version) -WorkingDirectory (Split-Path -Parent $exe)\n" +
                "  Remove-Item -LiteralPath $old -Force -ErrorAction SilentlyContinue\n" +
                "  Remove-Item -LiteralPath $staged -Force -ErrorAction SilentlyContinue\n" +
                "} catch {\n" +
                "  New-Item -ItemType Directory -Path (Split-Path -Parent $log) -Force | Out-Null\n" +
                "  Add-Content -LiteralPath $log -Value ('[' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss') + '] Elevated update failed: ' + $_.Exception.Message)\n" +
                "  exit 1\n" +
                "}\n";
            string encodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            string powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
            Process.Start(new ProcessStartInfo(powershell, "-NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -EncodedCommand " + encodedScript)
            {
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }

        private static string PowerShellQuote(string value)
        {
            return "'" + value.Replace("'", "''") + "'";
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
                        if (File.Exists(InstallNewPath)) File.Delete(InstallNewPath);
                        if (File.Exists(NewPath)) File.Delete(NewPath);
                        break;
                    }
                    catch { await Task.Delay(500); }
                }
            });
        }

        public static bool CanWriteHere()
        {
            string probe = Path.Combine(Path.GetDirectoryName(ExePath), ".update-probe-" + Guid.NewGuid().ToString("N"));
            try
            {
                File.WriteAllText(probe, "");
                File.Delete(probe);
                return true;
            }
            catch { return false; }
            finally
            {
                try { if (File.Exists(probe)) File.Delete(probe); } catch { }
            }
        }
    }
}
