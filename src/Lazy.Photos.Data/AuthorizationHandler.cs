using System.Net.Http.Headers;

namespace Lazy.Photos.Data;

/// <summary>
/// DelegatingHandler that adds Bearer token to all authenticated requests
/// </summary>
public class AuthorizationHandler : DelegatingHandler
{
	private readonly IAuthTokenProvider _tokenProvider;

	public AuthorizationHandler(IAuthTokenProvider tokenProvider)
	{
		_tokenProvider = tokenProvider;
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		// Only add token if Authorization header has "Bearer" placeholder
		if (request.Headers.Authorization?.Scheme == "Bearer")
		{
			var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
			if (!string.IsNullOrEmpty(token))
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			}
		}

		return await base.SendAsync(request, cancellationToken);
	}
}
