using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TracePML.Models;

namespace TracePML.Views;

public partial class ToastWindow : Window
{
    private readonly DispatcherTimer _waitTimer;

    public ToastWindow(OrderNotification notification)
    {
        InitializeComponent();
        Banner.Apply(notification);

        _waitTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _waitTimer.Tick += (_, _) =>
        {
            _waitTimer.Stop();
            SlideOut();
        };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Centré horizontalement sur la work area du moniteur primaire (= écran moins taskbar).
        // Top négatif puis SlideIn anime vers Top=10 → slide-in depuis le haut.
        Left = (SystemParameters.WorkArea.Width - ActualWidth) / 2;
        Top = -ActualHeight;
        Opacity = 0;
        SlideIn();
    }

    private void SlideIn()
    {
        var slideAnim = new DoubleAnimation(-ActualHeight, 10, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        var opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180));
        slideAnim.Completed += (_, _) => _waitTimer.Start();
        BeginAnimation(TopProperty, slideAnim);
        BeginAnimation(OpacityProperty, opacityAnim);
    }

    private void SlideOut()
    {
        var slideAnim = new DoubleAnimation(Top, -ActualHeight, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        var opacityAnim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180));
        slideAnim.Completed += (_, _) => Close();
        BeginAnimation(TopProperty, slideAnim);
        BeginAnimation(OpacityProperty, opacityAnim);
    }
}
