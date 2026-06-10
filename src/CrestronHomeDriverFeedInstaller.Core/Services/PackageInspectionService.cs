using System.IO.Compression;
using System.Text;

using CrestronHomeDriverFeedInstaller.Core.Abstractions;
using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.Core.Services;

public sealed class PackageInspectionService : IPackageInspectionService
	{
	public async Task<DriverPackageInfo> InspectPackageAsync (string packageId, string version, string packageArchivePath, string workingDirectory, CancellationToken cancellationToken = default)
		{
		Directory.CreateDirectory (workingDirectory);

		using var packageArchive = ZipFile.OpenRead (packageArchivePath);
		var pkgEntry = packageArchive.Entries.FirstOrDefault (entry => entry.FullName.EndsWith (".pkg", StringComparison.OrdinalIgnoreCase));
		if (pkgEntry is null)
			{
			throw new InvalidOperationException ("The selected NuGet package does not contain a .pkg driver package.");
			}

		var driverPackagePath = Path.Combine (workingDirectory, pkgEntry.Name);
		await using (var pkgStream = pkgEntry.Open ())
		await using (var output = File.Create (driverPackagePath))
			{
			await pkgStream.CopyToAsync (output, cancellationToken);
			}

		string crestronDriverPackageJsonContent;
		var crestronDriverPackageJsonEntry = packageArchive.Entries.FirstOrDefault (entry => entry.FullName.EndsWith ("crestron-driver-package.json", StringComparison.OrdinalIgnoreCase));
		if (crestronDriverPackageJsonEntry is null)
			{
			crestronDriverPackageJsonContent = "crestron-driver-package.json was not found in the NuGet package.";
			}
		else
			{
			using var reader = new StreamReader (crestronDriverPackageJsonEntry.Open (), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
			crestronDriverPackageJsonContent = await reader.ReadToEndAsync (cancellationToken);
			}

		return new DriverPackageInfo
			{
			PackageId = packageId,
			Version = version,
			PackageArchivePath = packageArchivePath,
			DriverPackagePath = driverPackagePath,
			CrestronDriverPackageJsonContent = crestronDriverPackageJsonContent,
			Entries = packageArchive.Entries.Select (entry => entry.FullName).OrderBy (name => name).ToArray ()
			};
		}
	}
