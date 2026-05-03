using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using Hardcodet.Wpf.TaskbarNotification;
using TracePML.Models;
using TracePML.Services;
using TracePML.ViewModels;
using TracePML.Views;
using Wps.ModuleService;

namespace TracePML;

/// <summary>
/// Entry point TracePML.
///
/// <para>Deux modes selon la présence de <c>--wps-session</c> dans args :</para>
/// <list type="bullet">
/// <item><b>Embedded</b> (lancé par un host wipiSoft) : pas de mutex singleton (le host gère
///   l'unicité via session ID), pas de systray (le host gère la présence via son panneau
///   Services), pas de fenêtre auto. La <see cref="MainWindow"/> est exposée via
///   <c>WpsModuleService.RegisterSettingsWindow</c> et n'apparaît qu'à la demande
///   <c>SHOW_SETTINGS</c> du host. Le service tourne en arrière-plan, émet ses toasts.</item>
/// <item><b>Standalone</b> (lancé directement par l'utilisateur) : comportement historique
///   intact — mutex singleton, systray, fenêtre Debug masquée par défaut, toasts émis
///   normalement. Utile pour debug/dépannage sans Host.</item>
/// </list>
/// </summary>
public partial class App : Application
{
    private static Mutex? _mutex;
    private bool _mutexOwned;
    private TaskbarIcon? _trayIcon;
    private MainViewModel? _viewModel;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Tout le reste est asynchrone (Bootstrap IPC) → délégué à InitAsync. OnStartup
        // ne peut pas être async (signature override), donc fire-and-forget.
        _ = InitAsync(e.Args);
    }

    private async Task InitAsync(string[] args)
    {
        // Bootstrap ModuleService : parse args → IsEmbedded/SessionId, ouvre les pipes IPC
        // si embedded, fait le handshake HELLO/WELCOME avec Kind=ModuleService. En standalone,
        // no-op silencieux.
        await WpsModuleService.BootstrapAsync(args);

        // ====== Mutex singleton — UNIQUEMENT en standalone ======
        // En embedded, le host gère l'unicité par session ID (chaque launch = nouveau pipe).
        if (!WpsModuleService.IsEmbedded)
        {
            _mutex = new Mutex(true, "TracePML_SingleInstance", out _mutexOwned);
            if (!_mutexOwned)
            {
                _mutex.Dispose();
                _mutex = null;
                Shutdown();
                return;
            }
        }

        // ====== Init services + ViewModel (commun aux 2 modes) ======
        var settings = new RegistrySettings();
        var parser = new PmlLogParser();
        var monitor = new PmlFileMonitor();
        var toastService = new ToastService();

        WpsDebugSender.Log($"TracePML demarrage (embedded={WpsModuleService.IsEmbedded})", LogLevel.Info, "TracePML");

        // Branchement diag logs → onglet Notify + wipiLOG
        Action<string> diagLog = msg =>
            Application.Current.Dispatcher.BeginInvoke(() =>
                _viewModel!.ToastLogText += $"{msg}{Environment.NewLine}");
        Action<string> wipiLog = msg =>
            WpsDebugSender.Log(msg, LogLevel.Debug, "TracePML");
        PmlFileMonitor.DiagLog = diagLog;
        PmlFileMonitor.WipiLog = wipiLog;
        ToastService.DiagLog = diagLog;
        ToastService.WipiLog = wipiLog;
        MainViewModel.FileLog = wipiLog;

        _viewModel = new MainViewModel(settings, parser, monitor, toastService);

        // ====== MainWindow : créée mais non affichée par défaut ======
        // En embedded : exposée comme settings window (factory ci-dessous).
        // En standalone : affichée via _viewModel.IsDebugMode (= comportement historique).
        _mainWindow = new MainWindow
        {
            DataContext = _viewModel,
            Icon = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/Assets/tracepml.ico"))
        };

        // Empêche la fermeture de fermer le process : la MainWindow se cache, le service
        // continue de tourner. Important en embedded (le host pilote le shutdown via CLOSE)
        // et utile en standalone (le user ferme la fenêtre, le systray reste actif).
        _mainWindow.Closing += (s, ev) =>
        {
            if (s is Window w)
            {
                ev.Cancel = true;
                w.Hide();
                if (_viewModel is not null) _viewModel.IsDebugMode = false;
            }
        };

        if (WpsModuleService.IsEmbedded)
        {
            await ConfigureEmbeddedAsync();
        }
        else
        {
            ConfigureStandalone();
        }

        // ====== Démarrer le monitoring (commun, sauf si TestMode) ======
        if (!_viewModel.IsTestMode)
            _viewModel.StartMonitoring();
    }

    /// <summary>Configuration mode embedded : enregistre les handlers Invoke + la settings
    /// window auprès du SDK, signale READY, attend le shutdown demandé par le host.</summary>
    private async Task ConfigureEmbeddedAsync()
    {
        // Settings window : à chaque SHOW_SETTINGS du host, on retourne l'instance unique
        // (le SDK appellera window.Show() — idempotent si déjà visible).
        WpsModuleService.RegisterSettingsWindow(() => _mainWindow!);

        // ====== Méthodes Invoke exposées au host ======
        WpsModuleService.RegisterInvokeHandler<MonitoringStatusParams, MonitoringStatusResult>(
            "GetMonitoringStatus",
            async _ =>
            {
                await Task.CompletedTask;
                var path = _viewModel?.MonitoringFilePath ?? "";
                long size = 0;
                DateTime? lastUtc = null;
                bool isRunning = false;
                try
                {
                    if (System.IO.File.Exists(path))
                    {
                        var fi = new System.IO.FileInfo(path);
                        size = fi.Length;
                        lastUtc = fi.LastWriteTimeUtc;
                    }
                    isRunning = _viewModel?.IsMonitoringActive ?? false;
                }
                catch { }
                return new MonitoringStatusResult
                {
                    IsRunning = isRunning,
                    FilePath = path,
                    FileSize = size,
                    LastModifiedUtc = lastUtc,
                };
            });

        WpsModuleService.RegisterInvokeHandler<EmptyParams, LastNotificationResult>(
            "GetLastNotification",
            async _ =>
            {
                await Task.CompletedTask;
                var n = _viewModel?.LastNotification;
                if (n is null) return new LastNotificationResult { HasNotification = false };
                return new LastNotificationResult
                {
                    HasNotification = true,
                    Status = n.Status.ToString(),
                    Title = n.Title,
                    Detail = n.Detail,
                    EmittedAtUtc = _viewModel?.LastNotificationAtUtc,
                };
            });

        await WpsModuleService.NotifyReadyAsync();
        WpsDebugSender.Log("ModuleService READY — handlers Invoke enregistrés (GetMonitoringStatus, GetLastNotification)",
            LogLevel.Info, "TracePML");

        // Attend la fin (CLOSE reçu ou pipe coupé) puis shutdown l'app WPF proprement.
        // Fire-and-forget : la tâche RunAsync bloque jusqu'au shutdown, on relaie sur le
        // dispatcher pour Application.Shutdown.
        _ = Task.Run(async () =>
        {
            await WpsModuleService.RunAsync();
            Application.Current.Dispatcher.Invoke(() =>
            {
                WpsDebugSender.Log("ModuleService shutdown demandé par le host → Application.Shutdown",
                    LogLevel.Info, "TracePML");
                Application.Current.Shutdown();
            });
        });
    }

    /// <summary>Configuration mode standalone : comportement historique inchangé — systray
    /// + fenêtre Debug si IsDebugMode.</summary>
    private void ConfigureStandalone()
    {
        // Systray
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "TracePML",
            MenuActivation = PopupActivationMode.RightClick,
            IconSource = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/Assets/tracepml.ico"))
        };

        var contextMenu = new System.Windows.Controls.ContextMenu();
        var quitItem = new System.Windows.Controls.MenuItem { Header = "Quitter" };
        quitItem.Click += (_, _) => Shutdown();
        contextMenu.Items.Add(quitItem);
        _trayIcon.ContextMenu = contextMenu;
        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();

        // Debug mode : afficher/cacher la fenêtre principale
        _viewModel!.DebugModeChanged += visible =>
        {
            if (visible)
                ShowMainWindow();
            else
                _mainWindow!.Hide();
        };

        if (_viewModel.IsDebugMode)
            _mainWindow!.Show();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _viewModel?.Dispose();
        _trayIcon?.Dispose();
        if (_mutexOwned && _mutex is not null)
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
        base.OnExit(e);
    }
}

// ====== DTOs Invoke (sérialisés via System.Text.Json côté SDK) ======

/// <summary>Paramètres vides pour les handlers Invoke sans paramètre. Une classe vide pour
/// satisfaire la contrainte <c>where TParams : class</c> du SDK.</summary>
public sealed class EmptyParams { }

public sealed class MonitoringStatusParams { }

public sealed class MonitoringStatusResult
{
    public bool IsRunning { get; set; }
    public string FilePath { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime? LastModifiedUtc { get; set; }
}

public sealed class LastNotificationResult
{
    public bool HasNotification { get; set; }
    public string Status { get; set; } = "";
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public DateTime? EmittedAtUtc { get; set; }
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}
