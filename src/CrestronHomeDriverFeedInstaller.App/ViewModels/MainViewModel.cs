// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.IO;

using CrestronHomeDriverFeedInstaller.App.Infrastructure;
using CrestronHomeDriverFeedInstaller.Core.Abstractions;
using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.App.ViewModels;

public sealed class MainViewModel : ObservableObject
	{
	private static readonly string _selectedPackageInfoMessage = "Select Info on a package entry to view more details.";
	private static readonly string _selectedCachedPackageInfoMessage = "Select Info on a cached package entry to view more details.";

	private readonly INuGetPackageService nugetPackageService;
	private readonly IPackageInspectionService packageInspectionService;
	private readonly IAppSettingsStore appSettingsStore;
	private readonly ICredentialStore credentialStore;
	private readonly IProcessorDiscoveryService processorDiscoveryService;
	private readonly ISftpDriverDeploymentService sftpDriverDeploymentService;
	private AppSettings appSettings = new ();
	private string cacheDirectory = new AppSettings ().CacheDirectory;
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
	private string selectedPackageDetails = _selectedPackageInfoMessage;
	private string selectedCachedPackageDetails = _selectedCachedPackageInfoMessage;
	private string discoveryDiagnostics = string.Empty;
	private string crestronDriverPackageJsonContent = string.Empty;
	private string packageEntries = string.Empty;
	private string stagedDriverPackagePath = "No package has been staged yet.";
	private PackageSearchResultViewModel? selectedPackage;
	private IReadOnlyList<PackageSearchResultViewModel> selectedPackages = Array.Empty<PackageSearchResultViewModel> ();
	private DriverPackageInfo? selectedDriverPackage;
	private SavedProcessorCredentialViewModel? selectedSavedProcessor;
	private CachedPackageInfoViewModel? selectedCachedPackage;
	private IReadOnlyList<CachedPackageInfoViewModel> selectedCachedPackages = Array.Empty<CachedPackageInfoViewModel> ();
	private bool isDiscoveringProcessors;
	private bool isApplyingSelectedProcessor;

	public MainViewModel (INuGetPackageService nugetPackageService, IPackageInspectionService packageInspectionService, IAppSettingsStore appSettingsStore, ICredentialStore credentialStore, IProcessorDiscoveryService processorDiscoveryService, ISftpDriverDeploymentService sftpDriverDeploymentService)
		{
		this.nugetPackageService = nugetPackageService;
		this.packageInspectionService = packageInspectionService;
		this.appSettingsStore = appSettingsStore;
		this.credentialStore = credentialStore;
		this.processorDiscoveryService = processorDiscoveryService;
		this.sftpDriverDeploymentService = sftpDriverDeploymentService;
		SearchResults = new ObservableCollection<PackageSearchResultViewModel> ();
		SavedProcessors = new ObservableCollection<SavedProcessorCredentialViewModel> ();
		CachedPackages = new ObservableCollection<CachedPackageInfoViewModel> ();
		SearchCommand = new AsyncRelayCommand (SearchAsync);
		InspectCommand = new AsyncRelayCommand (InspectSelectedAsync, () => SelectedPackage is not null);
		UseCachedPackageCommand = new AsyncRelayCommand (UploadSelectedCachedPackagesAsync, () => SelectedCachedPackages.Count > 0 && CanUploadToProcessor ());
		UpdateCachedPackageCommand = new AsyncRelayCommand (UpdateSelectedCachedPackagesAsync, () => SelectedCachedPackages.Count > 0);
		DeleteCachedPackageCommand = new AsyncRelayCommand (DeleteSelectedCachedPackagesAsync, () => SelectedCachedPackages.Count > 0);
		ShowCachedPackageInfoCommand = new AsyncRelayCommand (ShowSelectedCachedPackageInfoAsync, () => SelectedCachedPackage is not null);
		DiscoverProcessorsCommand = new AsyncRelayCommand (DiscoverProcessorsAsync, () => !isDiscoveringProcessors);
		AddProcessorCommand = new AsyncRelayCommand (AddProcessorAsync);
		DeleteProcessorCommand = new AsyncRelayCommand (DeleteSelectedProcessorsAsync, () => SavedProcessors.Any (processor => processor.IsSelected));
		InstallCommand = new AsyncRelayCommand (InstallAsync, () => selectedDriverPackage is not null && SavedProcessors.Any (processor => processor.IsSelected || ReferenceEquals (processor, SelectedSavedProcessor)) && !string.IsNullOrWhiteSpace (Username));
		_ = InitializeAsync ();
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

	public AsyncRelayCommand UpdateCachedPackageCommand
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

	public string InspectButtonText => SelectedPackages.Count == 1 ? "Download and inspect selected package" : "Download and inspect selected packages";

	public string UseCachedPackageButtonText => "Upload selected";

	public string UpdateCachedPackageButtonText => "Update selected";

	public string DeleteCachedPackageButtonText => "Delete selected";

	public bool CanDeleteCachedPackages => SelectedCachedPackages.Count > 0;

	public string FeedUrl
		{
		get => feedUrl;
		set
			{
			if (SetProperty (ref feedUrl, value))
				{
				appSettings.FeedUrl = value;
				PersistAppSettings ();
				}
			}
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
				UseCachedPackageCommand.RaiseCanExecuteChanged ();
				}
			}
		}

	public IReadOnlyList<CachedPackageInfoViewModel> SelectedCachedPackages
		{
		get => selectedCachedPackages;
		set
			{
			selectedCachedPackages = value;
			UseCachedPackageCommand.RaiseCanExecuteChanged ();
			UpdateCachedPackageCommand.RaiseCanExecuteChanged ();
			DeleteCachedPackageCommand.RaiseCanExecuteChanged ();
			OnPropertyChanged (nameof (UseCachedPackageButtonText));
			OnPropertyChanged (nameof (UpdateCachedPackageButtonText));
			OnPropertyChanged (nameof (DeleteCachedPackageButtonText));
			OnPropertyChanged (nameof (CanDeleteCachedPackages));
			UpdateSelectionStatusMessage ();
			}
		}

	public IReadOnlyList<PackageSearchResultViewModel> SelectedPackages
		{
		get => selectedPackages;
		set
			{
			selectedPackages = value;
			InspectCommand.RaiseCanExecuteChanged ();
			OnPropertyChanged (nameof (InspectButtonText));
			UpdateSelectionStatusMessage ();
			}
		}

	public SavedProcessorCredentialViewModel? SelectedSavedProcessor
		{
		get => selectedSavedProcessor;
		set
			{
			if (!ReferenceEquals (selectedSavedProcessor, value))
				{
				PersistSelectedProcessorDraft (selectedSavedProcessor);
				}

			if (SetProperty (ref selectedSavedProcessor, value))
				{
				_ = ApplySelectedProcessorAsync (value);
				if (value is not null)
					{
					appSettings.LastUsedProcessorHost = value.Host;
					PersistAppSettings ();
					}
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
				UseCachedPackageCommand.RaiseCanExecuteChanged ();
				PersistSelectedProcessorDraft ();
				}
			}
		}

	public string Password
		{
		get => password;
		set
			{
			if (SetProperty (ref password, value))
				{
				PersistSelectedProcessorDraft ();
				}
			}
		}

	public bool RememberCredentials
		{
		get => rememberCredentials;
		set
			{
			if (SetProperty (ref rememberCredentials, value))
				{
				UseCachedPackageCommand.RaiseCanExecuteChanged ();
				}
			}
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
				if (value is null)
					{
					SelectedPackages = Array.Empty<PackageSearchResultViewModel> ();
					}
				else if (!SelectedPackages.Contains (value))
					{
					SelectedPackages = [value];
					}
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
		SelectedPackages = Array.Empty<PackageSearchResultViewModel> ();
		SelectedPackageDetails = _selectedPackageInfoMessage;
		SelectedCachedPackageDetails = _selectedCachedPackageInfoMessage;
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
		StagedDriverPackagePath = $"Package info selected: {package.Id} {package.Version}";

		StatusMessage = $"Showing package information for {package.Id}.";
		}

	public async Task ShowCachedPackageInfoAsync (CachedPackageInfoViewModel? cachedPackage)
		{
		if (cachedPackage is null)
			{
			return;
			}

		SelectedCachedPackage = cachedPackage;
		SelectedPackageDetails = string.Join (Environment.NewLine, [
			$"Package ID: {cachedPackage.PackageId}",
			$"Version: {cachedPackage.Version}",
			$"Authors: {cachedPackage.Authors}",
			$"Latest available: {cachedPackage.LatestAvailableVersion ?? "Current"}",
			string.Empty,
			cachedPackage.Description
		]);
		SelectedCachedPackageDetails = string.Join (Environment.NewLine, [
			$"Package ID: {cachedPackage.PackageId}",
			$"Version: {cachedPackage.Version}",
			$"Cached archive: {cachedPackage.Model.PackageArchivePath}",
			$"Extracted driver package: {cachedPackage.DriverPackagePath}"
		]);

		var driverPackage = await InspectCachedPackageAsync (cachedPackage);
		CrestronDriverPackageJsonContent = driverPackage.CrestronDriverPackageJsonContent;
		PackageEntries = string.Join (Environment.NewLine, driverPackage.Entries);
		StagedDriverPackagePath = driverPackage.DriverPackagePath;

		StatusMessage = $"Showing cached package information for {cachedPackage.PackageId}.";
		}

	private Task ShowSelectedCachedPackageInfoAsync ()
		{
		return ShowCachedPackageInfoAsync (SelectedCachedPackage);
		}

	private async Task InspectSelectedAsync ()
		{
		var packagesToInspect = SelectedPackages.Count > 0
			? SelectedPackages.ToArray ()
			: SelectedPackage is null
				? Array.Empty<PackageSearchResultViewModel> ()
				: [SelectedPackage];

		if (packagesToInspect.Length == 0)
			{
			return;
			}

		DriverPackageInfo? lastInspectedPackage = null;

		for (var index = 0; index < packagesToInspect.Length; index++)
			{
			var package = packagesToInspect[index];
			StatusMessage = packagesToInspect.Length == 1
				? $"Downloading {package.Id}..."
				: $"Downloading package {index + 1} of {packagesToInspect.Length}: {package.Id}...";
			var packagePath = await nugetPackageService.DownloadPackageAsync (FeedUrl, package.Id, package.Version, cacheDirectory);

			StatusMessage = packagesToInspect.Length == 1
				? "Inspecting package contents..."
				: $"Inspecting package {index + 1} of {packagesToInspect.Length}: {package.Id}...";
			var extractDirectory = Path.Combine (cacheDirectory, "Extracted", package.Id, package.Version);
			lastInspectedPackage = await packageInspectionService.InspectPackageAsync (package.Id, package.Version, packagePath, extractDirectory);
			}

		if (lastInspectedPackage is null)
			{
			return;
			}

		selectedDriverPackage = lastInspectedPackage;
		CrestronDriverPackageJsonContent = selectedDriverPackage.CrestronDriverPackageJsonContent;
		PackageEntries = string.Join (Environment.NewLine, selectedDriverPackage.Entries);
		StagedDriverPackagePath = selectedDriverPackage.DriverPackagePath;
		InstallCommand.RaiseCanExecuteChanged ();
		await RefreshCachedPackagesAsync ();
		StatusMessage = packagesToInspect.Length == 1
			? $"Ready to upload {Path.GetFileName (selectedDriverPackage.DriverPackagePath)}."
			: $"Downloaded and inspected {packagesToInspect.Length} packages. Staged {Path.GetFileName (selectedDriverPackage.DriverPackagePath)}.";
		}

	private async Task UseSelectedCachedPackagesAsync ()
		{
		var selectedPackages = SelectedCachedPackages.ToArray ();
		if (selectedPackages.Length == 0)
			{
			return;
			}

		var selectedCachedPackageInfo = selectedPackages[0];
		selectedDriverPackage = await InspectCachedPackageAsync (selectedCachedPackageInfo);
		CrestronDriverPackageJsonContent = selectedDriverPackage.CrestronDriverPackageJsonContent;
		PackageEntries = string.Join (Environment.NewLine, selectedDriverPackage.Entries);
		StagedDriverPackagePath = selectedDriverPackage.DriverPackagePath;
		InstallCommand.RaiseCanExecuteChanged ();
		StatusMessage = selectedPackages.Length == 1
			? $"Using cached package {selectedCachedPackageInfo.DisplayLabel}."
			: $"Selected {selectedPackages.Length} cached package(s); staged {selectedCachedPackageInfo.DisplayLabel}.";
		}

	private async Task UploadSelectedCachedPackagesAsync ()
		{
		await UseSelectedCachedPackagesAsync ();
		if (selectedDriverPackage is null)
			{
			return;
			}

		await InstallAsync ();
		}

	private async Task DeleteSelectedCachedPackagesAsync ()
		{
		var selectedPackages = SelectedCachedPackages.ToArray ();
		if (selectedPackages.Length == 0)
			{
			return;
			}

		foreach (var cachedPackage in selectedPackages)
			{
			await nugetPackageService.DeleteCachedPackageAsync (cachedPackage.Model);
			}

		await RefreshCachedPackagesAsync ();
		SelectedCachedPackages = Array.Empty<CachedPackageInfoViewModel> ();
		SelectedCachedPackageDetails = _selectedCachedPackageInfoMessage;
		StatusMessage = selectedPackages.Length == 1
			? $"Deleted cached package {selectedPackages[0].DisplayLabel}."
			: $"Deleted {selectedPackages.Length} cached package(s).";
		}

	private async Task UpdateSelectedCachedPackagesAsync ()
		{
		var selectedPackages = SelectedCachedPackages
			.Where (package => package.HasNewerVersionAvailable && !string.IsNullOrWhiteSpace (package.LatestAvailableVersion))
			.ToArray ();
		if (selectedPackages.Length == 0)
			{
			SetStatus ("No selected cached packages have updates available.", isError: false);
			return;
			}

		for (var index = 0; index < selectedPackages.Length; index++)
			{
			var cachedPackage = selectedPackages[index];
			StatusMessage = selectedPackages.Length == 1
				? $"Updating cached package {cachedPackage.DisplayLabel}..."
				: $"Updating cached package {index + 1} of {selectedPackages.Length}: {cachedPackage.DisplayLabel}...";

			await nugetPackageService.DeleteCachedPackageAsync (cachedPackage.Model);
			await nugetPackageService.DownloadPackageAsync (FeedUrl, cachedPackage.PackageId, cachedPackage.LatestAvailableVersion!, cacheDirectory);
			}

		await RefreshCachedPackagesAsync ();
		SelectedCachedPackages = Array.Empty<CachedPackageInfoViewModel> ();
		SelectedCachedPackageDetails = _selectedCachedPackageInfoMessage;
		StatusMessage = selectedPackages.Length == 1
			? "Updated 1 cached package to the latest version."
			: $"Updated {selectedPackages.Length} cached packages to the latest versions.";
		}

	private async Task ApplySelectedProcessorAsync (SavedProcessorCredentialViewModel? processor)
		{
		isApplyingSelectedProcessor = true;
		try
			{
		if (processor is null)
			{
			ProcessorDisplayName = string.Empty;
			ProcessorHost = string.Empty;
			Username = string.Empty;
			Password = string.Empty;
			return;
			}

		ProcessorDisplayName = processor.DisplayName;
		ProcessorHost = processor.Host;
		var savedCredential = await credentialStore.GetAsync (processor.Host, processor.Port);
		Username = savedCredential?.Username ?? processor.Username;
		Password = savedCredential?.Password ?? processor.Password;
		}
		finally
			{
			isApplyingSelectedProcessor = false;
			}
		}

	private Task AddProcessorAsync ()
		{
		PersistSelectedProcessorDraft (SelectedSavedProcessor);
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

		RestoreLastUsedProcessorSelection ();

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

	private void UpdateSelectionStatusMessage ()
		{
		if (!IsStatusError
			&& (statusMessage.StartsWith ("Selected cached packages:", StringComparison.Ordinal)
				|| statusMessage.StartsWith ("Selected packages:", StringComparison.Ordinal)))
			{
			StatusMessage = "Ready.";
			}
		}

	private bool CanUploadToProcessor ()
		{
		return (!string.IsNullOrWhiteSpace (ProcessorHost) || SavedProcessors.Any (processor => processor.IsSelected || ReferenceEquals (processor, SelectedSavedProcessor)))
			&& !string.IsNullOrWhiteSpace (Username);
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
		if (selectedProcessors.Any (processor => string.Equals (processor.Host, appSettings.LastUsedProcessorHost, StringComparison.OrdinalIgnoreCase)))
			{
			appSettings.LastUsedProcessorHost = null;
			await PersistAppSettingsAsync ();
			}
		await RefreshSavedProcessorsAsync ();
		await AddProcessorAsync ();
		StatusMessage = selectedProcessors.Length == 1
			? $"Removed saved processor {selectedProcessors[0].Label}."
			: $"Removed {selectedProcessors.Length} saved processor(s).";
		}

	private async Task RefreshSavedProcessorsAsync ()
		{
		var hostToRestore = SelectedSavedProcessor?.Host ?? appSettings.LastUsedProcessorHost;
		var savedProcessors = await credentialStore.ListAsync ();
		SavedProcessors.Clear ();
		foreach (var savedProcessor in savedProcessors)
			{
			SavedProcessors.Add (new SavedProcessorCredentialViewModel (savedProcessor));
			}

		if (!string.IsNullOrWhiteSpace (hostToRestore))
			{
			var matchingProcessor = SavedProcessors.FirstOrDefault (processor => string.Equals (processor.Host, hostToRestore, StringComparison.OrdinalIgnoreCase));
			if (matchingProcessor is not null)
				{
				SelectedSavedProcessor = matchingProcessor;
				}
			}

		InstallCommand.RaiseCanExecuteChanged ();
		DeleteProcessorCommand.RaiseCanExecuteChanged ();
		}

	private async Task RefreshCachedPackagesAsync ()
		{
		var cachedPackages = await nugetPackageService.ListCachedPackagesAsync (cacheDirectory);
		var wasValidatingCachedPackages = false;
		if (cachedPackages.Count > 0)
			{
			wasValidatingCachedPackages = true;
			SetStatus ($"Validating {cachedPackages.Count} cached package(s) for newer versions...", isError: false);
			await FlagCachedPackagesWithNewerVersionsAsync (cachedPackages);
			}

		CachedPackages.Clear ();
		foreach (var cachedPackage in cachedPackages)
			{
			CachedPackages.Add (new CachedPackageInfoViewModel (cachedPackage));
			}

		SelectedCachedPackage = CachedPackages.FirstOrDefault (package => SelectedCachedPackage is not null && string.Equals (package.Model.CacheKey, SelectedCachedPackage.Model.CacheKey, StringComparison.Ordinal));

		ShowCachedPackageInfoCommand.RaiseCanExecuteChanged ();
		UseCachedPackageCommand.RaiseCanExecuteChanged ();
		DeleteCachedPackageCommand.RaiseCanExecuteChanged ();

		if (wasValidatingCachedPackages
			&& !IsStatusError
			&& statusMessage.StartsWith ("Validating ", StringComparison.Ordinal))
			{
			StatusMessage = "Ready.";
			}
		}

	private async Task FlagCachedPackagesWithNewerVersionsAsync (IReadOnlyList<CachedPackageInfo> cachedPackages)
		{
		foreach (var cachedPackage in cachedPackages)
			{
			try
				{
				var latestVersion = await nugetPackageService.GetLatestVersionAsync (FeedUrl, cachedPackage.PackageId);
				cachedPackage.LatestAvailableVersion = latestVersion;
				cachedPackage.HasNewerVersionAvailable = IsNewerVersionAvailable (cachedPackage.Version, latestVersion);
				}
			catch (Exception)
				{
				cachedPackage.LatestAvailableVersion = null;
				cachedPackage.HasNewerVersionAvailable = false;
				}
			}
		}

	private static bool IsNewerVersionAvailable (string currentVersion, string? latestVersion)
		{
		if (string.IsNullOrWhiteSpace (latestVersion)
			|| !NuGet.Versioning.NuGetVersion.TryParse (currentVersion, out var current)
			|| !NuGet.Versioning.NuGetVersion.TryParse (latestVersion, out var latest))
			{
			return false;
			}

		return latest > current;
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

		appSettings.LastUsedProcessorHost = selectedProcessors[0].Host;
		await PersistAppSettingsAsync ();

		await RefreshSavedProcessorsAsync ();

		StatusMessage = selectedProcessors.Length == 1
			? "Driver package uploaded to /user/ThirdPartyDrivers/Import."
			: $"Driver package uploaded to {selectedProcessors.Length} processors.";
		}

	private async Task InitializeAsync ()
		{
		appSettings = await appSettingsStore.LoadAsync ();
		if (!string.IsNullOrWhiteSpace (appSettings.FeedUrl))
			{
			feedUrl = appSettings.FeedUrl;
			}

		if (!string.IsNullOrWhiteSpace (appSettings.CacheDirectory))
			{
			cacheDirectory = appSettings.CacheDirectory;
			}

		await RefreshSavedProcessorsAsync ();
		await RefreshCachedPackagesAsync ();
		RestoreLastUsedProcessorSelection ();
		}

	private async Task<DriverPackageInfo> InspectCachedPackageAsync (CachedPackageInfoViewModel cachedPackage)
		{
		var extractDirectory = Path.Combine (cacheDirectory, "Extracted", cachedPackage.PackageId, cachedPackage.Version);
		return await packageInspectionService.InspectPackageAsync (cachedPackage.PackageId, cachedPackage.Version, cachedPackage.Model.PackageArchivePath, extractDirectory);
		}

	private void RestoreLastUsedProcessorSelection ()
		{
		if (string.IsNullOrWhiteSpace (appSettings.LastUsedProcessorHost))
			{
			return;
			}

		var matchingProcessor = SavedProcessors.FirstOrDefault (processor => string.Equals (processor.Host, appSettings.LastUsedProcessorHost, StringComparison.OrdinalIgnoreCase));
		if (matchingProcessor is not null && !ReferenceEquals (matchingProcessor, SelectedSavedProcessor))
			{
			SelectedSavedProcessor = matchingProcessor;
			}
		}

	public void PersistSelectedProcessorDraft ()
		{
		PersistSelectedProcessorDraft (SelectedSavedProcessor);
		}

	private void PersistSelectedProcessorDraft (SavedProcessorCredentialViewModel? processor)
		{
		if (isApplyingSelectedProcessor || processor is null || string.IsNullOrWhiteSpace (processor.Host))
			{
			return;
			}

		var updatedCredential = new SavedProcessorCredential
			{
			DisplayName = ProcessorDisplayName,
			Host = ProcessorHost,
			Port = processor.Port,
			Username = Username,
			Password = Password
			};

		var existingIndex = SavedProcessors
			.Select ((savedProcessor, index) => new { savedProcessor, index })
			.FirstOrDefault (entry => string.Equals (entry.savedProcessor.Host, processor.Host, StringComparison.OrdinalIgnoreCase))?
			.index;

		if (existingIndex is int index)
			{
			SavedProcessors[index].Update (updatedCredential);
			}

		if (RememberCredentials && !string.IsNullOrWhiteSpace (updatedCredential.Host))
			{
			_ = credentialStore.SaveAsync (new ProcessorConnectionInfo
				{
				DisplayName = updatedCredential.DisplayName,
				Host = updatedCredential.Host,
				Port = updatedCredential.Port,
				Username = updatedCredential.Username,
				Password = updatedCredential.Password,
				RememberCredentials = true
				});
			}
		}

	private void PersistAppSettings ()
		{
		_ = PersistAppSettingsAsync ();
		}

	private async Task PersistAppSettingsAsync ()
		{
		try
			{
			appSettings.FeedUrl = FeedUrl;
			appSettings.CacheDirectory = cacheDirectory;
			await appSettingsStore.SaveAsync (appSettings);
			}
		catch (Exception)
			{
			}
		}
	}
