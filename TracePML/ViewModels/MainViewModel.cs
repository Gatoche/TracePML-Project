using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TracePML.Models;
using TracePML.Services;

namespace TracePML.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private const string PathPmlWinpharma = @"C:\WPHARMA\TELECOM\pml.log";

    private readonly RegistrySettings _settings;
    private readonly PmlLogParser _parser;
    private readonly PmlFileMonitor _monitor;
    private readonly ToastService _toastService;

    private string _pmlLogText = "";
    private string _notificationLogText = "";
    private bool _isTestMode;
    private bool _isLocalTest;
    private bool _isDebugMode;
    private string _logFileSize = "0";

    // Filtres émission
    private bool _showOrder = true;
    private bool _showRequestInfoProduct = true;
    private bool _showAcknowledgement;
    private bool _showEmptying;

    // Filtres réception
    private bool _showOrderResponse = true;
    private bool _showDispoInfoResponse = true;
    private bool _showDeliveryResponse;
    private bool _showServiceEnd;
    private bool _showResponseError;

    public MainViewModel(RegistrySettings settings, PmlLogParser parser,
        PmlFileMonitor monitor, ToastService toastService)
    {
        _settings = settings;
        _parser = parser;
        _monitor = monitor;
        _toastService = toastService;

        ParseCommand = new RelayCommand(ExecuteParse, () => IsTestMode);
        TestNotifyCommand = new RelayCommand(ExecuteTestNotify);
        ShowPreviewToastCommand = new RelayCommand(ExecuteShowPreviewToast);
        ClearToastLogCommand = new RelayCommand(() => ToastLogText = "");
        QuitCommand = new RelayCommand(() => Application.Current.Shutdown());

        _monitor.NewContentAvailable += OnNewContent;
        _monitor.FileSizeChanged += size =>
            Application.Current.Dispatcher.Invoke(() => LogFileSize = size.ToString());

        LoadSettings();
    }

    public string PmlLogText
    {
        get => _pmlLogText;
        set => SetField(ref _pmlLogText, value);
    }

    public string NotificationLogText
    {
        get => _notificationLogText;
        set => SetField(ref _notificationLogText, value);
    }

    public bool IsTestMode
    {
        get => _isTestMode;
        set
        {
            if (SetField(ref _isTestMode, value))
            {
                _settings.TestMode = value;
                if (!value) StartMonitoring();
                else StopMonitoring();
                PmlLogText = "";
                NotificationLogText = "";
            }
        }
    }

    public bool IsLocalTest
    {
        get => _isLocalTest;
        set { if (SetField(ref _isLocalTest, value)) _settings.LocalTest = value; }
    }

    public bool IsDebugMode
    {
        get => _isDebugMode;
        set
        {
            if (SetField(ref _isDebugMode, value))
            {
                if (!value) IsTestMode = false;
                DebugModeChanged?.Invoke(value);
            }
        }
    }

    public string LogFileSize
    {
        get => _logFileSize;
        set => SetField(ref _logFileSize, value);
    }

    // Filtres émission
    public bool ShowOrder { get => _showOrder; set => SetField(ref _showOrder, value); }
    public bool ShowRequestInfoProduct { get => _showRequestInfoProduct; set => SetField(ref _showRequestInfoProduct, value); }
    public bool ShowAcknowledgement { get => _showAcknowledgement; set => SetField(ref _showAcknowledgement, value); }
    public bool ShowEmptying { get => _showEmptying; set => SetField(ref _showEmptying, value); }

    // Filtres réception
    public bool ShowOrderResponse { get => _showOrderResponse; set => SetField(ref _showOrderResponse, value); }
    public bool ShowDispoInfoResponse { get => _showDispoInfoResponse; set => SetField(ref _showDispoInfoResponse, value); }
    public bool ShowDeliveryResponse { get => _showDeliveryResponse; set => SetField(ref _showDeliveryResponse, value); }
    public bool ShowServiceEnd { get => _showServiceEnd; set => SetField(ref _showServiceEnd, value); }
    public bool ShowResponseError { get => _showResponseError; set => SetField(ref _showResponseError, value); }

    // Onglet Toast - preview
    private bool _previewConfirmed = true;
    private bool _previewModified;
    private bool _previewCancelled;
    private string _toastLogText = "";

    public bool PreviewConfirmed
    {
        get => _previewConfirmed;
        set => SetField(ref _previewConfirmed, value);
    }

    public bool PreviewModified
    {
        get => _previewModified;
        set => SetField(ref _previewModified, value);
    }

    public bool PreviewCancelled
    {
        get => _previewCancelled;
        set => SetField(ref _previewCancelled, value);
    }

    public string PreviewTitle => PreviewConfirmed ? "Commande CERP : Confirmee"
        : PreviewModified ? "Commande CERP : partielle" : "Commande CERP : annulee";

    public string PreviewDetail => PreviewConfirmed ? "KETOCONAZOLE ARROW 2% GEL SAC DOSE 8 X 6G : cmde 1 / livre 1"
        : PreviewModified ? "KETOCONAZOLE ARROW 2% GEL SAC DOSE 8 X 6G : cmde 2 / livre 1"
        : "KETOCONAZOLE ARROW 2% GEL SAC DOSE 8 X 6G : cmde 2 / livre 0";

    public string ToastLogText
    {
        get => _toastLogText;
        set => SetField(ref _toastLogText, value);
    }

    public ICommand ParseCommand { get; }
    public ICommand TestNotifyCommand { get; }
    public ICommand ShowPreviewToastCommand { get; }
    public ICommand ClearToastLogCommand { get; }
    public ICommand QuitCommand { get; }

    public event Action<bool>? DebugModeChanged;

    public void StartMonitoring()
    {
        if (!_isTestMode && File.Exists(PathPmlWinpharma))
        {
            _monitor.Start(PathPmlWinpharma);
            DiagLog($"[MONITOR] Demarrage surveillance: {PathPmlWinpharma}");
        }
        else
        {
            DiagLog($"[MONITOR] Non demarre - TestMode={_isTestMode}, Existe={File.Exists(PathPmlWinpharma)}");
        }
    }

    public void StopMonitoring()
    {
        _monitor.Stop();
    }

    private void OnNewContent(string content)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            DiagLog($"[CONTENT] {content.Length} chars at {DateTime.Now:HH:mm:ss.fff}");
            ProcessContent(content);
        });
    }

    private int _processCount;

    private void ProcessContent(string content)
    {
        _processCount++;
        var result = _parser.Parse(content);

        DiagLog($"--- ProcessContent #{_processCount} ---");
        DiagLog($"[PARSE] Entries={result.Entries.Count}, Notifications={result.Notifications.Count}");
        WipiLog($"ProcessContent #{_processCount}: Entries={result.Entries.Count}, Notifs={result.Notifications.Count}");

        foreach (var entry in result.Entries)
        {
            DiagLog($"  [{entry.MessageType}] {(entry.IsRequest ? "REQ" : "REP")} {entry.Summary}");

            if (!IsFilterEnabled(entry.MessageType))
                continue;

            PmlLogText += entry.RawHeader + Environment.NewLine;
            if (!string.IsNullOrEmpty(entry.Summary))
                PmlLogText += ">>           " + entry.Summary + Environment.NewLine;
            PmlLogText += Environment.NewLine;
        }

        foreach (var notif in result.Notifications)
        {
            DiagLog($"[NOTIF] {notif.Status} - {notif.Title}");
            WipiLog($"[NOTIF] {notif.Status} - {notif.Title}");
            NotificationLogText += $"Status: {notif.Status}" + Environment.NewLine;
            NotificationLogText += $"Title: {notif.Title}" + Environment.NewLine;
            NotificationLogText += $"Detail: {notif.Detail}" + Environment.NewLine;
            ShowToastAndLog(notif);
        }
    }

    private bool IsFilterEnabled(PmlMessageType type) => type switch
    {
        PmlMessageType.Order => ShowOrder,
        PmlMessageType.RequestInfoProduct => ShowRequestInfoProduct,
        PmlMessageType.Acknowledgement => ShowAcknowledgement,
        PmlMessageType.Emptying => ShowEmptying,
        PmlMessageType.OrderResponse => ShowOrderResponse,
        PmlMessageType.DispoInfoResponse => ShowDispoInfoResponse,
        PmlMessageType.DeliveryResponse => ShowDeliveryResponse,
        PmlMessageType.ServiceEnd => ShowServiceEnd,
        PmlMessageType.ResponseError => ShowResponseError,
        _ => true
    };

    private void ExecuteParse()
    {
        string path = IsLocalTest ? "pml.log" : PathPmlWinpharma;
        if (!File.Exists(path)) return;

        string content = _monitor.ReadFromBeginning(path);
        if (!string.IsNullOrEmpty(content))
            ProcessContent(content);
    }

    private void ExecuteTestNotify()
    {
        var notif = new OrderNotification(OrderStatus.Modified,
            "COMMANDE MODIFIEE", "Cmde  3  |  Livre  2  |  Test");
        ShowToastAndLog(notif);
    }

    private void ExecuteShowPreviewToast()
    {
        var notif = new OrderNotification(
            PreviewConfirmed ? OrderStatus.Confirmed :
            PreviewModified ? OrderStatus.Modified : OrderStatus.Cancelled,
            PreviewTitle, PreviewDetail);
        ShowToastAndLog(notif);
    }

    private void ShowToastAndLog(OrderNotification notif)
    {
        _toastService.ShowNotification(notif);
        ToastLogText += $"[{DateTime.Now:HH:mm:ss}] {notif.Status} - {notif.Title} - {notif.Detail}{Environment.NewLine}";
    }

    public static Action<string>? FileLog;

    private void DiagLog(string msg)
    {
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}{Environment.NewLine}";
        PmlLogText += line;
        ToastLogText += line;
        System.Diagnostics.Debug.WriteLine(msg);
    }

    private void WipiLog(string msg)
    {
        FileLog?.Invoke(msg);
    }

    private void LoadSettings()
    {
        _isTestMode = _settings.TestMode;
        _isLocalTest = _settings.LocalTest;
        _isDebugMode = false; // Démarre réduit en systray
        OnPropertyChanged(nameof(IsTestMode));
        OnPropertyChanged(nameof(IsLocalTest));
        OnPropertyChanged(nameof(IsDebugMode));
    }

    public void Dispose()
    {
        _monitor.Dispose();
        GC.SuppressFinalize(this);
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    #endregion
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
