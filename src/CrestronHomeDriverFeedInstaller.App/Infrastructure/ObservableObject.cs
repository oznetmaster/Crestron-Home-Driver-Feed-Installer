// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CrestronHomeDriverFeedInstaller.App.Infrastructure;

public abstract class ObservableObject : INotifyPropertyChanged
	{
	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged ([CallerMemberName] string? propertyName = null)
		{
		PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
		}

	protected bool SetProperty<T> (ref T field, T value, [CallerMemberName] string? propertyName = null)
		{
		if (EqualityComparer<T>.Default.Equals (field, value))
			{
			return false;
			}

		field = value;
		OnPropertyChanged (propertyName);
		return true;
		}
	}
