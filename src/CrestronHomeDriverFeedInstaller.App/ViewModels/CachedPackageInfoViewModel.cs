// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.App.ViewModels;

public sealed class CachedPackageInfoViewModel
	{
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

	public string Authors => string.IsNullOrWhiteSpace (Model.Authors) ? "Unknown" : Model.Authors;

	public string Description => string.IsNullOrWhiteSpace (Model.Description) ? "No description provided." : Model.Description;

	public string? LatestAvailableVersion => Model.LatestAvailableVersion;

	public bool HasNewerVersionAvailable => Model.HasNewerVersionAvailable;

	public string DisplayLabel => Model.DisplayLabel;

	public string DriverPackagePath => Model.ExtractedDriverPackagePath ?? "Not yet extracted";
	}

