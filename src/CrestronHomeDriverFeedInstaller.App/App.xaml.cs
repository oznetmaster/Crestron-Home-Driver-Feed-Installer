// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows;

using Microsoft.Extensions.DependencyInjection;

using CrestronHomeDriverFeedInstaller.App.ViewModels;
using CrestronHomeDriverFeedInstaller.Core.Abstractions;
using CrestronHomeDriverFeedInstaller.Core.Services;

namespace CrestronHomeDriverFeedInstaller.App;

public partial class App : Application
	{
	private ServiceProvider? serviceProvider;

	protected override void OnStartup (StartupEventArgs e)
		{
		base.OnStartup (e);

		var services = new ServiceCollection ();
		services.AddSingleton<IAppSettingsStore, JsonAppSettingsStore> ();
		services.AddSingleton<INuGetPackageService, NuGetPackageService> ();
		services.AddSingleton<IPackageInspectionService, PackageInspectionService> ();
		services.AddSingleton<ICredentialStore, ProtectedCredentialStore> ();
		services.AddSingleton<IProcessorDiscoveryService, ProcessorDiscoveryService> ();
		services.AddSingleton<ISftpDriverDeploymentService, SftpDriverDeploymentService> ();
		services.AddSingleton<MainViewModel> ();
		services.AddSingleton<MainWindow> ();

		serviceProvider = services.BuildServiceProvider ();
		var mainWindow = serviceProvider.GetRequiredService<MainWindow> ();
		mainWindow.DataContext = serviceProvider.GetRequiredService<MainViewModel> ();
		mainWindow.Show ();
		}

	protected override void OnExit (ExitEventArgs e)
		{
		serviceProvider?.Dispose ();
		base.OnExit (e);
		}
	}
