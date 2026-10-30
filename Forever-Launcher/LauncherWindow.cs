using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

#nullable disable
namespace ForeverLauncher
{
  public sealed class LauncherWindow
  {
    public readonly Window Window;
    private readonly System.Windows.Application app;
    private readonly Dispatcher ui;
    private System.Windows.Controls.CheckBox noFogBox;
    private System.Windows.Controls.Button playButton;
    private System.Windows.Controls.Button folderButton;
    private System.Windows.Controls.Button logButton;
    private System.Windows.Controls.Button updateLink;
    private System.Windows.Controls.Button languageButton;
    private System.Windows.Controls.Button serverButton;
    private System.Windows.Controls.Button editServersLink;
    private System.Windows.Controls.Button reloadServersLink;
    private System.Windows.Controls.Button websiteLink;
    private TextBlock statusText;
    private TextBlock detailText;
    private TextBlock pingText;
    private TextBlock messageText;
    private TextBlock stepText;
    private TextBlock folderText;
    private TextBlock versionText;
    private TextBlock updateText;
    private TextBlock subtitleText;
    private TextBlock newsHeader;
    private TextBlock loginLabel;
    private TextBlock newsPlaceholder;
    private TextBlock titleText;
    private TextBlock launcherTitleText;
    private TextBlock serverLabel;
    private Ellipse statusDot;
    private Ellipse statusHalo;
    private Ellipse loginDot;
    private UniformGrid realmGrid;
    private StackPanel newsList;
    private WrapPanel linksPanel;
    private Border updateBanner;
    private Canvas stars;
    private string gameDir;
    private readonly List<Patcher> patchers = new List<Patcher>();
    private bool gameRunning;
    private NotifyIcon tray;
    private string updateUrl = App.ReleasesUrl;
    private LauncherWindow.UpdateState updateState;
    private LauncherRelease launcherUpdate;
    private int updatePercent;
    private string updateError;
    private ServerStatus lastStatus;
    private ServerEntry server;
    private StatusMessage lastStep;
    private static readonly System.Windows.Media.Color Green = System.Windows.Media.Color.FromRgb((byte) 92, (byte) 214, (byte) 122);
    private static readonly System.Windows.Media.Color Red = System.Windows.Media.Color.FromRgb((byte) 224, (byte) 90, (byte) 78);
    private static readonly System.Windows.Media.Color Amber = System.Windows.Media.Color.FromRgb((byte) 240, (byte) 178, (byte) 62);
    private static readonly System.Windows.Media.Color Grey = System.Windows.Media.Color.FromRgb((byte) 138, (byte) 138, (byte) 138);

    public LauncherWindow(System.Windows.Application app)
    {
      this.app = app;
      using (Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ForeverLauncher.MainWindow.xaml"))
        this.Window = (Window) XamlReader.Load(manifestResourceStream);
      this.ui = this.Window.Dispatcher;
      try
      {
        this.Window.Icon = (ImageSource) System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(LauncherWindow.AppIcon().Handle, Int32Rect.Empty, (BitmapSizeOptions) null);
      }
      catch
      {
      }
      this.playButton = this.Find<System.Windows.Controls.Button>("PlayButton");
      this.folderButton = this.Find<System.Windows.Controls.Button>("FolderButton");
      this.logButton = this.Find<System.Windows.Controls.Button>("LogButton");
      this.updateLink = this.Find<System.Windows.Controls.Button>("UpdateLink");
      this.statusText = this.Find<TextBlock>("StatusText");
      this.detailText = this.Find<TextBlock>("DetailText");
      this.pingText = this.Find<TextBlock>("PingText");
      this.messageText = this.Find<TextBlock>("MessageText");
      this.stepText = this.Find<TextBlock>("StepText");
      this.folderText = this.Find<TextBlock>("FolderText");
      this.versionText = this.Find<TextBlock>("VersionText");
      this.updateText = this.Find<TextBlock>("UpdateText");
      this.statusDot = this.Find<Ellipse>("StatusDot");
      this.statusHalo = this.Find<Ellipse>("StatusHalo");
      this.loginDot = this.Find<Ellipse>("LoginDot");
      this.realmGrid = this.Find<UniformGrid>("RealmGrid");
      this.newsList = this.Find<StackPanel>("NewsList");
      this.linksPanel = this.Find<WrapPanel>("LinksPanel");
      this.updateBanner = this.Find<Border>("UpdateBanner");
      this.stars = this.Find<Canvas>("Stars");
      this.subtitleText = this.Find<TextBlock>("SubtitleText");
      this.newsHeader = this.Find<TextBlock>("NewsHeader");
      this.loginLabel = this.Find<TextBlock>("LoginLabel");
      this.newsPlaceholder = this.Find<TextBlock>("NewsPlaceholder");
      this.languageButton = this.Find<System.Windows.Controls.Button>("LanguageButton");
      this.titleText = this.Find<TextBlock>("TitleText");
      this.launcherTitleText = this.Find<TextBlock>("LauncherTitleText");
      this.serverLabel = this.Find<TextBlock>("ServerLabel");
      this.serverButton = this.Find<System.Windows.Controls.Button>("ServerButton");
      this.editServersLink = this.Find<System.Windows.Controls.Button>("EditServersLink");
      this.reloadServersLink = this.Find<System.Windows.Controls.Button>("ReloadServersLink");
      this.websiteLink = this.Find<System.Windows.Controls.Button>("WebsiteLink");
      this.serverButton.Click += (RoutedEventHandler) ((s, e) => this.ShowServerMenu());
      this.editServersLink.Click += (RoutedEventHandler) ((s, e) => this.EditServers());
      this.reloadServersLink.Click += (RoutedEventHandler) ((s, e) => this.LoadServers());
      this.websiteLink.Click += (RoutedEventHandler) ((s, e) =>
      {
        if (this.server == null)
          return;
        LauncherWindow.OpenUrl(this.server.Website);
      });
      System.Windows.Controls.Button button = this.Find<System.Windows.Controls.Button>("DiscordButton");
      button.Click += (RoutedEventHandler) ((s, e) => LauncherWindow.OpenUrl("https://discord.gg/gRjgsqXNje"));
      button.ToolTip = (object) "https://discord.gg/gRjgsqXNje";
      this.languageButton.Click += (RoutedEventHandler) ((s, e) => this.ShowLanguageMenu());
      this.launcherTitleText.Text = "FOREVER LAUNCHER v" + App.Version;
      this.Find<Grid>("TitleBar").MouseLeftButtonDown += (MouseButtonEventHandler) ((s, e) =>
      {
        if (e.ButtonState != MouseButtonState.Pressed)
          return;
        this.Window.DragMove();
      });
      this.Find<System.Windows.Controls.Button>("MinButton").Click += (RoutedEventHandler) ((s, e) => this.Window.WindowState = WindowState.Minimized);
      this.Find<System.Windows.Controls.Button>("CloseButton").Click += (RoutedEventHandler) ((s, e) => this.OnCloseClicked());
      this.playButton.Click += (RoutedEventHandler) ((s, e) => this.Play());
      this.folderButton.Click += (RoutedEventHandler) ((s, e) => this.PickFolder());
      this.noFogBox = this.Find<System.Windows.Controls.CheckBox>("NoFogBox");
      Patcher.RemoveFog = Settings.LoadRemoveFog();
      this.noFogBox.IsChecked = new bool?(Patcher.RemoveFog);
      this.noFogBox.Click += (RoutedEventHandler) ((s, e) =>
      {
        bool? isChecked = this.noFogBox.IsChecked;
        Patcher.RemoveFog = isChecked.GetValueOrDefault() && isChecked.HasValue;
        Settings.SaveRemoveFog(Patcher.RemoveFog);
      });
      this.logButton.Click += (RoutedEventHandler) ((s, e) => this.OpenLog());
      this.updateLink.Click += (RoutedEventHandler) ((s, e) =>
      {
        if (this.updateState == LauncherWindow.UpdateState.Available && Updater.CanWriteHere())
          this.RunLauncherUpdate();
        else
          LauncherWindow.OpenUrl(this.launcherUpdate == null || this.launcherUpdate.PageUrl == null ? this.updateUrl : this.launcherUpdate.PageUrl);
      });
      this.Window.Closing += (CancelEventHandler) ((s, e) =>
      {
        if (!this.gameRunning)
          return;
        e.Cancel = true;
        this.HideToTray();
      });
      this.Window.Closed += (EventHandler) ((s, e) => this.Shutdown());
      this.DrawStars();
      this.PulseHalo();
      this.LoadServers();
      this.ApplyLanguage();
      this.SetGameDir(Settings.LoadGameDir() ?? GameLocator.Find(), false);
      DispatcherTimer timer = new DispatcherTimer()
      {
        Interval = TimeSpan.FromSeconds(60.0)
      };
      timer.Tick += (EventHandler) ((s, e) => this.RefreshStatus());
      this.Window.Loaded += (RoutedEventHandler) ((s, e) =>
      {
        timer.Start();
        this.RefreshStatus();
        this.CheckLauncherUpdate();
      });
    }

    private async void CheckLauncherUpdate()
    {
      Updater.Cleanup();
      if (App.UpdatedTo != null)
      {
        this.updateState = LauncherWindow.UpdateState.Done;
        this.RenderLauncherUpdate();
      }
      LauncherRelease r = await Task.Run<LauncherRelease>((Func<LauncherRelease>) (() => Updater.Latest()));
      if (!Updater.IsNewer(r))
        return;
      this.launcherUpdate = r;
      this.updateState = LauncherWindow.UpdateState.Available;
      this.RenderLauncherUpdate();
      if (!Updater.AutoUpdate || !Updater.CanWriteHere())
        return;
      this.RunLauncherUpdate();
    }

    private async void RunLauncherUpdate()
    {
      if (this.updateState == LauncherWindow.UpdateState.Downloading || this.updateState == LauncherWindow.UpdateState.Restarting)
        return;
      this.updateState = LauncherWindow.UpdateState.Downloading;
      this.updatePercent = 0;
      this.RenderLauncherUpdate();
      try
      {
        await Updater.Download(this.launcherUpdate, (Action<int>) (p => this.ui.BeginInvoke((Action) (() =>
        {
          if (this.updateState != LauncherWindow.UpdateState.Downloading)
            return;
          this.updatePercent = p;
          this.RenderLauncherUpdate();
        }))));
        this.updateState = LauncherWindow.UpdateState.Restarting;
        this.RenderLauncherUpdate();
        Updater.InstallAndRestart(this.launcherUpdate.Version);
        this.Shutdown();
      }
      catch (Exception ex)
      {
        this.updateState = LauncherWindow.UpdateState.Failed;
        this.updateError = ex is WebException ? Localization.Get("upd.nonet") : ex.Message;
        this.RenderLauncherUpdate();
      }
    }

    private void RenderLauncherUpdate()
    {
      string str = this.launcherUpdate != null ? this.launcherUpdate.Version : App.UpdatedTo;
      switch (this.updateState)
      {
        case LauncherWindow.UpdateState.None:
          return;
        case LauncherWindow.UpdateState.Available:
          this.updateText.Text = Localization.Get("ui.update", (object) str);
          this.updateLink.Content = (object) Localization.Get("upd.now");
          break;
        case LauncherWindow.UpdateState.Downloading:
          this.updateText.Text = Localization.Get("upd.downloading", (object) str, (object) this.updatePercent);
          break;
        case LauncherWindow.UpdateState.Restarting:
          this.updateText.Text = Localization.Get("upd.restarting");
          break;
        case LauncherWindow.UpdateState.Failed:
          this.updateText.Text = Localization.Get("upd.failed", (object) str, (object) this.updateError);
          this.updateLink.Content = (object) Localization.Get("ui.download");
          break;
        case LauncherWindow.UpdateState.Done:
          this.updateText.Text = Localization.Get("upd.done", (object) str);
          break;
      }
      this.updateLink.Visibility = this.updateState == LauncherWindow.UpdateState.Available || this.updateState == LauncherWindow.UpdateState.Failed ? Visibility.Visible : Visibility.Collapsed;
      this.updateBanner.Visibility = Visibility.Visible;
    }

    private T Find<T>(string name) where T : class => (T) this.Window.FindName(name);

    private void SetLanguage(string lang)
    {
      if (Localization.Lang == lang)
        return;
      Localization.Lang = lang;
      Localization.Save();
      this.ApplyLanguage();
    }

    private void ShowLanguageMenu()
    {
      System.Windows.Controls.ContextMenu menu = new System.Windows.Controls.ContextMenu();
      string[] codes = { "en", "es", "ru", "de" };
      string[] names = { "English", "Español", "Русский", "Deutsch" };
      for (int i = 0; i < codes.Length; i++)
      {
        string code = codes[i];
        System.Windows.Controls.MenuItem item = new System.Windows.Controls.MenuItem();
        item.Header = code.ToUpperInvariant() + "   " + names[i];
        item.IsCheckable = true;
        item.IsChecked = Localization.Lang == code;
        item.Click += (RoutedEventHandler) ((o, e) => this.SetLanguage(code));
        menu.Items.Add(item);
      }
      menu.PlacementTarget = this.languageButton;
      menu.Placement = PlacementMode.Bottom;
      menu.IsOpen = true;
    }

    private void ApplyLanguage()
    {
      System.Windows.Media.Brush resource = (System.Windows.Media.Brush) this.Window.FindResource((object) "Gold");
      SolidColorBrush solidColorBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte) 51, (byte) 232, (byte) 196, (byte) 106));
      this.languageButton.Content = (object) (Localization.Lang.ToUpperInvariant() + "   ▾");
      this.subtitleText.Text = Localization.Get("ui.subtitle");
      this.newsHeader.Text = Localization.Get("ui.news");
      this.loginLabel.Text = Localization.Get("ui.login");
      this.logButton.Content = (object) Localization.Get("ui.viewlog");
      this.serverLabel.Text = Localization.Get("ui.server");
      this.editServersLink.Content = (object) Localization.Get("ui.editservers");
      this.reloadServersLink.Content = (object) Localization.Get("ui.reloadservers");
      this.websiteLink.Content = (object) Localization.Get("ui.website");
      this.updateLink.Content = (object) Localization.Get("ui.download");
      this.RenderLauncherUpdate();
      this.playButton.Content = (object) Localization.Get(this.gameRunning ? "ui.playanother" : "ui.play");
      this.folderButton.Content = (object) Localization.Get(this.gameDir == null ? "ui.pickfolder" : "ui.changefolder");
      this.UpdateFogControl();
      if (this.newsPlaceholder.Parent != null)
        this.newsPlaceholder.Text = Localization.Get(this.lastStatus == null ? "ui.loading" : "ui.newsfail");
      if (this.server == null)
        this.RenderNoServer();
      else if (this.lastStatus == null)
        this.statusText.Text = Localization.Get("status.checking");
      else
        this.RenderStatus(this.lastStatus);
      if (this.lastStep != null)
        this.Step(this.lastStep);
      if (this.tray == null)
        return;
      this.BuildTrayMenu();
    }

    private static Icon AppIcon()
    {
      return Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
    }

    private void DrawStars()
    {
      Random random = new Random(1601);
      for (int index = 0; index < 140; ++index)
      {
        double num = random.NextDouble() < 0.12 ? 2.2 : 1.2 + random.NextDouble() * 0.6;
        Ellipse ellipse = new Ellipse();
        ellipse.Width = num;
        ellipse.Height = num;
        ellipse.Fill = (System.Windows.Media.Brush) System.Windows.Media.Brushes.White;
        ellipse.Opacity = 0.25 + random.NextDouble() * 0.6;
        Ellipse element = ellipse;
        Canvas.SetLeft((UIElement) element, random.NextDouble() * 978.0);
        Canvas.SetTop((UIElement) element, Math.Pow(random.NextDouble(), 1.6) * 330.0);
        if (index % 5 == 0)
        {
          DoubleAnimation doubleAnimation = new DoubleAnimation(element.Opacity, 0.1, (Duration) TimeSpan.FromSeconds(1.5 + random.NextDouble() * 3.0));
          doubleAnimation.AutoReverse = true;
          doubleAnimation.RepeatBehavior = RepeatBehavior.Forever;
          doubleAnimation.BeginTime = new TimeSpan?(TimeSpan.FromSeconds(random.NextDouble() * 4.0));
          DoubleAnimation animation = doubleAnimation;
          element.BeginAnimation(UIElement.OpacityProperty, (AnimationTimeline) animation);
        }
        this.stars.Children.Add((UIElement) element);
      }
    }

    private void PulseHalo()
    {
      ScaleTransform scaleTransform = new ScaleTransform(1.0, 1.0);
      this.statusHalo.RenderTransform = (Transform) scaleTransform;
      DoubleAnimation doubleAnimation1 = new DoubleAnimation(1.0, 2.1, (Duration) TimeSpan.FromSeconds(1.8));
      doubleAnimation1.RepeatBehavior = RepeatBehavior.Forever;
      DoubleAnimation doubleAnimation2 = doubleAnimation1;
      QuadraticEase quadraticEase1 = new QuadraticEase();
      quadraticEase1.EasingMode = EasingMode.EaseOut;
      QuadraticEase quadraticEase2 = quadraticEase1;
      doubleAnimation2.EasingFunction = (IEasingFunction) quadraticEase2;
      DoubleAnimation animation1 = doubleAnimation1;
      scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, (AnimationTimeline) animation1);
      scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, (AnimationTimeline) animation1);
      DoubleAnimation doubleAnimation3 = new DoubleAnimation(0.5, 0.0, (Duration) TimeSpan.FromSeconds(1.8));
      doubleAnimation3.RepeatBehavior = RepeatBehavior.Forever;
      DoubleAnimation animation2 = doubleAnimation3;
      this.statusHalo.BeginAnimation(UIElement.OpacityProperty, (AnimationTimeline) animation2);
    }

    private async void RefreshStatus()
    {
      ServerEntry target = this.server;
      if (target == null)
      {
        this.RenderNoServer();
      }
      else
      {
        ServerStatus s;
        try
        {
          s = await StatusClient.FetchAsync(target);
        }
        catch
        {
          return;
        }
        if (target != this.server)
          return;
        if (s.FeedOk || this.lastStatus == null || !this.lastStatus.FeedOk)
        {
          this.lastStatus = s;
        }
        else
        {
          s.FeedOk = true;
          s.Root = this.lastStatus.Root;
          s.News = this.lastStatus.News;
          s.Links = this.lastStatus.Links;
          s.LatestLauncher = this.lastStatus.LatestLauncher;
          s.LauncherUrl = this.lastStatus.LauncherUrl;
          this.lastStatus = s;
        }
        this.RenderStatus(s);
      }
    }

    private void RenderStatus(ServerStatus s)
    {
      System.Windows.Media.Color c;
      string key;
      if (s.LoginUp && s.WorldUp)
      {
        c = LauncherWindow.Green;
        key = "status.online";
      }
      else if (s.LoginUp)
      {
        c = LauncherWindow.Amber;
        key = "status.worlddown";
      }
      else if (s.FeedFresh && s.FeedLoginUp)
      {
        c = LauncherWindow.Amber;
        key = "status.unreachable";
      }
      else if (s.Maintenance)
      {
        c = LauncherWindow.Amber;
        key = "status.maintenance";
      }
      else
      {
        c = LauncherWindow.Red;
        key = "status.offline";
      }
      LauncherWindow.SetDot((Shape) this.statusDot, c);
      LauncherWindow.SetDot((Shape) this.statusHalo, c);
      this.statusText.Text = Localization.Get(key);
      LauncherWindow.SetDot((Shape) this.loginDot, s.LoginUp ? LauncherWindow.Green : LauncherWindow.Red);
      this.FillRealms(s);
      this.pingText.Text = s.LoginMs >= 0L ? s.LoginMs.ToString() + " ms" : "";
      this.detailText.Text = s.LoginUp || s.WorldUp ? " " : Localization.Get("status.noresponse");
      string str = s.Root.Get("message") ?? "";
      this.messageText.Text = str;
      this.messageText.Visibility = string.IsNullOrWhiteSpace(str) ? Visibility.Collapsed : Visibility.Visible;
      if (s.FeedOk)
      {
        this.FillNews(s);
        this.FillLinks(s);
      }
      else if (string.IsNullOrEmpty(this.server.StatusUrl))
        this.ShowNewsText(Localization.Get("ui.nofeed"));
      else
        this.ShowNewsText(Localization.Get("ui.newsfail"));
      if (this.updateState != LauncherWindow.UpdateState.None || !StatusClient.IsNewer(s.LatestLauncher, App.Version))
        return;
      this.updateText.Text = Localization.Get("ui.update", (object) s.LatestLauncher);
      if (!string.IsNullOrEmpty(s.LauncherUrl))
        this.updateUrl = s.LauncherUrl;
      this.updateBanner.Visibility = Visibility.Visible;
    }

    private void FillRealms(ServerStatus s)
    {
      this.realmGrid.Children.Clear();
      System.Windows.Media.Brush resource = (System.Windows.Media.Brush) this.Window.FindResource((object) "Dim");
      foreach (RealmStatus realm in s.Realms)
      {
        Border border1 = new Border();
        border1.Width = 3.0;
        border1.CornerRadius = new CornerRadius(2.0);
        border1.Margin = new Thickness(0.0, 0.0, 10.0, 0.0);
        border1.Background = (System.Windows.Media.Brush) new SolidColorBrush(LauncherWindow.RealmColor(realm.Realm.Kind));
        Border element1 = border1;
        Ellipse ellipse1 = new Ellipse();
        ellipse1.Width = 8.0;
        ellipse1.Height = 8.0;
        ellipse1.VerticalAlignment = VerticalAlignment.Center;
        ellipse1.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
        Ellipse ellipse2 = ellipse1;
        LauncherWindow.SetDot((Shape) ellipse2, realm.Up ? LauncherWindow.Green : LauncherWindow.Red);
        StackPanel element2 = new StackPanel()
        {
          Orientation = System.Windows.Controls.Orientation.Horizontal
        };
        element2.Children.Add((UIElement) ellipse2);
        element2.Children.Add((UIElement) new TextBlock()
        {
          Text = realm.Realm.Name,
          FontSize = 14.0,
          FontWeight = FontWeights.SemiBold,
          TextTrimming = TextTrimming.CharacterEllipsis
        });
        StackPanel stackPanel = new StackPanel();
        stackPanel.VerticalAlignment = VerticalAlignment.Center;
        StackPanel element3 = stackPanel;
        element3.Children.Add((UIElement) element2);
        UIElementCollection children1 = element3.Children;
        TextBlock textBlock = new TextBlock();
        textBlock.Text = Localization.Get(realm.Up ? "realm.online" : "realm.offline") + (realm.Up ? "  ·  " + (object) realm.Ms + " ms" : "");
        textBlock.FontSize = 11.5;
        textBlock.Foreground = realm.Up ? (System.Windows.Media.Brush) new SolidColorBrush(LauncherWindow.Green) : resource;
        textBlock.Margin = new Thickness(16.0, 2.0, 0.0, 0.0);
        TextBlock element4 = textBlock;
        children1.Add((UIElement) element4);
        DockPanel dockPanel = new DockPanel();
        DockPanel.SetDock((UIElement) element1, Dock.Left);
        dockPanel.Children.Add((UIElement) element1);
        dockPanel.Children.Add((UIElement) element3);
        UIElementCollection children2 = this.realmGrid.Children;
        Border border2 = new Border();
        border2.Child = (UIElement) dockPanel;
        border2.CornerRadius = new CornerRadius(6.0);
        border2.Padding = new Thickness(8.0, 7.0, 10.0, 7.0);
        border2.Margin = new Thickness(0.0, 6.0, 6.0, 0.0);
        border2.Background = (System.Windows.Media.Brush) new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte) 64, (byte) 0, (byte) 0, (byte) 0));
        border2.BorderThickness = new Thickness(1.0);
        border2.BorderBrush = (System.Windows.Media.Brush) new SolidColorBrush(System.Windows.Media.Color.FromArgb(realm.Up ? (byte) 85 : (byte) 34, (byte) 232, (byte) 196, (byte) 106));
        border2.ToolTip = (object) (this.server.Portal + ":" + (object) realm.Realm.Port);
        Border element5 = border2;
        children2.Add((UIElement) element5);
      }
    }

    private static System.Windows.Media.Color RealmColor(string kind)
    {
      switch (kind)
      {
        case "pvp":
          return System.Windows.Media.Color.FromRgb((byte) 208, (byte) 74, (byte) 60);
        case "rp":
          return System.Windows.Media.Color.FromRgb((byte) 92, (byte) 184, (byte) 138);
        case "hardcore":
          return System.Windows.Media.Color.FromRgb((byte) 154, (byte) 106, (byte) 224);
        default:
          return System.Windows.Media.Color.FromRgb((byte) 232, (byte) 196, (byte) 106);
      }
    }

    private void RenderNoServer()
    {
      this.lastStatus = (ServerStatus) null;
      LauncherWindow.SetDot((Shape) this.statusDot, LauncherWindow.Grey);
      LauncherWindow.SetDot((Shape) this.statusHalo, LauncherWindow.Grey);
      this.statusText.Text = Localization.Get("status.noserver");
      LauncherWindow.SetDot((Shape) this.loginDot, LauncherWindow.Grey);
      this.realmGrid.Children.Clear();
      this.pingText.Text = "";
      TextBlock detailText = this.detailText;
      string str;
      if (Servers.Errors.Count <= 0)
        str = Localization.Get("srv.none");
      else
        str = Localization.Get("srv.errors", (object) string.Join("; ", Servers.Errors.ToArray()));
      detailText.Text = str;
      this.messageText.Visibility = Visibility.Collapsed;
      this.ShowNewsText("");
      this.linksPanel.Children.Clear();
    }

    private void ShowNewsText(string text)
    {
      this.newsList.Children.Clear();
      if (string.IsNullOrEmpty(text))
        return;
      this.newsList.Children.Add((UIElement) new TextBlock()
      {
        Text = text,
        Foreground = (System.Windows.Media.Brush) this.Window.FindResource((object) "Dim"),
        FontSize = 13.0,
        TextWrapping = TextWrapping.Wrap
      });
    }

    private void LoadServers()
    {
      Servers.Load();
      this.SelectServer(Servers.Find(this.server != null ? this.server.Name : Settings.LoadServer()), false);
      if (Servers.Errors.Count > 0)
        this.Step(new StatusMessage(MessageKind.Warn, "srv.errors", new object[1]
        {
          (object) string.Join("; ", Servers.Errors.ToArray())
        }));
      else if (Servers.List.Count == 0)
        this.Step(new StatusMessage(MessageKind.Warn, "srv.none", new object[0]));
      else
        this.Step(new StatusMessage(MessageKind.Dim, "srv.loaded", new object[2]
        {
          (object) Servers.List.Count,
          (object) Servers.FilePath
        }));
    }

    private void SelectServer(ServerEntry s, bool save)
    {
      this.server = s;
      this.lastStatus = (ServerStatus) null;
      this.titleText.Text = s != null ? s.Name : "Forever Launcher";
      this.serverButton.Content = (object) ((s != null ? s.Name + "  (" + s.Portal + ")" : "-") + "  ▾");
      this.websiteLink.Visibility = s == null || string.IsNullOrEmpty(s.Website) ? Visibility.Collapsed : Visibility.Visible;
      this.updateBanner.Visibility = Visibility.Collapsed;
      if (save && s != null)
        Settings.SaveServer(s.Name);
      if (s == null)
      {
        this.RenderNoServer();
      }
      else
      {
        this.statusText.Text = Localization.Get("status.checking");
        this.ShowNewsText(Localization.Get("ui.loading"));
        this.linksPanel.Children.Clear();
        if (!this.Window.IsLoaded)
          return;
        this.RefreshStatus();
      }
    }

    private void ShowServerMenu()
    {
      System.Windows.Controls.ContextMenu contextMenu = new System.Windows.Controls.ContextMenu();
      foreach (ServerEntry serverEntry in Servers.List)
      {
        ServerEntry entry = serverEntry;
        System.Windows.Controls.MenuItem menuItem = new System.Windows.Controls.MenuItem();
        menuItem.Header = (object) (serverEntry.Name + "   " + serverEntry.Portal);
        menuItem.IsCheckable = true;
        menuItem.IsChecked = serverEntry == this.server;
        System.Windows.Controls.MenuItem newItem = menuItem;
        newItem.Click += (RoutedEventHandler) ((o, e) => this.SelectServer(entry, true));
        contextMenu.Items.Add((object) newItem);
      }
      if (contextMenu.Items.Count > 0)
        contextMenu.Items.Add((object) new Separator());
      System.Windows.Controls.MenuItem menuItem1 = new System.Windows.Controls.MenuItem();
      menuItem1.Header = (object) Localization.Get("ui.editservers");
      System.Windows.Controls.MenuItem newItem1 = menuItem1;
      newItem1.Click += (RoutedEventHandler) ((o, e) => this.EditServers());
      contextMenu.Items.Add((object) newItem1);
      contextMenu.PlacementTarget = (UIElement) this.serverButton;
      contextMenu.Placement = PlacementMode.Bottom;
      contextMenu.IsOpen = true;
    }

    private void UpdateFogControl()
    {
      this.noFogBox.Content = (object) Localization.Get("ui.nofog");
      this.noFogBox.ToolTip = (object) Localization.Get("ui.nofogtip");
      this.noFogBox.Visibility = this.gameDir == null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void EditServers()
    {
      Servers.Load();
      try
      {
        if (string.IsNullOrEmpty(Servers.FilePath) || !File.Exists(Servers.FilePath))
          throw new IOException("The server configuration could not be created.");

        Process.Start(new ProcessStartInfo
        {
          FileName = "notepad.exe",
          Arguments = "\"" + Servers.FilePath + "\"",
          UseShellExecute = false
        });
      }
      catch (Exception ex)
      {
        System.Windows.MessageBox.Show(this.Window, "Could not open servers.json:\n" + ex.Message, "Forever Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
    private static void SetDot(Shape e, System.Windows.Media.Color c)
    {
      e.Fill = (System.Windows.Media.Brush) new SolidColorBrush(c);
    }

    private void FillNews(ServerStatus s)
    {
      this.newsList.Children.Clear();
      if (s.News.Count == 0)
      {
        this.newsList.Children.Add((UIElement) new TextBlock()
        {
          Text = Localization.Get("ui.nonews"),
          Foreground = (System.Windows.Media.Brush) this.Window.FindResource((object) "Dim"),
          FontSize = 13.0
        });
      }
      else
      {
        foreach (FeedEntry feedEntry in s.News)
        {
          string str1 = feedEntry.Get("date");
          string str2 = feedEntry.Get("text");
          string str3 = feedEntry.Get("url");
          StackPanel stackPanel = new StackPanel();
          stackPanel.Margin = new Thickness(0.0, 0.0, 0.0, 16.0);
          StackPanel element1 = stackPanel;
          if (!string.IsNullOrEmpty(str1))
            element1.Children.Add((UIElement) new TextBlock()
            {
              Text = str1,
              FontSize = 11.0,
              Foreground = (System.Windows.Media.Brush) this.Window.FindResource((object) "Dim")
            });
          TextBlock textBlock1 = new TextBlock();
          textBlock1.Text = feedEntry.Get("title") ?? "";
          textBlock1.FontSize = 14.0;
          textBlock1.FontWeight = FontWeights.SemiBold;
          textBlock1.TextWrapping = TextWrapping.Wrap;
          textBlock1.Margin = new Thickness(0.0, 2.0, 0.0, 0.0);
          TextBlock element2 = textBlock1;
          if (!string.IsNullOrEmpty(str3))
          {
            string url = str3;
            element2.Cursor = System.Windows.Input.Cursors.Hand;
            element2.Foreground = (System.Windows.Media.Brush) this.Window.FindResource((object) "Gold");
            element2.MouseLeftButtonUp += (MouseButtonEventHandler) ((o, e) => LauncherWindow.OpenUrl(url));
          }
          element1.Children.Add((UIElement) element2);
          if (!string.IsNullOrEmpty(str2))
          {
            UIElementCollection children = element1.Children;
            TextBlock textBlock2 = new TextBlock();
            textBlock2.Text = str2;
            textBlock2.FontSize = 12.5;
            textBlock2.Foreground = (System.Windows.Media.Brush) new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte) 196, (byte) 187, (byte) 165));
            textBlock2.TextWrapping = TextWrapping.Wrap;
            textBlock2.Margin = new Thickness(0.0, 3.0, 0.0, 0.0);
            textBlock2.LineHeight = 18.0;
            TextBlock element3 = textBlock2;
            children.Add((UIElement) element3);
          }
          this.newsList.Children.Add((UIElement) element1);
        }
      }
    }

    private void FillLinks(ServerStatus s)
    {
      this.linksPanel.Children.Clear();
      foreach (FeedEntry link in s.Links)
      {
        string url = link.Get("url");
        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
          System.Windows.Controls.Button button = new System.Windows.Controls.Button();
          button.Content = (object) link.Get("label");
          button.Style = (Style) this.Window.FindResource((object) "PillButton");
          System.Windows.Controls.Button element = button;
          element.Click += (RoutedEventHandler) ((o, e) => LauncherWindow.OpenUrl(url));
          this.linksPanel.Children.Add((UIElement) element);
        }
      }
    }

    private static void OpenUrl(string url)
    {
      if (string.IsNullOrEmpty(url))
        return;
      if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        return;
      try
      {
        Process.Start(new ProcessStartInfo(url)
        {
          UseShellExecute = true
        });
      }
      catch
      {
      }
    }

    private void SetGameDir(string dir, bool save)
    {
      this.gameDir = dir;
      string version;
      StatusMessage m = Patcher.CheckGameDir(dir, out version);
      this.folderText.Text = dir ?? "";
      this.folderText.ToolTip = (object) dir;
      this.folderButton.Content = (object) Localization.Get(dir == null ? "ui.pickfolder" : "ui.changefolder");
      this.versionText.Text = string.IsNullOrEmpty(version) ? "WoW version unavailable" : "WoW " + version;
      this.UpdateFogControl();
      if (m != null)
      {
        this.Step(m);
      }
      else
      {
        this.Step(Patcher.UntestedNotice(version) ?? new StatusMessage(MessageKind.Info, "ui.ready", new object[0]));
        if (!save)
          return;
        Settings.SaveGameDir(dir);
      }
    }

    private void PickFolder()
    {
      using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
      {
        folderBrowserDialog.Description = Localization.Get("ui.folderdlg");
        folderBrowserDialog.ShowNewFolderButton = false;
        if (this.gameDir != null && Directory.Exists(this.gameDir))
          folderBrowserDialog.SelectedPath = this.gameDir;
        if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
          return;
        string str = folderBrowserDialog.SelectedPath;
        string path1 = System.IO.Path.Combine(str, "_classic_beta_");
        if (!System.IO.File.Exists(System.IO.Path.Combine(str, "Wow.exe")) && System.IO.File.Exists(System.IO.Path.Combine(path1, "Wow.exe")))
          str = path1;
        this.SetGameDir(str, true);
      }
    }

    private void OpenLog()
    {
      if (this.gameDir == null)
        return;
      string str = System.IO.Path.Combine(this.gameDir, "Logs", "launcher.log");
      if (System.IO.File.Exists(str))
      {
        try
        {
          Process.Start(new ProcessStartInfo(str)
          {
            UseShellExecute = true
          });
        }
        catch
        {
        }
      }
      else
        this.Step(new StatusMessage(MessageKind.Dim, "ui.nolog", new object[0]));
    }

    private void Play()
    {
      StatusMessage msg = Patcher.CheckGameDir(this.gameDir, out string _);
      if (msg != null)
      {
        this.Step(new StatusMessage(MessageKind.Error, msg.Key, msg.Args));
        if (System.IO.File.Exists(System.IO.Path.Combine(this.gameDir ?? "", "Wow.exe")))
          return;
        this.PickFolder();
      }
      else
      {
        Settings.SaveGameDir(this.gameDir);
        if (this.server == null)
          this.Step(new StatusMessage(MessageKind.Error, "srv.none", new object[0]));
        else if (this.server.Portal.EndsWith("example.com", StringComparison.OrdinalIgnoreCase))
        {
          this.Step(new StatusMessage(MessageKind.Error, "srv.example", new object[1]
          {
            (object) this.server.Name
          }));
        }
        else
        {
          Patcher patcher = new Patcher(this.gameDir, this.server)
          {
            StartNew = this.gameRunning
          };
          this.gameRunning = true;
          this.playButton.Content = (object) Localization.Get("ui.playanother");
          this.folderButton.IsEnabled = false;
          this.serverButton.IsEnabled = false;
          this.patchers.Add(patcher);
          patcher.Message += (Action<StatusMessage>) (m => this.ui.BeginInvoke((Action) (() => this.Step(m))));
          patcher.PhaseChanged += (Action<PatchPhase>) (p => this.ui.BeginInvoke((Action) (() => this.OnPhase(patcher, p))));
          new Thread(new ThreadStart(patcher.Run))
          {
            IsBackground = true,
            Name = "patcher"
          }.Start();
        }
      }
    }

    private void OnPhase(Patcher patcher, PatchPhase p)
    {
      switch (p)
      {
        case PatchPhase.WaitingLogin:
          this.ShowTray();
          this.Window.WindowState = WindowState.Minimized;
          break;
        case PatchPhase.Ready:
          SystemSounds.Asterisk.Play();
          if (this.tray == null)
            break;
          this.tray.ShowBalloonTip(6000, Localization.Get("tray.readytitle"), Localization.Get("tray.readytext"), ToolTipIcon.Info);
          break;
        case PatchPhase.Closed:
        case PatchPhase.Failed:
          this.patchers.Remove(patcher);
          if (this.patchers.Count > 0)
            break;
          this.gameRunning = false;
          this.playButton.IsEnabled = true;
          this.playButton.Content = (object) Localization.Get("ui.play");
          this.folderButton.IsEnabled = true;
          this.serverButton.IsEnabled = true;
          this.HideTray();
          this.RestoreWindow();
          break;
      }
    }

    private void Step(StatusMessage m)
    {
      this.lastStep = m;
      string str = m.ToString();
      this.stepText.Text = str;
      System.Windows.Media.Color color;
      switch (m.Kind)
      {
        case MessageKind.Good:
          color = LauncherWindow.Green;
          break;
        case MessageKind.Warn:
          color = LauncherWindow.Amber;
          break;
        case MessageKind.Error:
          color = LauncherWindow.Red;
          break;
        case MessageKind.Dim:
          color = System.Windows.Media.Color.FromRgb((byte) 176, (byte) 168, (byte) 148);
          break;
        default:
          color = System.Windows.Media.Color.FromRgb((byte) 237, (byte) 230, (byte) 214);
          break;
      }
      this.stepText.Foreground = (System.Windows.Media.Brush) new SolidColorBrush(color);
      if (this.tray == null)
        return;
      this.tray.Text = LauncherWindow.Truncate("Forever Launcher - " + str, 63);
    }

    private static string Truncate(string s, int n)
    {
      return s.Length > n ? s.Substring(0, n - 1) + "…" : s;
    }

    private void ShowTray()
    {
      if (this.tray != null)
        return;
      this.tray = new NotifyIcon()
      {
        Icon = LauncherWindow.AppIcon(),
        Text = "Forever Launcher",
        Visible = true
      };
      this.BuildTrayMenu();
      this.tray.DoubleClick += (EventHandler) ((s, e) => this.RestoreWindow());
    }

    private void BuildTrayMenu()
    {
      ContextMenuStrip contextMenuStrip1 = new ContextMenuStrip();
      contextMenuStrip1.Items.Add(Localization.Get("tray.open"), (System.Drawing.Image) null, (EventHandler) ((s, e) => this.RestoreWindow()));
      contextMenuStrip1.Items.Add(Localization.Get("tray.exit"), (System.Drawing.Image) null, (EventHandler) ((s, e) => this.ConfirmExit()));
      ContextMenuStrip contextMenuStrip2 = this.tray.ContextMenuStrip;
      this.tray.ContextMenuStrip = contextMenuStrip1;
      contextMenuStrip2?.Dispose();
    }

    private void HideTray()
    {
      if (this.tray == null)
        return;
      this.tray.Visible = false;
      this.tray.Dispose();
      this.tray = (NotifyIcon) null;
    }

    private void HideToTray()
    {
      this.ShowTray();
      this.Window.Hide();
      this.tray.ShowBalloonTip(4000, "Forever Launcher", Localization.Get("tray.hidden"), ToolTipIcon.None);
    }

    private void RestoreWindow()
    {
      this.Window.Show();
      if (this.Window.WindowState == WindowState.Minimized)
        this.Window.WindowState = WindowState.Normal;
      this.Window.Activate();
    }

    private void OnCloseClicked()
    {
      if (this.gameRunning)
        this.HideToTray();
      else
        this.Window.Close();
    }

    private void ConfirmExit()
    {
      if (this.gameRunning && System.Windows.MessageBox.Show(Localization.Get("exit.confirm"), "Forever Launcher", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
        return;
      this.Shutdown();
    }

    private void Shutdown()
    {
      foreach (Patcher patcher in this.patchers)
        patcher.Stop();
      this.HideTray();
      this.app.Shutdown();
    }

    private enum UpdateState
    {
      None,
      Available,
      Downloading,
      Restarting,
      Failed,
      Done,
    }
  }
}
