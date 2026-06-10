using System.Windows;
using System.Windows.Controls;

using CrestronHomeDriverFeedInstaller.App.ViewModels;

namespace CrestronHomeDriverFeedInstaller.App;

public partial class MainWindow : Window
	{
	public MainWindow ()
		{
		InitializeComponent ();
		}

	private void PasswordBox_OnPasswordChanged (object sender, RoutedEventArgs e)
		{
		if (DataContext is MainViewModel viewModel)
			{
			viewModel.Password = PasswordBox.Password;
			}
		}

	private void PackageInfoButton_OnClick (object sender, RoutedEventArgs e)
		{
		if (DataContext is not MainViewModel viewModel)
			{
			return;
			}

		if (sender is FrameworkElement frameworkElement && frameworkElement.DataContext is PackageSearchResultViewModel package)
			{
			viewModel.ShowPackageInfo (package);
			}
		}

	private void CachedPackageInfoButton_OnClick (object sender, RoutedEventArgs e)
		{
		if (DataContext is not MainViewModel viewModel)
			{
			return;
			}

		if (sender is FrameworkElement frameworkElement && frameworkElement.DataContext is CachedPackageInfoViewModel cachedPackage)
			{
			viewModel.ShowCachedPackageInfo (cachedPackage);
			}
		}
	}
