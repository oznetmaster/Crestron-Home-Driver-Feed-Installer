using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.App.ViewModels;

public sealed class CachedPackageInfoViewModel
	{
	private bool isSelected;

	public CachedPackageInfoViewModel (CachedPackageInfo model)
		{
		Model = model;
		}

	public CachedPackageInfo Model
		{
		get;
		}

	public string PackageId => Model.PackageId;

	public string Version => Model.Version;

	public string DisplayLabel => Model.DisplayLabel;

	public string DriverPackagePath => Model.ExtractedDriverPackagePath ?? "Not yet extracted";

	public bool IsSelected
		{
		get => isSelected;
		set => isSelected = value;
		}
	}

