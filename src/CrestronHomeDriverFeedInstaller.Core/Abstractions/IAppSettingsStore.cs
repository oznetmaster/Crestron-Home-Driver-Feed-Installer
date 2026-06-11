// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.Core.Abstractions;

public interface IAppSettingsStore
	{
	Task<AppSettings> LoadAsync (CancellationToken cancellationToken = default);

	Task SaveAsync (AppSettings settings, CancellationToken cancellationToken = default);

	AppSettings Load ();

	void Save (AppSettings settings);
	}