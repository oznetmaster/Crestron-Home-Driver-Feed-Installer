// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using CrestronHomeDriverFeedInstaller.Core.Abstractions;

namespace CrestronHomeDriverFeedInstaller.App.Services;

public sealed class WindowsAppDataPathProvider : IAppDataPathProvider
	{
	private readonly string applicationDataDirectory = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), "CrestronHomeDriverFeedInstaller");
	private readonly string extractedPackageDirectory = Path.Combine (Path.GetTempPath (), "CrestronHomeDriverFeedInstaller", "Extracted");

	public string ApplicationDataDirectory => applicationDataDirectory;

	public string CacheDirectory => Path.Combine (applicationDataDirectory, "Cache");

	public string ExtractedPackageDirectory => extractedPackageDirectory;
	}
