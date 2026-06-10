using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.App.ViewModels;

public sealed class SavedProcessorCredentialViewModel
	{
	private bool isSelected;

	public SavedProcessorCredentialViewModel (SavedProcessorCredential model)
		{
		Model = model;
		}

	public SavedProcessorCredential Model
		{
		get;
		}

	public string DisplayName => Model.DisplayName;

	public string Host => Model.Host;

	public int Port => Model.Port;

	public string Username => Model.Username;

	public string Password => Model.Password;

	public string Label => Model.Label;

	public bool IsSelected
		{
		get => isSelected;
		set => isSelected = value;
		}
	}
