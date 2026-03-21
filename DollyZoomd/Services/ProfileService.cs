using DollyZoomd.DTOs.Favorites;
using DollyZoomd.DTOs.Profile;
using DollyZoomd.Options;
using DollyZoomd.Repositories.Interfaces;
using DollyZoomd.Services.Interfaces;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace DollyZoomd.Services;

public class ProfileService(
    IProfileRepository profileRepository,
    IFavoritesRepository favoritesRepository,
    IWebHostEnvironment environment,
    IOptions<AvatarOptions> avatarOptions) : IProfileService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".gif"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    private readonly AvatarOptions _avatarOptions = avatarOptions.Value;

    public async Task<UserProfileDto> GetProfileAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = (username ?? string.Empty).Trim();
        if (normalizedUsername.StartsWith('@'))
        {
            normalizedUsername = normalizedUsername[1..];
        }

        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        var user = await profileRepository.GetUserByUsernameAsync(normalizedUsername, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{normalizedUsername}' was not found.");

        var summary = await profileRepository.GetWatchlistSummaryAsync(user.Id, cancellationToken);
        var rawFavorites = await favoritesRepository.GetFavoritesAsync(user.Id, cancellationToken);

        var favorites = rawFavorites.Select(f => new FavoriteDto
        {
            ShowId       = f.ShowId,
            ShowName     = f.Show?.Name ?? string.Empty,
            PosterUrl    = f.Show?.PosterUrl,
            Genres       = ParseGenres(f.Show?.GenresCsv),
            DisplayOrder = f.DisplayOrder
        }).ToList();

        return new UserProfileDto
        {
            Username        = user.Username,
            AvatarUrl       = BuildAvatarUrl(user.AvatarFileName),
            MemberSinceUtc  = user.CreatedAtUtc,
            WatchlistSummary = summary,
            Favorites       = favorites
        };
    }

    public async Task<string> UpdateAvatarAsync(Guid userId, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length <= 0)
        {
            throw new ArgumentException("Avatar file is required.", nameof(file));
        }

        if (file.Length > _avatarOptions.MaxAvatarSizeBytes)
        {
            throw new ArgumentException("Avatar image must be 8 MB or smaller.", nameof(file));
        }

        var extension = Path.GetExtension(file.FileName);
        var hasAllowedExtension = !string.IsNullOrWhiteSpace(extension) && AllowedExtensions.Contains(extension);
        var hasAllowedContentType = AllowedContentTypes.Contains(file.ContentType ?? string.Empty);

        if (!hasAllowedExtension && !hasAllowedContentType)
        {
            throw new ArgumentException("Avatar must be JPEG/JPG, PNG, WebP, or GIF.", nameof(file));
        }

        var user = await profileRepository.GetUserByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");

        var webRootPath = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            throw new InvalidOperationException("Web root path is not configured.");
        }

        var normalizedStoragePath = NormalizeStoragePath(_avatarOptions.StoragePath);
        var avatarDirectory = Path.Combine(webRootPath, normalizedStoragePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(avatarDirectory);

        if (!string.IsNullOrWhiteSpace(user.AvatarFileName))
        {
            var existingFilePath = Path.Combine(avatarDirectory, user.AvatarFileName);
            if (File.Exists(existingFilePath))
            {
                File.Delete(existingFilePath);
            }
        }

        // Always output as JPEG after processing.
        const string fileExtension = ".jpg";
        var avatarFileName = $"{userId:N}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{fileExtension}";
        var avatarFilePath = Path.Combine(avatarDirectory, avatarFileName);

        byte[] processedBytes;
        try
        {
            await using var inputStream = file.OpenReadStream();
            processedBytes = await ProcessAvatarImageAsync(inputStream, cancellationToken);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex) when (ex is SixLabors.ImageSharp.UnknownImageFormatException
                                       or SixLabors.ImageSharp.InvalidImageContentException)
        {
            throw new ArgumentException("The uploaded file is not a valid image.", nameof(file));
        }

        await File.WriteAllBytesAsync(avatarFilePath, processedBytes, cancellationToken);

        await profileRepository.UpdateAvatarFileNameAsync(userId, avatarFileName, cancellationToken);
        return BuildAvatarUrl(avatarFileName) ?? string.Empty;
    }

    private async Task<byte[]> ProcessAvatarImageAsync(Stream inputStream, CancellationToken cancellationToken)
    {
        using var image = await Image.LoadAsync(inputStream, cancellationToken);

        if (image.Width < _avatarOptions.MinInputDimensionPixels || image.Height < _avatarOptions.MinInputDimensionPixels)
        {
            throw new ArgumentException(
                $"Image is too small. Minimum size is {_avatarOptions.MinInputDimensionPixels}\u00d7{_avatarOptions.MinInputDimensionPixels} pixels.");
        }

        if (image.Width > _avatarOptions.MaxInputDimensionPixels || image.Height > _avatarOptions.MaxInputDimensionPixels)
        {
            throw new ArgumentException(
                $"Image dimensions are too large. Maximum allowed is {_avatarOptions.MaxInputDimensionPixels}\u00d7{_avatarOptions.MaxInputDimensionPixels} pixels.");
        }

        var maxSide = _avatarOptions.ResizeMaxSidePixels;
        if (image.Width > maxSide || image.Height > maxSide)
        {
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(maxSide, maxSide),
                Mode = ResizeMode.Max
            }));
        }

        // Flatten transparency so JPEG encoding produces correct output.
        image.Mutate(ctx => ctx.BackgroundColor(Color.White));

        using var ms = new MemoryStream();
        var encoder = new JpegEncoder { Quality = _avatarOptions.JpegQuality };
        await image.SaveAsJpegAsync(ms, encoder, cancellationToken);
        return ms.ToArray();
    }

    private static IReadOnlyList<string> ParseGenres(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return [];
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private string? BuildAvatarUrl(string? avatarFileName)
    {
        if (string.IsNullOrWhiteSpace(avatarFileName))
        {
            return null;
        }

        var normalizedStoragePath = NormalizeStoragePath(_avatarOptions.StoragePath);
        return $"/{normalizedStoragePath}/{avatarFileName}";
    }

    private static string NormalizeStoragePath(string? storagePath)
    {
        var normalized = (storagePath ?? "uploads/avatars")
            .Trim()
            .Replace('\\', '/');

        var trimmed = normalized.Trim('/');
        return string.IsNullOrWhiteSpace(trimmed) ? "uploads/avatars" : trimmed;
    }
}
