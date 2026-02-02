using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos;

public partial class PhotoViewerPage : ContentPage, IQueryAttributable
{
	private readonly PhotoViewerViewModel _viewModel;
	private double _startScale = 1;
	private double _currentScale = 1;
	private double _maxScale = 4;

	public PhotoViewerPage(PhotoViewerViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	public PhotoViewerPage()
		: this(Microsoft.Maui.IPlatformApplication.Current?.Services?.GetRequiredService<PhotoViewerViewModel>()
			?? throw new InvalidOperationException("PhotoViewerViewModel not registered in DI container"))
	{
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("photo", out var value) && value is PhotoItem photo)
			_viewModel.SetPhoto(photo);
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
