// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using CrestronHomeDriverFeedInstaller.Core.Abstractions;
using CrestronHomeDriverFeedInstaller.Core.Models;

using Renci.SshNet;

namespace CrestronHomeDriverFeedInstaller.Core.Services;

public sealed class SftpDriverDeploymentService : ISftpDriverDeploymentService
	{
	private const string ImportDirectory = "/user/ThirdPartyDrivers/Import";

	public Task UploadDriverAsync (string driverPackagePath, ProcessorConnectionInfo connectionInfo, CancellationToken cancellationToken = default)
		{
		if (!File.Exists (driverPackagePath))
			{
			throw new FileNotFoundException ("Driver package file was not found.", driverPackagePath);
			}

		return Task.Run (() =>
		{
			using var client = new SftpClient (connectionInfo.Host, connectionInfo.Port, connectionInfo.Username, connectionInfo.Password);
			client.Connect ();

			if (!client.Exists (ImportDirectory))
				{
				throw new InvalidOperationException ($"The processor import folder '{ImportDirectory}' was not found.");
				}

			using var stream = File.OpenRead (driverPackagePath);
			var remotePath = $"{ImportDirectory}/{Path.GetFileName (driverPackagePath)}";
			client.UploadFile (stream, remotePath, canOverride: true);
			client.Disconnect ();
		}, cancellationToken);
		}
	}
