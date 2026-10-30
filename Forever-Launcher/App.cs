using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

#nullable disable
namespace ForeverLauncher
{
  public static class App
  {
    public const string Version = "2.4.2";
    public const string Title = "Forever Launcher";
    public const string ReleasesUrl = "https://github.com/PixelCattt/Forever-Launcher/releases/latest";
    public const string DiscordUrl = "https://discord.gg/gRjgsqXNje";
    public static string UpdatedTo;

    [STAThread]
    public static void Main()
    {
      string[] commandLineArgs = Environment.GetCommandLineArgs();
      int num1 = Array.IndexOf<string>(commandLineArgs, "--updated");
      if (num1 > 0)
        App.UpdatedTo = num1 + 1 < commandLineArgs.Length ? commandLineArgs[num1 + 1] : "2.4.2";
      bool createdNew;
      using (Mutex mutex = new Mutex(true, "ForeverLauncher-single", out createdNew))
      {
        if (!createdNew)
        {
          if (App.UpdatedTo != null)
          {
            try
            {
              createdNew = mutex.WaitOne(TimeSpan.FromSeconds(30.0));
            }
            catch (AbandonedMutexException)
            {
              createdNew = true;
            }
          }
        }
        if (!createdNew)
        {
          Localization.Load();
          int num2 = (int) MessageBox.Show(Localization.Get("app.already"), "Forever Launcher");
        }
        else
        {
          Localization.Load();
          ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
          AppDomain.CurrentDomain.UnhandledException += (UnhandledExceptionEventHandler) ((s, e) => App.Crash(e.ExceptionObject as Exception));
          if (Settings.LoadSoftwareRendering())
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
          Application app = new Application()
          {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
          };
          app.DispatcherUnhandledException += (DispatcherUnhandledExceptionEventHandler) ((s, e) =>
          {
            App.Crash(e.Exception);
            e.Handled = true;
            app.Shutdown();
          });
          LauncherWindow launcherWindow = new LauncherWindow(app);
          try
          {
            app.Run(launcherWindow.Window);
          }
          catch (Exception ex)
          {
            if (!App.IsRenderFailure(ex))
            {
              throw;
            }
            else
            {
              App.Log(ex);
              Settings.SaveSoftwareRendering();
            }
          }
        }
      }
    }

    private static bool IsRenderFailure(Exception ex)
    {
      for (Exception exception = ex; exception != null; exception = exception.InnerException)
      {
        string str = exception.StackTrace ?? "";
        if ((exception is OutOfMemoryException || exception is COMException) && str.Contains("System.Windows.Media.MediaContext") || exception.Message.Contains("UCEERR_RENDERTHREADFAILURE"))
          return true;
      }
      return false;
    }

    private static void Log(Exception ex)
    {
      try
      {
        Directory.CreateDirectory(Path.GetDirectoryName(App.CrashFile));
        System.IO.File.AppendAllText(App.CrashFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " v2.4.2" + Environment.NewLine + (object) ex + Environment.NewLine + Environment.NewLine);
      }
      catch
      {
      }
    }

    public static string CrashFile => Path.Combine(Settings.Dir, "crash.txt");

    private static void Crash(Exception ex)
    {
      App.Log(ex);
      try
      {
        if (App.IsRenderFailure(ex))
        {
          bool flag = Settings.LoadSoftwareRendering();
          Settings.SaveSoftwareRendering();
          int num = (int) MessageBox.Show(Localization.Get(flag ? "app.crash" : "app.renderfail", (object) App.CrashFile), "Forever Launcher", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
        else
        {
          int num1 = (int) MessageBox.Show(Localization.Get("app.crash", (object) App.CrashFile), "Forever Launcher", MessageBoxButton.OK, MessageBoxImage.Hand);
        }
      }
      catch
      {
      }
    }
  }
}
