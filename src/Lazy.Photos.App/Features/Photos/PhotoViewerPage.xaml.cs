using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos;

public partial class PhotoViewerPage : ContentPage, IQueryAttributable
{
	private readonly PhotoViewerViewModel _viewModel;
	private double _startScale = 1;
	private double _currentScale = 1;
	private double _maxScale = 4;
	private bool _isTransitioning;

	public PhotoViewerPage(PhotoViewerViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
		_viewModel.PropertyChanged += OnViewModelPropertyChanged;
	}

	public PhotoViewerPage()
		: this(Microsoft.Maui.IPlatformApplication.Current?.Services?.GetRequiredService<PhotoViewerViewModel>()
			?? throw new InvalidOperationException("PhotoViewerViewModel not registered in DI container"))
	{
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		PhotoItem? selectedPhoto = null;
		IReadOnlyList<PhotoItem>? contextPhotos = null;

		if (query.TryGetValue("photo", out var value) && value is PhotoItem photo)
			selectedPhoto = photo;

		if (query.TryGetValue("photos", out var photosValue))
		{
			switch (photosValue)
			{
				case IReadOnlyList<PhotoItem> readOnly:
					contextPhotos = readOnly;
					break;
				case IList<PhotoItem> list:
					contextPhotos = new ReadOnlyCollection<PhotoItem>(list);
					break;
				case IEnumerable<PhotoItem> enumerable:
					contextPhotos = enumerable.ToList();
					break;
			}
		}

		if (selectedPhoto == null)
			return;

		if (contextPhotos is { Count: > 0 })
			_viewModel.SetPhotoContext(selectedPhoto, contextPhotos);
		else
			_viewModel.SetPhoto(selectedPhoto);
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_viewModel.PropertyChanged -= OnViewModelPropertyChanged;
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(PhotoViewerViewModel.Photo))
			ResetTransforms();
	}

	private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
	{
		if (PhotoImage == null)
			return;

		switch (e.Status)
		{
			case GestureStatus.Started:
				_startScale = PhotoImage.Scale;
				PhotoImage.AnchorX = e.ScaleOrigin.X;
				PhotoImage.AnchorY = e.ScaleOrigin.Y;
				break;
			case GestureStatus.Running:
				_currentScale = Math.Clamp(_startScale * e.Scale, 1, _maxScale);
				PhotoImage.Scale = _currentScale;
				break;
			case GestureStatus.Completed:
				if (_currentScale <= 1.02)
				{
					ResetTransforms();
					return;
				}
				_startScale = _currentScale;
				PhotoImage.AnchorX = 0.5;
				PhotoImage.AnchorY = 0.5;
				break;
			case GestureStatus.Canceled:
				ResetTransforms();
				break;
		}
	}

	private void ResetTransforms()
	{
		PhotoImage.Scale = 1;
		PhotoImage.TranslationX = 0;
		PhotoImage.TranslationY = 0;
		_currentScale = 1;
		_startScale = 1;
		PhotoImage.AnchorX = 0.5;
		PhotoImage.AnchorY = 0.5;
	}

	private void OnDoubleTap(object? sender, TappedEventArgs e)
	{
		ResetTransforms();
	}

	private async void OnSwipeLeft(object? sender, SwipedEventArgs e)
	{
		await AnimateTransitionAsync(1, _viewModel.NextPhotoCommand);
	}

	private async void OnSwipeRight(object? sender, SwipedEventArgs e)
	{
		await AnimateTransitionAsync(-1, _viewModel.PreviousPhotoCommand);
	}

	private async void OnNextClicked(object? sender, EventArgs e)
	{
		await AnimateTransitionAsync(1, _viewModel.NextPhotoCommand);
	}

	private async void OnPreviousClicked(object? sender, EventArgs e)
	{
		await AnimateTransitionAsync(-1, _viewModel.PreviousPhotoCommand);
	}

	private async Task AnimateTransitionAsync(int direction, IRelayCommand command)
	{
		if (_isTransitioning || PhotoImage == null)
			return;

		if (!command.CanExecute(null))
			return;

		_isTransitioning = true;
		var width = GetTransitionWidth();
		var outX = -direction * width;
		var inX = direction * width;

		await Task.WhenAll(
			PhotoImage.TranslateToAsync(outX, 0, 180, Easing.CubicIn),
			PhotoImage.FadeToAsync(0, 180, Easing.CubicIn));

		command.Execute(null);

		PhotoImage.TranslationX = inX;
		PhotoImage.Opacity = 0;

		await Task.WhenAll(
			PhotoImage.TranslateToAsync(0, 0, 200, Easing.CubicOut),
			PhotoImage.FadeToAsync(1, 200, Easing.CubicOut));

		_isTransitioning = false;
	}

	private double GetTransitionWidth()
	{
		var width = PhotoImage.Width;
		if (width <= 0)
			width = Width;
		if (width <= 0)
			width = Window?.Width ?? 0;

		return width > 0 ? width : 300;
	}

	protected override bool OnBackButtonPressed()
	{
		if (_currentScale > 1)
		{
			ResetTransforms();
			return true;
		}

		return base.OnBackButtonPressed();
	}
}
