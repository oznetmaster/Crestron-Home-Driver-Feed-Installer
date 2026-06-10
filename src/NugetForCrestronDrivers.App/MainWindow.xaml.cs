using System.Windows;
using NugetForCrestronDrivers.App.ViewModels;

namespace NugetForCrestronDrivers.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Password = PasswordBox.Password;
        }
    }
}