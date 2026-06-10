using System.IO;
using System.Collections.ObjectModel;
using NugetForCrestronDrivers.App.Infrastructure;
using NugetForCrestronDrivers.Core.Abstractions;
using NugetForCrestronDrivers.Core.Models;

namespace NugetForCrestronDrivers.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly INuGetPackageService nugetPackageService;
    private readonly IPackageInspectionService packageInspectionService;
    private readonly ICredentialStore credentialStore;
    private readonly ISftpDriverDeploymentService sftpDriverDeploymentService;
    private readonly string cacheDirectory;
    private string feedUrl = "https://api.nuget.org/v3/index.json";
    private string searchTerm = "Crestron";
    private string processorHost = string.Empty;
    private int processorPort = 22;
    private string username = string.Empty;
    private string password = string.Empty;
    private bool rememberCredentials = true;
    private string statusMessage = "Ready.";
    private string driversJsonContent = string.Empty;
    private string packageEntries = string.Empty;
    private PackageSearchResultViewModel? selectedPackage;
    private DriverPackageInfo? selectedDriverPackage;

    public MainViewModel(INuGetPackageService nugetPackageService, IPackageInspectionService packageInspectionService, ICredentialStore credentialStore, ISftpDriverDeploymentService sftpDriverDeploymentService)
    {
        this.nugetPackageService = nugetPackageService;
        this.packageInspectionService = packageInspectionService;
        this.credentialStore = credentialStore;
        this.sftpDriverDeploymentService = sftpDriverDeploymentService;
        cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NugetForCrestronDrivers", "Cache");
        SearchResults = new ObservableCollection<PackageSearchResultViewModel>();
        SearchCommand = new AsyncRelayCommand(SearchAsync);
        InspectCommand = new AsyncRelayCommand(InspectSelectedAsync, () => SelectedPackage is not null);
        LoadCredentialsCommand = new AsyncRelayCommand(LoadCredentialsAsync, () => !string.IsNullOrWhiteSpace(ProcessorHost));
        InstallCommand = new AsyncRelayCommand(InstallAsync, () => selectedDriverPackage is not null && !string.IsNullOrWhiteSpace(ProcessorHost) && !string.IsNullOrWhiteSpace(Username));
    }

    public ObservableCollection<PackageSearchResultViewModel> SearchResults { get; }

    public AsyncRelayCommand SearchCommand { get; }

    public AsyncRelayCommand InspectCommand { get; }

    public AsyncRelayCommand LoadCredentialsCommand { get; }

    public AsyncRelayCommand InstallCommand { get; }

    public string FeedUrl
    {
        get => feedUrl;
        set => SetProperty(ref feedUrl, value);
    }

    public string SearchTerm
    {
        get => searchTerm;
        set => SetProperty(ref searchTerm, value);
    }

    public string ProcessorHost
    {
        get => processorHost;
        set
        {
            if (SetProperty(ref processorHost, value))
            {
                LoadCredentialsCommand.RaiseCanExecuteChanged();
                InstallCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int ProcessorPort
    {
        get => processorPort;
        set => SetProperty(ref processorPort, value);
    }

    public string Username
    {
        get => username;
        set
        {
            if (SetProperty(ref username, value))
            {
                InstallCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Password
    {
        get => password;
        set => SetProperty(ref password, value);
    }

    public bool RememberCredentials
    {
        get => rememberCredentials;
        set => SetProperty(ref rememberCredentials, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    public string DriversJsonContent
    {
        get => driversJsonContent;
        set => SetProperty(ref driversJsonContent, value);
    }

    public string PackageEntries
    {
        get => packageEntries;
        set => SetProperty(ref packageEntries, value);
    }

    public PackageSearchResultViewModel? SelectedPackage
    {
        get => selectedPackage;
        set
        {
            if (SetProperty(ref selectedPackage, value))
            {
                InspectCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private async Task SearchAsync()
    {
        StatusMessage = "Searching feed...";
        SearchResults.Clear();
        DriversJsonContent = string.Empty;
        PackageEntries = string.Empty;
        selectedDriverPackage = null;
        InstallCommand.RaiseCanExecuteChanged();

        var results = await nugetPackageService.SearchPackagesAsync(FeedUrl, SearchTerm, 30);
        foreach (var result in results)
        {
            SearchResults.Add(new PackageSearchResultViewModel(result));
        }

        StatusMessage = $"Found {SearchResults.Count} package(s).";
    }

    private async Task InspectSelectedAsync()
    {
        if (SelectedPackage is null)
        {
            return;
        }

        StatusMessage = $"Downloading {SelectedPackage.Id}...";
        var packagePath = await nugetPackageService.DownloadPackageAsync(FeedUrl, SelectedPackage.Id, SelectedPackage.Version, cacheDirectory);

        StatusMessage = "Inspecting package contents...";
        var extractDirectory = Path.Combine(cacheDirectory, "Extracted", SelectedPackage.Id, SelectedPackage.Version);
        selectedDriverPackage = await packageInspectionService.InspectPackageAsync(SelectedPackage.Id, SelectedPackage.Version, packagePath, extractDirectory);
        DriversJsonContent = selectedDriverPackage.DriversJsonContent;
        PackageEntries = string.Join(Environment.NewLine, selectedDriverPackage.Entries);
        InstallCommand.RaiseCanExecuteChanged();
        StatusMessage = $"Ready to upload {Path.GetFileName(selectedDriverPackage.DriverPackagePath)}.";
    }

    private async Task LoadCredentialsAsync()
    {
        var saved = await credentialStore.GetAsync(ProcessorHost, ProcessorPort);
        if (saved is null)
        {
            StatusMessage = "No saved credentials found for that processor.";
            return;
        }

        Username = saved.Username;
        Password = saved.Password;
        StatusMessage = "Saved credentials loaded.";
    }

    private async Task InstallAsync()
    {
        if (selectedDriverPackage is null)
        {
            return;
        }

        StatusMessage = "Uploading driver package to processor...";
        var connection = new ProcessorConnectionInfo
        {
            Host = ProcessorHost,
            Port = ProcessorPort,
            Username = Username,
            Password = Password,
            RememberCredentials = RememberCredentials
        };

        await sftpDriverDeploymentService.UploadDriverAsync(selectedDriverPackage.DriverPackagePath, connection);

        if (RememberCredentials)
        {
            await credentialStore.SaveAsync(connection);
        }

        StatusMessage = "Driver package uploaded to /user/ThirdPartyDrivers/Import.";
    }
}
