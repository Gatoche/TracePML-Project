using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Hardcodet.Wpf.TaskbarNotification;
using TracePML.Services;
using TracePML.ViewModels;
using TracePML.Views;

namespace TracePML;

public partial class App : Application
{
    private static Mutex? _mutex;
    private bool _mutexOwned;
    private TaskbarIcon? _trayIcon;
    private MainViewModel? _viewModel;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "TracePML_SingleInstance", out _mutexOwned);
        if (!_mutexOwned)
        {
            _mutex.Dispose();
            _mutex = null;
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // Services
        var settings = new RegistrySettings();
        var parser = new PmlLogParser();
        var monitor = new PmlFileMonitor();
        var toastService = new ToastService();

        // wipiLOG au démarrage
        WpsDebugSender.Log("TracePML demarrage", LogLevel.Info, "TracePML");

        // Brancher les diag logs sur le ViewModel (onglet Notify) + wipiLOG
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

        // MainWindow
        _mainWindow = new MainWindow
        {
            DataContext = _viewModel,
            Icon = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/Assets/tracepml.ico"))
        };

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

        // Debug mode : écouter les changements
        _viewModel.DebugModeChanged += visible =>
        {
            if (visible)
                ShowMainWindow();
            else
                _mainWindow.Hide();
        };

        // Démarrage : fenêtre visible si debug mode
        if (_viewModel.IsDebugMode)
            _mainWindow.Show();

        // Démarrer le monitoring si pas en mode test
        if (!_viewModel.IsTestMode)
            _viewModel.StartMonitoring();
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

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}
