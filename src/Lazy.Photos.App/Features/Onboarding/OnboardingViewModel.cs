using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lazy.Photos.App.Services;

namespace Lazy.Photos.App.Features.Onboarding;

public partial class OnboardingViewModel : ObservableObject
{
	private readonly IAppSettingsService _settingsService;
	private readonly IAuthenticationService _authService;

	public OnboardingViewModel(
		IAppSettingsService settingsService,
		IAuthenticationService authService)
	{
		_settingsService = settingsService;
		_authService = authService;
	}

	[ObservableProperty]
	private int currentStep = 0; // 0=Welcome, 1=Server, 2=SignIn

	// Server configuration
	[ObservableProperty]
	private string apiUrl = "http://192.168.0.161:5175";

	[ObservableProperty]
	private bool isTestingConnection;

	[ObservableProperty]
	private string connectionStatus = string.Empty;

	[ObservableProperty]
	private Color connectionStatusColor = Colors.Gray;

	// Authentication
	[ObservableProperty]
	private string email = string.Empty;

	[ObservableProperty]
	private string password = string.Empty;

	[ObservableProperty]
	private string confirmPassword = string.Empty;

	[ObservableProperty]
	private string displayName = string.Empty;

	[ObservableProperty]
	private bool isRegisterMode;

	[ObservableProperty]
	private bool isSigningIn;

	[ObservableProperty]
	private bool isRegistering;

	[ObservableProperty]
	private string errorMessage = string.Empty;

	public bool CanContinueFromServerStep => !string.IsNullOrEmpty(ConnectionStatus)
		&& ConnectionStatus.StartsWith("✓");

	partial void OnIsTestingConnectionChanged(bool value)
	{
		OnPropertyChanged(nameof(IsNotTesting));
	}

	partial void OnIsSigningInChanged(bool value)
	{
		OnPropertyChanged(nameof(IsNotSigningIn));
	}

	partial void OnIsRegisteringChanged(bool value)
	{
		OnPropertyChanged(nameof(IsNotRegistering));
	}

	partial void OnIsRegisterModeChanged(bool value)
	{
		ErrorMessage = string.Empty;
	}

	partial void OnConnectionStatusChanged(string value)
	{
		OnPropertyChanged(nameof(CanContinueFromServerStep));
		OnPropertyChanged(nameof(HasConnectionStatus));
	}

	partial void OnErrorMessageChanged(string value)
	{
		OnPropertyChanged(nameof(HasErrorMessage));
	}

	public bool IsNotTesting => !IsTestingConnection;
	public bool IsNotSigningIn => !IsSigningIn;
	public bool IsNotRegistering => !IsRegistering;
	public bool HasConnectionStatus => !string.IsNullOrEmpty(ConnectionStatus);
	public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

	[RelayCommand]
	private Task NextStepAsync()
	{
		CurrentStep++;
		return Task.CompletedTask;
	}

	[RelayCommand]
	private Task PreviousStepAsync()
	{
		if (CurrentStep > 0) CurrentStep--;
		return Task.CompletedTask;
	}

	[RelayCommand]
	private void ShowRegister()
	{
		IsRegisterMode = true;
	}

	[RelayCommand]
	private void ShowSignIn()
	{
		IsRegisterMode = false;
	}

	[RelayCommand]
	private async Task TestConnectionAsync()
	{
		IsTestingConnection = true;
		ConnectionStatus = "Testing...";
		ConnectionStatusColor = Colors.Gray;

		try
		{
			var httpClient = new HttpClient { BaseAddress = new Uri(ApiUrl) };
			var response = await httpClient.GetAsync("/health");

			if (response.IsSuccessStatusCode)
			{
				ConnectionStatus = "✓ Connected successfully";
				ConnectionStatusColor = Colors.Green;
			}
			else
			{
				ConnectionStatus = $"✗ Server error ({response.StatusCode})";
				ConnectionStatusColor = Colors.Red;
			}
		}
		catch (Exception ex)
		{
			ConnectionStatus = $"✗ Connection failed: {ex.Message}";
			ConnectionStatusColor = Colors.Red;
		}
		finally
		{
			IsTestingConnection = false;
		}
	}

	[RelayCommand]
	private async Task SignInAsync()
	{
		if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
		{
			ErrorMessage = "Please enter email and password";
			return;
		}

		IsSigningIn = true;
		ErrorMessage = string.Empty;

		try
		{
			// Save API URL first
			await _settingsService.SetApiUrlAsync(ApiUrl);

			// Attempt login
			var result = await _authService.LoginAsync(Email, Password);

			if (result.Success)
			{
				// Login successful, complete onboarding
				await CompleteOnboardingAsync();
			}
			else
			{
				ErrorMessage = result.ErrorMessage ?? "Login failed";
			}
		}
		catch (Exception ex)
		{
			ErrorMessage = $"Error: {ex.Message}";
		}
		finally
		{
			IsSigningIn = false;
		}
	}

	[RelayCommand]
	private async Task RegisterAsync()
	{
		if (string.IsNullOrWhiteSpace(DisplayName))
		{
			ErrorMessage = "Please enter a display name";
			return;
		}

		if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
		{
			ErrorMessage = "Please fill in all required fields";
			return;
		}

		if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
		{
			ErrorMessage = "Passwords do not match";
			return;
		}

		IsRegistering = true;
		ErrorMessage = string.Empty;

		try
		{
			await _settingsService.SetApiUrlAsync(ApiUrl);

			var result = await _authService.RegisterAsync(Email, Password, DisplayName);

			if (result.Success)
			{
				await CompleteOnboardingAsync();
			}
			else
			{
				ErrorMessage = result.ErrorMessage ?? "Registration failed";
			}
		}
		catch (Exception ex)
		{
			ErrorMessage = $"Error: {ex.Message}";
		}
		finally
		{
			IsRegistering = false;
		}
	}

	[RelayCommand]
	private async Task SkipToMainAppAsync()
	{
		await CompleteOnboardingAsync();
	}

	[RelayCommand]
	private async Task SetPresetUrlAsync(string preset)
	{
		ApiUrl = preset switch
		{
			"local" => "http://192.168.0.161:5175",
			"pi" => "http://raspberrypi.local",
			_ => ApiUrl
		};

		// Auto-test connection after preset selection
		await TestConnectionAsync();
	}

	private async Task CompleteOnboardingAsync()
	{
		await _settingsService.SetFirstLaunchCompleteAsync();

		// Pop onboarding page, returning to main TabBar
		await Shell.Current.Navigation.PopToRootAsync();
	}
}
