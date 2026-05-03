using System.Runtime.InteropServices;
using System.Windows;
using TracePML.Models;
using TracePML.Views;

namespace TracePML.Services;

public class ToastService
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public static Action<string>? DiagLog;
    public static Action<string>? WipiLog;

    public void ShowNotification(OrderNotification notification)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IntPtr hwndPrevious = GetForegroundWindow();

            var toast = new ToastWindow(notification);
            toast.Show();

            DiagLog?.Invoke($"[TOAST] Show: {notification.Title} at {DateTime.Now:HH:mm:ss.fff}");
            WipiLog?.Invoke($"[TOAST] Show: {notification.Title}");

            if (hwndPrevious != IntPtr.Zero)
                SetForegroundWindow(hwndPrevious);
        });
    }
}
