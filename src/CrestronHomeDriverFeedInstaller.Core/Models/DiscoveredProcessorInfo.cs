// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;

namespace CrestronHomeDriverFeedInstaller.Core.Models;

public sealed class DiscoveredProcessorInfo
	{
	private static readonly string[] AllowedCrestronHomeModels = ["CP4-R", "MC4-R", "DIN-AP4-R", "MC4-R-I"];

	public string Hostname { get; init; } = string.Empty;

	public string VersionString { get; init; } = string.Empty;

	public IPAddress IPAddress { get; init; } = IPAddress.None;

	public int Port { get; init; } = 22;

	public string Label => string.IsNullOrWhiteSpace (Hostname)
		 ? $"{IPAddress}"
		 : $"{Hostname} ({IPAddress})";

	public string? Model => Array.Find (AllowedCrestronHomeModels, model => VersionString.Contains (model, StringComparison.OrdinalIgnoreCase));

	public bool IsSupportedCrestronHomeProcessor => Model is not null;
	}
