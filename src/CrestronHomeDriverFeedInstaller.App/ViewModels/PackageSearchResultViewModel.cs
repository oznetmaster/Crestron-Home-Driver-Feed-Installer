// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.App.ViewModels;

public sealed class PackageSearchResultViewModel
	{
	public PackageSearchResultViewModel (PackageSearchResult model)
		{
		Model = model;
		}

	public PackageSearchResult Model
		{
		get;
		}

	public string Id => Model.Id;

	public string Version => Model.Version;

	public string Description => string.IsNullOrWhiteSpace (Model.Description) ? "No description provided." : Model.Description;

	public string Authors => string.IsNullOrWhiteSpace (Model.Authors) ? "Unknown" : Model.Authors;

	public string DownloadCount => Model.DownloadCount?.ToString ("N0") ?? "n/a";

	public string Compliance => Model.IsV1Compliant ? "V1 compliant" : "Not verified";
	}
