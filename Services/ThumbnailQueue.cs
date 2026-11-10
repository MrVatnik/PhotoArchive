using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoArchive.Data;

namespace PhotoArchive.Services
{
    public interface IThumbnailQueue
    {
        /// <summary>Queues generation of the thumbnail (and optionally the preview) of a photo.</summary>
        void Enqueue(int photoId, bool includePreview = false);
    }

    /// <summary>
    /// Generates thumbnails/previews in the background so pages never wait for a whole film to render.
    /// On startup it optionally queues every photo, so missing thumbnails are ready before anyone asks.
    /// </summary>
    public class ThumbnailQueue : BackgroundService, IThumbnailQueue
    {
        private readonly Channel<(int PhotoId, bool Preview)> _channel = Channel.CreateUnbounded<(int, bool)>();
        private readonly IServiceScopeFactory _scopes;
        private readonly IPhotoFileService _files;
        private readonly StorageOptions _options;
        private readonly ILogger<ThumbnailQueue> _logger;

        public ThumbnailQueue(IServiceScopeFactory scopes, IPhotoFileService files, IOptions<StorageOptions> options, ILogger<ThumbnailQueue> logger)
        {
            _scopes = scopes;
            _files = files;
            _options = options.Value;
            _logger = logger;
        }

        public void Enqueue(int photoId, bool includePreview = false) => _channel.Writer.TryWrite((photoId, includePreview));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_options.WarmUpThumbnailsOnStartup)
                await QueueAllAsync(stoppingToken);

            await foreach (var (photoId, preview) in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<PhotoContext>();
                    var photo = await db.Photos.AsNoTracking().Include(p => p.Film)
                        .FirstOrDefaultAsync(p => p.Id == photoId, stoppingToken);
                    if (photo == null)
                        continue;

                    await _files.GetOrCreateAsync(photo, ImageKind.Thumb, stoppingToken);
                    if (preview)
                        await _files.GetOrCreateAsync(photo, ImageKind.Preview, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background rendering failed for photo {Id}", photoId);
                }
            }
        }

        private async Task QueueAllAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PhotoContext>();
                var ids = await db.Photos.OrderByDescending(p => p.Id).Select(p => p.Id).ToListAsync(ct);
                foreach (var id in ids)
                    Enqueue(id);
                _logger.LogInformation("Queued {Count} photos for thumbnail warm-up.", ids.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Thumbnail warm-up failed to start.");
            }
        }
    }
}
