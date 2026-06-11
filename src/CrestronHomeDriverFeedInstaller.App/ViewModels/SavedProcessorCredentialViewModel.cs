// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.App.ViewModels;

public sealed class SavedProcessorCredentialViewModel
	{
	private bool isSelected;
	private SavedProcessorCredential model;

	public SavedProcessorCredentialViewModel (SavedProcessorCredential model)
		{
		this.model = model;
		}

	public SavedProcessorCredential Model
		{
		get => model;
		private set => model = value;
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

	public void Update (SavedProcessorCredential updatedModel)
		{
		Model = updatedModel;
		}
	}
