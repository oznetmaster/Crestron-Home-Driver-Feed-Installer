// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Windows.Navigation;
using System.Windows;

namespace CrestronHomeDriverFeedInstaller.App;

public partial class AboutWindow : Window
	{
	public AboutWindow (string applicationName, string version, string targetFramework, string copyright, string repositoryUrl)
		{
		InitializeComponent ();
		DataContext = new AboutWindowViewModel (applicationName, version, targetFramework, copyright, repositoryUrl);
		}

	private void OkButton_OnClick (object sender, RoutedEventArgs e)
		{
		DialogResult = true;
		}

	private void RepositoryLink_OnRequestNavigate (object sender, RequestNavigateEventArgs e)
		{
		Process.Start (new ProcessStartInfo (e.Uri.AbsoluteUri)
			{
			UseShellExecute = true
			});
		e.Handled = true;
		}

	private sealed class AboutWindowViewModel
		{
		public AboutWindowViewModel (string applicationName, string version, string targetFramework, string copyright, string repositoryUrl)
			{
			ApplicationName = applicationName;
			VersionText = $"Version {version}";
			TargetFrameworkText = $"Target framework: {targetFramework}";
			CopyrightText = copyright;
			TrademarkDisclaimerText = "Crestron and Crestron Home are trademarks or registered trademarks of Crestron Electronics, Inc. This project is not affiliated with, endorsed by, or sponsored by Crestron Electronics, Inc.";
			RepositoryUrl = repositoryUrl;
			RepositoryUri = Uri.TryCreate (repositoryUrl, UriKind.Absolute, out var repositoryUri) ? repositoryUri : null;
			}

		public string ApplicationName
			{
			get;
			}

		public string VersionText
			{
			get;
			}

		public string TargetFrameworkText
			{
			get;
			}

		public string CopyrightText
			{
			get;
			}

		public string TrademarkDisclaimerText
			{
			get;
			}

		public string RepositoryUrl
			{
			get;
			}

		public Uri? RepositoryUri
			{
			get;
			}
		}
	}
