// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace CrestronHomeDriverFeedInstaller.Core.Abstractions;

public interface ICredentialProtector
	{
	byte[] Protect (byte[] payload);

	byte[] Unprotect (byte[] payload);
	}
