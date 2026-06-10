using System.Collections.ObjectModel;
using System.IO;

using CrestronHomeDriverFeedInstaller.App.Infrastructure;
using CrestronHomeDriverFeedInstaller.Core.Abstractions;
using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.App.ViewModels;

public sealed class MainViewModel : ObservableObject
	{
	private readonly INuGetPackageService nugetPackageService;
	private readonly IPackageInspectionService packageInspectionService;
	private readonly ICredentialStore credentialStore;
	private readonly IProcessorDiscoveryService processorDiscoveryService;
	private readonly ISftpDriverDeploymentService sftpDriverDeploymentService;
	private readonly string cacheDirectory;
	private string feedUrl = "https://api.nuget.org/v3/index.json";
	private string searchTerm = "Crestron";
	private string processorDisplayName = string.Empty;
	private string processorHost = string.Empty;
	private string username = string.Empty;
	private string password = string.Empty;
	private bool rememberCredentials = true;
	private bool onlyV1CompliantPackages = true;
	private bool isStatusError;
	private bool showDiscoveryDiagnostics;
	private string statusMessage = "Ready.";
	private string selectedPackageDetails = "Select Info on a package entry to view more details.";
	private string selectedCachedPackageDetails = "Select Info on a cached package entry to view more details.";
	private string discoveryDiagnostics = string.Empty;
	private string crestronDriverPackageJsonContent = string.Empty;
	private string packageEntries = string.Empty;
	private string stagedDriverPackagePath = "No package has been staged yet.";
	private PackageSearchResultViewModel? selectedPackage;
	private DriverPackageInfo? selectedDriverPackage;
	private SavedProcessorCredentialViewModel? selectedSavedProcessor;
	private CachedPackageInfoViewModel? selectedCachedPackage;
	private bool isDiscoveringProcessors;

	public MainViewModel (INuGetPackageService nugetPackageService, IPackageInspectionService packageInspectionService, ICredentialStore credentialStore, IProcessorDiscoveryService processorDiscoveryService, ISftpDriverDeploymentService sftpDriverDeploymentService)
		{
		this.nugetPackageService = nugetPackageService;
		this.packageInspectionService = packageInspectionService;
		this.credentialStore = credentialStore;
		this.processorDiscoveryService = processorDiscoveryService;
		this.sftpDriverDeploymentService = sftpDriverDeploymentService;
		cacheDirectory = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), "CrestronHomeDriverFeedInstaller", "Cache");
		SearchResults = new ObservableCollection<PackageSearchResultViewModel> ();
		SavedProcessors = new ObservableCollection<SavedProcessorCredentialViewModel> ();
		CachedPackages = new ObservableCollection<CachedPackageInfoViewModel> ();
		SearchCommand = new AsyncRelayCommand (SearchAsync);
		InspectCommand = new AsyncRelayCommand (InspectSelectedAsync, () => SelectedPackage is not null);
		UseCachedPackageCommand = new AsyncRelayCommand (UseSelectedCachedPackagesAsync, () => CachedPackages.Any (package => package.IsSelected));
		DeleteCachedPackageCommand = new AsyncRelayCommand (DeleteSelectedCachedPackagesAsync, () => CachedPackages.Any (package => package.IsSelected));
		ShowCachedPackageInfoCommand = new AsyncRelayCommand (ShowSelectedCachedPackageInfoAsync, () => SelectedCachedPackage is not null);
		DiscoverProcessorsCommand = new AsyncRelayCommand (DiscoverProcessorsAsync, () => !isDiscoveringProcessors);
		AddProcessorCommand = new AsyncRelayCommand (AddProcessorAsync);
		DeleteProcessorCommand = new AsyncRelayCommand (DeleteSelectedProcessorsAsync, () => SavedProcessors.Any (processor => processor.IsSelected));
		InstallCommand = new AsyncRelayCommand (InstallAsync, () => selectedDriverPackage is not null && SavedProcessors.Any (processor => processor.IsSelected || ReferenceEquals (processor, SelectedSavedProcessor)) && !string.IsNullOrWhiteSpace (Username));
		_ = RefreshSavedProcessorsAsync ();
		_ = RefreshCachedPackagesAsync ();
		}

	public ObservableCollection<PackageSearchResultViewModel> SearchResults
		{
		get;
		}

	public ObservableCollection<SavedProcessorCredentialViewModel> SavedProcessors
		{
		get;
		}

	public ObservableCollection<CachedPackageInfoViewModel> CachedPackages
		{
		get;
		}

	public AsyncRelayCommand SearchCommand
		{
		get;
		}

	public AsyncRelayCommand InspectCommand
		{
		get;
		}

	public AsyncRelayCommand UseCachedPackageCommand
		{
		get;
		}

	public AsyncRelayCommand DeleteCachedPackageCommand
		{
		get;
		}

	public AsyncRelayCommand ShowCachedPackageInfoCommand
		{
		get;
		}

	public AsyncRelayCommand DiscoverProcessorsCommand
		{
		get;
		}

	public AsyncRelayCommand AddProcessorCommand
		{
		get;
		}

	public AsyncRelayCommand DeleteProcessorCommand
		{
		get;
		}

	public AsyncRelayCommand InstallCommand
		{
		get;
		}

	public string FeedUrl
		{
		get => feedUrl;
		set => SetProperty (ref feedUrl, value);
		}

	public string SearchTerm
		{
		get => searchTerm;
		set => SetProperty (ref searchTerm, value);
		}

	public string ProcessorDisplayName
		{
		get => processorDisplayName;
		set => SetProperty (ref processorDisplayName, value);
		}

	public string ProcessorHost
		{
		get => processorHost;
		set
			{
			if (SetProperty (ref processorHost, value))
				{
				InstallCommand.RaiseCanExecuteChanged ();
				}
			}
		}

	public SavedProcessorCredentialViewModel? SelectedSavedProcessor
		{
		get => selectedSavedProcessor;
		set
			{
			if (SetProperty (ref selectedSavedProcessor, value))
				{
				ApplySelectedProcessor (value);
				DeleteProcessorCommand.RaiseCanExecuteChanged ();
				}
			}
		}

	public CachedPackageInfoViewModel? SelectedCachedPackage
		{
		get => selectedCachedPackage;
		set
			{
			if (SetProperty (ref selectedCachedPackage, value))
				{
				ShowCachedPackageInfoCommand.RaiseCanExecuteChanged ();
				UseCachedPackageCommand.RaiseCanExecuteChanged ();
				DeleteCachedPackageCommand.RaiseCanExecuteChanged ();
				}
			}
		}

	public string Username
		{
		get => username;
		set
			{
			if (SetProperty (ref username, value))
				{
				InstallCommand.RaiseCanExecuteChanged ();
				}
			}
		}

	public string Password
		{
		get => password;
		set => SetProperty (ref password, value);
		}

	public bool RememberCredentials
		{
		get => rememberCredentials;
		set => SetProperty (ref rememberCredentials, value);
		}

	public bool OnlyV1CompliantPackages
		{
		get => onlyV1CompliantPackages;
		set => SetProperty (ref onlyV1CompliantPackages, value);
		}

	public bool ShowDiscoveryDiagnostics
		{
		get => showDiscoveryDiagnostics;
		set => SetProperty (ref showDiscoveryDiagnostics, value);
		}

	public bool IsDiscoveringProcessors
		{
		get => isDiscoveringProcessors;
		private set
			{
			if (SetProperty (ref isDiscoveringProcessors, value))
				{
				DiscoverProcessorsCommand.RaiseCanExecuteChanged ();
				}
			}
		}

	public bool IsStatusError
		{
		get => isStatusError;
		private set => SetProperty (ref isStatusError, value);
		}

	public string StatusMessage
		{
		get => statusMessage;
		set => SetStatus (value, isError: false);
		}

	public string CrestronDriverPackageJsonContent
		{
		get => crestronDriverPackageJsonContent;
		set => SetProperty (ref crestronDriverPackageJsonContent, value);
		}

	public string SelectedPackageDetails
		{
		get => selectedPackageDetails;
		set => SetProperty (ref selectedPackageDetails, value);
		}

	public string SelectedCachedPackageDetails
		{
		get => selectedCachedPackageDetails;
		set => SetProperty (ref selectedCachedPackageDetails, value);
		}

	public string DiscoveryDiagnostics
		{
		get => discoveryDiagnostics;
		set => SetProperty (ref discoveryDiagnostics, value);
		}

	public string PackageEntries
		{
		get => packageEntries;
		set => SetProperty (ref packageEntries, value);
		}

	public string StagedDriverPackagePath
		{
		get => stagedDriverPackagePath;
		set => SetProperty (ref stagedDriverPackagePath, value);
		}

	public PackageSearchResultViewModel? SelectedPackage
		{
		get => selectedPackage;
		set
			{
			if (SetProperty (ref selectedPackage, value))
				{
				InspectCommand.RaiseCanExecuteChanged ();
				}
			}
		}

	private async Task SearchAsync ()
		{
		StatusMessage = onlyV1CompliantPackages
			 ? "Searching feed for v1-compliant driver packages..."
			 : "Searching feed...";
		SearchResults.Clear ();
		SelectedPackageDetails = "Select Info on a package entry to view more details.";
		SelectedCachedPackageDetails = "Select Info on a cached package entry to view more details.";
		CrestronDriverPackageJsonContent = string.Empty;
		PackageEntries = string.Empty;
		selectedDriverPackage = null;
		StagedDriverPackagePath = "No package has been staged yet.";
		InstallCommand.RaiseCanExecuteChanged ();

		var results = await nugetPackageService.SearchPackagesAsync (FeedUrl, SearchTerm, 30, onlyV1CompliantPackages);
		foreach (var result in results)
			{
			SearchResults.Add (new PackageSearchResultViewModel (result));
			}

		StatusMessage = onlyV1CompliantPackages
			 ? $"Found {SearchResults.Count} v1-compliant package(s)."
			 : $"Found {SearchResults.Count} package(s).";
		}

	public void ShowPackageInfo (PackageSearchResultViewModel? package)
		{
		if (package is null)
			{
			return;
			}

		SelectedPackage = package;
		SelectedPackageDetails = string.Join (Environment.NewLine, [
			$"Package ID: {package.Id}",
			$"Version: {package.Version}",
			$"Authors: {package.Authors}",
			$"Downloads: {package.DownloadCount}",
			$"Compliance: {package.Compliance}",
			string.Empty,
			package.Description
		]);

		StatusMessage = $"Showing package information for {package.Id}.";
		}

	public void ShowCachedPackageInfo (CachedPackageInfoViewModel? cachedPackage)
		{
		if (cachedPackage is null)
			{
			return;
			}

		SelectedCachedPackage = cachedPackage;
		SelectedCachedPackageDetails = string.Join (Environment.NewLine, [
			$"Package ID: {cachedPackage.PackageId}",
			$"Version: {cachedPackage.Version}",
			$"Cached archive: {cachedPackage.Model.PackageArchivePath}",
			$"Extracted driver package: {cachedPackage.DriverPackagePath}"
		]);

		StatusMessage = $"Showing cached package information for {cachedPackage.PackageId}.";
		}

	private Task ShowSelectedCachedPackageInfoAsync ()
		{
		ShowCachedPackageInfo (SelectedCachedPackage);
		return Task.CompletedTask;
		}

	private async Task InspectSelectedAsync ()
		{
		if (SelectedPackage is null)
			{
			return;
			}

		StatusMessage = $"Downloading {SelectedPackage.Id}...";
		var packagePath = await nugetPackageService.DownloadPackageAsync (FeedUrl, SelectedPackage.Id, SelectedPackage.Version, cacheDirectory);

		StatusMessage = "Inspecting package contents...";
		var extractDirectory = Path.Combine (cacheDirectory, "Extracted", SelectedPackage.Id, SelectedPackage.Version);
		selectedDriverPackage = await packageInspectionService.InspectPackageAsync (SelectedPackage.Id, SelectedPackage.Version, packagePath, extractDirectory);
		CrestronDriverPackageJsonContent = selectedDriverPackage.CrestronDriverPackageJsonContent;
		PackageEntries = string.Join (Environment.NewLine, selectedDriverPackage.Entries);
		StagedDriverPackagePath = selectedDriverPackage.DriverPackagePath;
		InstallCommand.RaiseCanExecuteChanged ();
		await RefreshCachedPackagesAsync ();
		StatusMessage = $"Ready to upload {Path.GetFileName (selectedDriverPackage.DriverPackagePath)}.";
		}

	private async Task UseSelectedCachedPackagesAsync ()
		{
		var selectedPackages = CachedPackages.Where (package => package.IsSelected).ToArray ();
		if (selectedPackages.Length == 0)
			{
			return;
			}

		var selectedCachedPackageInfo = selectedPackages[0];
		var extractDirectory = Path.Combine (cacheDirectory, "Extracted", selectedCachedPackageInfo.PackageId, selectedCachedPackageInfo.Version);
		selectedDriverPackage = await packageInspectionService.InspectPackageAsync (selectedCachedPackageInfo.PackageId, selectedCachedPackageInfo.Version, selectedCachedPackageInfo.Model.PackageArchivePath, extractDirectory);
		CrestronDriverPackageJsonContent = selectedDriverPackage.CrestronDriverPackageJsonContent;
		PackageEntries = string.Join (Environment.NewLine, selectedDriverPackage.Entries);
		StagedDriverPackagePath = selectedDriverPackage.DriverPackagePath;
		InstallCommand.RaiseCanExecuteChanged ();
		StatusMessage = selectedPackages.Length == 1
			? $"Using cached package {selectedCachedPackageInfo.DisplayLabel}."
			: $"Selected {selectedPackages.Length} cached package(s); staged {selectedCachedPackageInfo.DisplayLabel}.";
		}

	private async Task DeleteSelectedCachedPackagesAsync ()
		{
		var selectedPackages = CachedPackages.Where (package => package.IsSelected).ToArray ();
		if (selectedPackages.Length == 0)
			{
			return;
			}

		foreach (var cachedPackage in selectedPackages)
			{
			await nugetPackageService.DeleteCachedPackageAsync (cachedPackage.Model);
			}

		await RefreshCachedPackagesAsync ();
		SelectedCachedPackageDetails = "Select Info on a cached package entry to view more details.";
		StatusMessage = selectedPackages.Length == 1
			? $"Deleted cached package {selectedPackages[0].DisplayLabel}."
			: $"Deleted {selectedPackages.Length} cached package(s).";
		}

	private void ApplySelectedProcessor (SavedProcessorCredentialViewModel? processor)
		{
		if (processor is null)
			{
			return;
			}

		ProcessorDisplayName = processor.DisplayName;
		ProcessorHost = processor.Host;
		if (!string.IsNullOrWhiteSpace (processor.Username))
			{
			Username = processor.Username;
			}
		if (!string.IsNullOrWhiteSpace (processor.Password))
			{
			Password = processor.Password;
			}
		}

	private Task AddProcessorAsync ()
		{
		SelectedSavedProcessor = null;
		ProcessorDisplayName = string.Empty;
		ProcessorHost = string.Empty;
		Username = string.Empty;
		Password = string.Empty;
		StatusMessage = "Ready to add a new processor.";
		return Task.CompletedTask;
		}

	private async Task DiscoverProcessorsAsync ()
		{
		IsDiscoveringProcessors = true;
		try
			{
			SetStatus ("Discovering Crestron processors on the local network...", isError: false);
			DiscoveryDiagnostics = string.Empty;
			var discoveryResult = await processorDiscoveryService.DiscoverAsync (TimeSpan.FromSeconds (3));
			DiscoveryDiagnostics = string.Join (Environment.NewLine, discoveryResult.Diagnostics);
			var supportedProcessors = discoveryResult.Processors
				.Where (processor => processor.IsSupportedCrestronHomeProcessor)
				.ToArray ();
			var merged = SavedProcessors.ToDictionary (processor => processor.Host, StringComparer.OrdinalIgnoreCase);

			foreach (var discoveredProcessor in supportedProcessors)
				{
				var discoveredCredential = new SavedProcessorCredential
					{
					DisplayName = discoveredProcessor.Hostname,
					Host = discoveredProcessor.IPAddress.ToString (),
					Username = string.Empty,
					Password = string.Empty
					};

				merged[discoveredCredential.Host] = new SavedProcessorCredentialViewModel (discoveredCredential);
				await credentialStore.SaveAsync (new ProcessorConnectionInfo
					{
					DisplayName = discoveredCredential.DisplayName,
					Host = discoveredCredential.Host,
					Username = discoveredCredential.Username,
					Password = discoveredCredential.Password,
					RememberCredentials = true
					});
				}

			SavedProcessors.Clear ();
			foreach (var processor in merged.Values.OrderBy (processor => processor.Label))
				{
				SavedProcessors.Add (processor);
				}

			if (supportedProcessors.Length > 0)
				{
				var firstDiscovered = supportedProcessors
					.Select (processor => SavedProcessors.FirstOrDefault (saved => string.Equals (saved.Host, processor.IPAddress.ToString (), StringComparison.OrdinalIgnoreCase)))
					.FirstOrDefault (saved => saved is not null);

				if (firstDiscovered is not null)
					{
					SelectedSavedProcessor = firstDiscovered;
					}
				}

			SetStatus (
				supportedProcessors.Length == 0
					? "No Crestron processors discovered on the local network."
					: $"Discovered {supportedProcessors.Length} Crestron Home processor(s).",
				isError: supportedProcessors.Length == 0);
			}
		catch (Exception ex)
			{
			DiscoveryDiagnostics = string.Join (
				Environment.NewLine,
				new[]
					{
					DiscoveryDiagnostics,
					$"ERROR: {ex.Message}"
					}.Where (line => !string.IsNullOrWhiteSpace (line)));
			SetStatus ($"Discovery failed: {ex.Message}", isError: true);
			}
		finally
			{
			IsDiscoveringProcessors = false;
			}
		}

	private void SetStatus (string message, bool isError)
		{
		SetProperty (ref statusMessage, message, nameof (StatusMessage));
		IsStatusError = isError;
		}

	private async Task DeleteSelectedProcessorsAsync ()
		{
		var selectedProcessors = SavedProcessors.Where (processor => processor.IsSelected).ToArray ();
		if (selectedProcessors.Length == 0 && SelectedSavedProcessor is not null)
			{
			selectedProcessors = [SelectedSavedProcessor];
			}
		if (selectedProcessors.Length == 0)
			{
			return;
			}

		foreach (var processor in selectedProcessors)
			{
			await credentialStore.DeleteAsync (processor.Host, 22);
			}
		await RefreshSavedProcessorsAsync ();
		await AddProcessorAsync ();
		StatusMessage = selectedProcessors.Length == 1
			? $"Removed saved processor {selectedProcessors[0].Label}."
			: $"Removed {selectedProcessors.Length} saved processor(s).";
		}

	private async Task RefreshSavedProcessorsAsync ()
		{
		var savedProcessors = await credentialStore.ListAsync ();
		SavedProcessors.Clear ();
		foreach (var savedProcessor in savedProcessors)
			{
			SavedProcessors.Add (new SavedProcessorCredentialViewModel (savedProcessor));
			}

		InstallCommand.RaiseCanExecuteChanged ();
		DeleteProcessorCommand.RaiseCanExecuteChanged ();
		}

	private async Task RefreshCachedPackagesAsync ()
		{
		var cachedPackages = await nugetPackageService.ListCachedPackagesAsync (cacheDirectory);
		CachedPackages.Clear ();
		foreach (var cachedPackage in cachedPackages)
			{
			CachedPackages.Add (new CachedPackageInfoViewModel (cachedPackage));
			}

		ShowCachedPackageInfoCommand.RaiseCanExecuteChanged ();
		UseCachedPackageCommand.RaiseCanExecuteChanged ();
		DeleteCachedPackageCommand.RaiseCanExecuteChanged ();
		}

	private async Task InstallAsync ()
		{
		if (selectedDriverPackage is null)
			{
			return;
			}

		StatusMessage = "Uploading driver package to processor...";
		var selectedProcessors = SavedProcessors.Where (processor => processor.IsSelected).ToArray ();
		if (selectedProcessors.Length == 0 && SelectedSavedProcessor is not null)
			{
			selectedProcessors = [SelectedSavedProcessor];
			}

		if (selectedProcessors.Length == 0)
			{
			selectedProcessors = [new SavedProcessorCredentialViewModel (new SavedProcessorCredential
				{
				DisplayName = ProcessorDisplayName,
				Host = ProcessorHost,
				Username = Username,
				Password = Password
				})];
			}

		foreach (var processor in selectedProcessors)
			{
			var connection = new ProcessorConnectionInfo
				{
				DisplayName = processor.DisplayName,
				Host = processor.Host,
				Username = string.IsNullOrWhiteSpace (processor.Username) ? Username : processor.Username,
				Password = string.IsNullOrWhiteSpace (processor.Password) ? Password : processor.Password,
				RememberCredentials = RememberCredentials
				};

			await sftpDriverDeploymentService.UploadDriverAsync (selectedDriverPackage.DriverPackagePath, connection);

			if (RememberCredentials)
				{
				await credentialStore.SaveAsync (connection);
				}
			}

		await RefreshSavedProcessorsAsync ();

		StatusMessage = selectedProcessors.Length == 1
			? "Driver package uploaded to /user/ThirdPartyDrivers/Import."
			: $"Driver package uploaded to {selectedProcessors.Length} processors.";
		}
	}
