using System.ComponentModel;
using System.Windows;
using TracePML.Models;
using TracePML.ViewModels;
using wipisoft;

namespace TracePML.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is MainViewModel vm)
        {
            // Appliquer l'état initial du preview
            UpdatePreviewBanner(vm);

            // Écouter les changements de propriétés
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(MainViewModel.PreviewConfirmed)
                    or nameof(MainViewModel.PreviewModified)
                    or nameof(MainViewModel.PreviewCancelled))
                {
                    UpdatePreviewBanner(vm);
                }
            };
        }
    }

    private void UpdatePreviewBanner(MainViewModel vm)
    {
        var status = vm.PreviewConfirmed ? OrderStatus.Confirmed
            : vm.PreviewModified ? OrderStatus.Modified
            : OrderStatus.Cancelled;

        PreviewBanner.Apply(new OrderNotification(status, vm.PreviewTitle, vm.PreviewDetail));
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
