// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

using CrestronHomeDriverFeedInstaller.Core.Abstractions;

namespace CrestronHomeDriverFeedInstaller.App.Services;

public sealed class WindowsCredentialProtector : ICredentialProtector
	{
	public byte[] Protect (byte[] payload)
		{
		return ProtectedData.Protect (payload, optionalEntropy: null, DataProtectionScope.CurrentUser);
		}

	public byte[] Unprotect (byte[] payload)
		{
		return ProtectedData.Unprotect (payload, optionalEntropy: null, DataProtectionScope.CurrentUser);
		}
	}
