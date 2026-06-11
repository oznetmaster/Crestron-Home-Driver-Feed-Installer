// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace CrestronHomeDriverFeedInstaller.Core.Models;

public sealed class SavedProcessorCredential
	{
	public string DisplayName { get; init; } = string.Empty;

	public string Host { get; init; } = string.Empty;

	public int Port
		{
		get; init;
		}

	public string Username { get; init; } = string.Empty;

	public string Password { get; init; } = string.Empty;

	public string Label => string.IsNullOrWhiteSpace (DisplayName) ? Host : $"{DisplayName} ({Host})";
	}
