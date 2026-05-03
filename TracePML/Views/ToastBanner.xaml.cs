using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TracePML.Models;
using wipisoft;

namespace TracePML.Views;

public partial class ToastBanner : UserControl
{
    public ToastBanner()
    {
        InitializeComponent();

// #if DEBUG
//         Loaded += (_, _) =>
//         {
//             WpsXamlAdjust.RegisterTree(this, BannerBorder);
//             WpsXamlAdjust.InjectOverlayButton(BannerBorder);
//         };
// #endif
    }

    public void SetContent(string title, string detail)
    {
        TitleText.Text = title;
        DetailText.Text = detail;
    }

    public void SetStatus(OrderStatus status)
    {
        Brush bgBrush;
        Brush textBrush;

        switch (status)
        {
            case OrderStatus.Confirmed:
                bgBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#55D500"));
                textBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4a4a4a"));
                break;
            case OrderStatus.Modified:
                bgBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                textBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4a4a4a"));
                break;
            case OrderStatus.Cancelled:
                bgBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff4d00"));
                textBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffffff"));
                break;
            default:
                return;
        }

        BannerBorder.Background = bgBrush;
        TitleText.Foreground = textBrush;
        DetailText.Foreground = textBrush;
        AppText.Foreground = textBrush;
    }

    public void Apply(OrderNotification notification)
    {
        SetContent(notification.Title, notification.Detail);
        SetStatus(notification.Status);
    }
}
