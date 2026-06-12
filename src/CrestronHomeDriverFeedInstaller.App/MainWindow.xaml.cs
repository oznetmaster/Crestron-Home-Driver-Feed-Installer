// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;

using CrestronHomeDriverFeedInstaller.App.ViewModels;
using CrestronHomeDriverFeedInstaller.Core.Abstractions;
using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.App;

public partial class MainWindow : Window
	{
	private const int ABOUT_SYSTEM_MENU_ITEM_ID = 0x1000;
	private const int MF_SEPARATOR = 0x800;
	private const int MF_STRING = 0x0;
	private const int WM_SYSCOMMAND = 0x112;

	private readonly IAppSettingsStore appSettingsStore;
	private DataGrid? preservedSelectionDataGrid;
	private object? preservedSelectedItem;
	private IReadOnlyList<object> preservedSelectedItems = Array.Empty<object> ();

	public MainWindow (IAppSettingsStore appSettingsStore)
		{
		this.appSettingsStore = appSettingsStore;
		InitializeComponent ();
		Loaded += MainWindow_OnLoaded;
		SourceInitialized += MainWindow_OnSourceInitialized;
		Closing += MainWindow_OnClosing;
		DataContextChanged += MainWindow_OnDataContextChanged;
		}

	private void MainWindow_OnSourceInitialized (object? sender, EventArgs e)
		{
		var windowHandle = new WindowInteropHelper (this).Handle;
		var systemMenuHandle = GetSystemMenu (windowHandle, false);

		if (systemMenuHandle == IntPtr.Zero)
			{
			return;
			}

		AppendMenu (systemMenuHandle, MF_SEPARATOR, 0, string.Empty);
		AppendMenu (systemMenuHandle, MF_STRING, ABOUT_SYSTEM_MENU_ITEM_ID, $"About {GetApplicationTitle ()}...");

		HwndSource.FromHwnd (windowHandle)?.AddHook (WindowProcedure);
		}

	private async void MainWindow_OnLoaded (object sender, RoutedEventArgs e)
		{
		var settings = await appSettingsStore.LoadAsync ();
		ApplyWindowSettings (settings);
		if (DataContext is MainViewModel viewModel)
			{
			PasswordBox.Password = viewModel.Password;
			}
		}

	private void MainWindow_OnClosing (object? sender, CancelEventArgs e)
		{
		var settings = appSettingsStore.Load ();
		var bounds = WindowState == WindowState.Normal ? new Rect (Left, Top, Width, Height) : RestoreBounds;
		if (!IsRestorableWindowBounds (bounds))
			{
			return;
			}

		settings.WindowLeft = bounds.Left;
		settings.WindowTop = bounds.Top;
		settings.WindowWidth = bounds.Width;
		settings.WindowHeight = bounds.Height;
		appSettingsStore.Save (settings);
		}

	private void PasswordBox_OnPasswordChanged (object sender, RoutedEventArgs e)
		{
		if (DataContext is MainViewModel viewModel)
			{
			viewModel.Password = PasswordBox.Password;
			}
		}

	private void MainWindow_OnDataContextChanged (object sender, DependencyPropertyChangedEventArgs e)
		{
		if (e.OldValue is MainViewModel oldViewModel)
			{
			oldViewModel.PropertyChanged -= ViewModel_OnPropertyChanged;
			}

		if (e.NewValue is MainViewModel newViewModel)
			{
			newViewModel.PropertyChanged += ViewModel_OnPropertyChanged;
			PasswordBox.Password = newViewModel.Password;
			}
		}

	private void ViewModel_OnPropertyChanged (object? sender, PropertyChangedEventArgs e)
		{
		if (sender is not MainViewModel viewModel || e.PropertyName != nameof (MainViewModel.Password) || PasswordBox.Password == viewModel.Password)
			{
			return;
			}

		PasswordBox.Password = viewModel.Password;
		}

	private void PackageInfoButton_OnClick (object sender, RoutedEventArgs e)
		{
		if (DataContext is not MainViewModel viewModel)
			{
			RestorePreservedSelection ();
			return;
			}

		if (sender is FrameworkElement frameworkElement && frameworkElement.DataContext is PackageSearchResultViewModel package)
			{
			viewModel.ShowPackageInfo (package);
			}

		RestorePreservedSelection ();
		}

	private void CachedPackageInfoButton_OnClick (object sender, RoutedEventArgs e)
		{
		if (DataContext is not MainViewModel viewModel)
			{
			RestorePreservedSelection ();
			return;
			}

		if (sender is FrameworkElement frameworkElement && frameworkElement.DataContext is CachedPackageInfoViewModel cachedPackage)
			{
			_ = viewModel.ShowCachedPackageInfoAsync (cachedPackage);
			}

		RestorePreservedSelection ();
		}

	private void InfoButton_OnPreviewMouseLeftButtonDown (object sender, MouseButtonEventArgs e)
		{
		if (sender is not DependencyObject dependencyObject)
			{
			return;
			}

		var dataGrid = FindAncestor<DataGrid> (dependencyObject);
		if (dataGrid is null)
			{
			return;
			}

		preservedSelectionDataGrid = dataGrid;
		preservedSelectedItem = dataGrid.SelectedItem;
		preservedSelectedItems = dataGrid.SelectedItems.Cast<object> ().ToArray ();
		}

	private void CachedPackagesDataGrid_OnSelectionChanged (object sender, SelectionChangedEventArgs e)
		{
		if (DataContext is not MainViewModel viewModel || sender is not DataGrid dataGrid)
			{
			return;
			}

		viewModel.SelectedCachedPackages = dataGrid.SelectedItems.OfType<CachedPackageInfoViewModel> ().ToArray ();
		viewModel.SelectedCachedPackage = dataGrid.SelectedItem as CachedPackageInfoViewModel;
		}

	private void DataGrid_OnPreviewMouseLeftButtonDown (object sender, MouseButtonEventArgs e)
		{
		if (sender is not DataGrid dataGrid)
			{
			return;
			}

		var dependencyObject = e.OriginalSource as DependencyObject;
		if (dependencyObject is null || IsInteractiveElement (dependencyObject))
			{
			return;
			}

		var row = FindAncestor<DataGridRow> (dependencyObject);
		if (row?.Item is null || !row.IsSelected || dataGrid.SelectedItems.Count != 1)
			{
			return;
			}

		dataGrid.UnselectAll ();
		dataGrid.SelectedItem = null;
		e.Handled = true;
		}

	private void DeleteCachedPackagesButton_OnClick (object sender, RoutedEventArgs e)
		{
		if (DataContext is not MainViewModel viewModel || !viewModel.DeleteCachedPackageCommand.CanExecute (null))
			{
			return;
			}

		var selectionDescription = string.Join (Environment.NewLine, viewModel.SelectedCachedPackages.Select (package => $"• {package.DisplayLabel}"));
		var result = MessageBox.Show (
			$"Delete the selected cached package(s)?{Environment.NewLine}{Environment.NewLine}{selectionDescription}",
			"Confirm delete",
			MessageBoxButton.YesNo,
			MessageBoxImage.Warning,
			MessageBoxResult.No);

		if (result == MessageBoxResult.Yes)
			{
			viewModel.DeleteCachedPackageCommand.Execute (null);
			}
		}

	private void SearchResultsDataGrid_OnSelectionChanged (object sender, SelectionChangedEventArgs e)
		{
		if (DataContext is not MainViewModel viewModel || sender is not DataGrid dataGrid)
			{
			return;
			}

		viewModel.SelectedPackages = dataGrid.SelectedItems.OfType<PackageSearchResultViewModel> ().ToArray ();
		viewModel.SelectedPackage = dataGrid.SelectedItem as PackageSearchResultViewModel;
		}

	private void PackageTabTextBox_OnTextChanged (object sender, TextChangedEventArgs e)
		{
		if (sender is not TextBox textBox)
			{
			return;
			}

		var activeTabMatches = ReferenceEquals (textBox, ReadmeTextBox) && ReadmeTabItem.IsSelected
			|| ReferenceEquals (textBox, EntriesTextBox) && EntriesTabItem.IsSelected
			|| ReferenceEquals (textBox, ManifestTextBox) && ManifestTabItem.IsSelected;

		if (!activeTabMatches)
			{
			return;
			}

		textBox.ScrollToHome ();
		}

	private void ApplyWindowSettings (AppSettings settings)
		{
		if (settings.WindowLeft is null || settings.WindowTop is null || settings.WindowWidth is null || settings.WindowHeight is null)
			{
			return;
			}

		var bounds = new Rect (settings.WindowLeft.Value, settings.WindowTop.Value, settings.WindowWidth.Value, settings.WindowHeight.Value);
		if (!IsRestorableWindowBounds (bounds))
			{
			return;
			}

		Left = bounds.Left;
		Top = bounds.Top;
		Width = bounds.Width;
		Height = bounds.Height;
		}

	private static bool IsRestorableWindowBounds (Rect bounds)
		{
		if (double.IsNaN (bounds.Left)
			|| double.IsNaN (bounds.Top)
			|| double.IsNaN (bounds.Width)
			|| double.IsNaN (bounds.Height)
			|| bounds.Width <= 0
			|| bounds.Height <= 0)
			{
			return false;
			}

		var virtualScreenBounds = new Rect (
			SystemParameters.VirtualScreenLeft,
			SystemParameters.VirtualScreenTop,
			SystemParameters.VirtualScreenWidth,
			SystemParameters.VirtualScreenHeight);

		return bounds.IntersectsWith (virtualScreenBounds);
		}

	private static bool IsInteractiveElement (DependencyObject dependencyObject)
		{
		return FindAncestor<ButtonBase> (dependencyObject) is not null
			|| FindAncestor<TextBoxBase> (dependencyObject) is not null
			|| FindAncestor<ScrollBar> (dependencyObject) is not null;
		}

	private void RestorePreservedSelection ()
		{
		if (preservedSelectionDataGrid is null)
			{
			return;
			}

		var dataGrid = preservedSelectionDataGrid;
		var selectedItem = preservedSelectedItem;
		var selectedItems = preservedSelectedItems;

		preservedSelectionDataGrid = null;
		preservedSelectedItem = null;
		preservedSelectedItems = Array.Empty<object> ();

		dataGrid.UnselectAll ();
		if (selectedItem is not null)
			{
			dataGrid.SelectedItem = selectedItem;
			}

		foreach (var item in selectedItems.Where (item => !ReferenceEquals (item, selectedItem)))
			{
			dataGrid.SelectedItems.Add (item);
			}
		}

	private static T? FindAncestor<T> (DependencyObject? dependencyObject)
		where T : DependencyObject
		{
		while (dependencyObject is not null)
			{
			if (dependencyObject is T target)
				{
				return target;
				}

			dependencyObject = VisualTreeHelper.GetParent (dependencyObject);
			}

		return null;
		}

	private IntPtr WindowProcedure (IntPtr windowHandle, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
		if (message == WM_SYSCOMMAND && wParam.ToInt32 () == ABOUT_SYSTEM_MENU_ITEM_ID)
			{
			new AboutWindow (GetApplicationTitle (), GetApplicationVersion (), GetTargetFramework (), GetCopyright (), GetRepositoryUrl ())
				{
				Owner = this
				}.ShowDialog ();
			handled = true;
			}

		return IntPtr.Zero;
		}

	private static string GetApplicationTitle ()
		{
		var assembly = Assembly.GetEntryAssembly ();
		return assembly?.GetCustomAttribute<AssemblyTitleAttribute> ()?.Title
			?? assembly?.GetCustomAttribute<AssemblyProductAttribute> ()?.Product
			?? assembly?.GetName ().Name
			?? "Crestron Home Driver Feed Installer";
		}

	private static string GetApplicationVersion ()
		{
		var assembly = Assembly.GetEntryAssembly ();
		var assemblyNameVersion = assembly?.GetName ().Version;
		if (assemblyNameVersion is not null)
			{
			return assemblyNameVersion.Build > 0 || assemblyNameVersion.Revision > 0
				? assemblyNameVersion.ToString ()
				: $"{assemblyNameVersion.Major}.{assemblyNameVersion.Minor}.{Math.Max (assemblyNameVersion.Build, 0)}";
			}

		return "Unknown";
		}

	private static string GetTargetFramework ()
		{
		return Assembly.GetEntryAssembly ()?.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute> ()?.FrameworkName ?? "Unknown";
		}

	private static string GetCopyright ()
		{
		return Assembly.GetEntryAssembly ()?.GetCustomAttribute<AssemblyCopyrightAttribute> ()?.Copyright ?? string.Empty;
		}

	private static string GetRepositoryUrl ()
		{
		var assembly = Assembly.GetEntryAssembly ();
		var metadataAttribute = assembly?.GetCustomAttributes<AssemblyMetadataAttribute> ()
			.FirstOrDefault (attribute => string.Equals (attribute.Key, "RepositoryUrl", StringComparison.Ordinal));

		return metadataAttribute?.Value
			?? assembly?.GetCustomAttribute<AssemblyMetadataAttribute> ()?.Value
			?? "https://github.com/oznetmaster/Crestron-Home-Driver-Feed-Installer";
		}

	[DllImport ("user32.dll", CharSet = CharSet.Unicode)]
	private static extern bool AppendMenu (IntPtr menuHandle, int flags, int itemId, string itemText);

	[DllImport ("user32.dll")]
	private static extern IntPtr GetSystemMenu (IntPtr windowHandle, bool revert);
	}
