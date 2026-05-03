using System.IO;
using System.Text;

namespace TracePML.Services;

public class PmlFileMonitor : IDisposable
{
    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private long _lastPosition;
    private string _filePath = "";
    private readonly object _lock = new();
    private int _eventCount;
    private int _processing; // 0=idle, 1=processing

    public event Action<string>? NewContentAvailable;
    public event Action<long>? FileSizeChanged;

    public void Start(string filePath)
    {
        Stop();

        _filePath = filePath;
        string directory = Path.GetDirectoryName(filePath)!;
        string fileName = Path.GetFileName(filePath);

        if (File.Exists(filePath))
        {
            var fi = new FileInfo(filePath);
            _lastPosition = fi.Length;
            FileSizeChanged?.Invoke(fi.Length);
        }

        _debounceTimer = new Timer(OnDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);

        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFileChanged;
    }

    public void Stop()
    {
        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnFileChanged;
            _watcher.Dispose();
            _watcher = null;
        }

        _debounceTimer?.Dispose();
        _debounceTimer = null;
    }

    public string ReadFromBeginning(string filePath)
    {
        _filePath = filePath;
        _lastPosition = 0;
        return ReadDelta();
    }

    public static Action<string>? DiagLog;
    public static Action<string>? WipiLog;

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        Interlocked.Increment(ref _eventCount);
        DiagLog?.Invoke($"[FSW] Event #{_eventCount} at {DateTime.Now:HH:mm:ss.fff}");
        WipiLog?.Invoke($"[FSW] Event #{_eventCount}");
        _debounceTimer?.Change(500, Timeout.Infinite);
    }

    private void OnDebounceElapsed(object? state)
    {
        // Empêcher le double-fire
        if (Interlocked.CompareExchange(ref _processing, 1, 0) != 0)
        {
            DiagLog?.Invoke($"[FSW] Debounce SKIPPED (already processing)");
            return;
        }

        try
        {
            int evts = Interlocked.Exchange(ref _eventCount, 0);
            DiagLog?.Invoke($"[FSW] Debounce fired, {evts} events at {DateTime.Now:HH:mm:ss.fff}");
            WipiLog?.Invoke($"[FSW] Debounce fired, {evts} events");

            string delta = ReadDelta();
            if (!string.IsNullOrEmpty(delta))
            {
                DiagLog?.Invoke($"[FSW] Delta: {delta.Length} chars, pos={_lastPosition}");
                WipiLog?.Invoke($"[FSW] Delta: {delta.Length} chars");
                NewContentAvailable?.Invoke(delta);
            }
            else
            {
                DiagLog?.Invoke($"[FSW] Delta empty");
                WipiLog?.Invoke($"[FSW] Delta empty");
            }
        }
        finally
        {
            Interlocked.Exchange(ref _processing, 0);
        }
    }

    private string ReadDelta()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(_filePath))
                    return "";

                using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                long currentSize = fs.Length;

                FileSizeChanged?.Invoke(currentSize);

                if (currentSize < _lastPosition)
                    _lastPosition = 0;

                if (currentSize <= _lastPosition)
                    return "";

                fs.Seek(_lastPosition, SeekOrigin.Begin);
                int bytesToRead = (int)(currentSize - _lastPosition);
                byte[] buffer = new byte[bytesToRead];
                int read = fs.Read(buffer, 0, bytesToRead);
                _lastPosition = currentSize;

                return Encoding.UTF8.GetString(buffer, 0, read);
            }
            catch
            {
                return "";
            }
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
