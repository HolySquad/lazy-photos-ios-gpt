using Lazy.Photos.Data;
using Lazy.Photos.Data.Contracts;
using Lazy.Photos.App.Features.Photos.Models;
using Lazy.Photos.App.Features.Photos.Services;

namespace Lazy.Photos.App.Features.Albums;

public sealed class ApiAlbumService : IAlbumService
{
	private readonly IPhotosApiClient _apiClient;

	public ApiAlbumService(IPhotosApiClient apiClient)
	{
		_apiClient = apiClient;
	}

	public async Task<IReadOnlyList<AlbumDto>> GetAlbumsAsync(CancellationToken cancellationToken = default)
	{
		var response = await _apiClient.GetAlbumsAsync(cancellationToken);
		return response.Albums.Select(Map).ToList();
	}

	public async Task<AlbumDto?> GetAlbumAsync(string id, CancellationToken cancellationToken = default)
	{
		if (!Guid.TryParse(id, out var albumId))
			return null;

		var response = await _apiClient.GetAlbumsAsync(cancellationToken);
		var match = response.Albums.FirstOrDefault(a => a.Id == albumId);
		return match is null ? null : Map(match);
	}

	public async Task<AlbumDto> CreateAlbumAsync(string name, CancellationToken cancellationToken = default)
	{
		var request = new AlbumCreateRequest(name);
		var album = await _apiClient.CreateAlbumAsync(request, cancellationToken);
		return Map(album);
	}

	public async Task<AlbumDto> UpdateAlbumAsync(string id, string name, CancellationToken cancellationToken = default)
	{
		if (!Guid.TryParse(id, out var albumId))
			throw new InvalidOperationException($"Invalid album id '{id}'");

		var request = new AlbumUpdateRequest(name, CoverPhotoId: null);
		var album = await _apiClient.UpdateAlbumAsync(albumId, request, cancellationToken);
		return Map(album);
	}

	public async Task DeleteAlbumAsync(string id, CancellationToken cancellationToken = default)
	{
		if (!Guid.TryParse(id, out var albumId))
			throw new InvalidOperationException($"Invalid album id '{id}'");

		await _apiClient.DeleteAlbumAsync(albumId, cancellationToken);
	}

	public Task<int> GetPhotoCountAsync(string albumId, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(0);
	}

	public async Task<IReadOnlyList<PhotoItem>> GetAlbumPhotosAsync(string albumId, CancellationToken cancellationToken = default)
	{
		if (!Guid.TryParse(albumId, out var albumGuid))
			return Array.Empty<PhotoItem>();

		var response = await _apiClient.GetAlbumPhotosAsync(albumGuid, cancellationToken);
		return response.Photos.Select(PhotoItemMapper.FromDto).ToList();
	}

	public async Task AddPhotoToAlbumAsync(string albumId, string photoId, CancellationToken cancellationToken = default)
	{
		if (!Guid.TryParse(albumId, out var albumGuid))
			throw new InvalidOperationException($"Invalid album id '{albumId}'");

		if (!Guid.TryParse(photoId, out var photoGuid))
			throw new InvalidOperationException($"Invalid photo id '{photoId}'");

		await _apiClient.AddPhotoToAlbumAsync(albumGuid, new AlbumItemRequest(photoGuid), cancellationToken);
	}

	public async Task RemovePhotoFromAlbumAsync(string albumId, string photoId, CancellationToken cancellationToken = default)
	{
		if (!Guid.TryParse(albumId, out var albumGuid))
			throw new InvalidOperationException($"Invalid album id '{albumId}'");

		if (!Guid.TryParse(photoId, out var photoGuid))
			throw new InvalidOperationException($"Invalid photo id '{photoId}'");

		await _apiClient.RemovePhotoFromAlbumAsync(albumGuid, photoGuid, cancellationToken);
	}

	private static AlbumDto Map(Lazy.Photos.Data.Contracts.AlbumDto dto)
	{
		return new AlbumDto(
			dto.Id.ToString(),
			dto.Name,
			dto.CoverPhotoId?.ToString(),
			dto.CreatedAt,
			dto.UpdatedAt);
	}
}
