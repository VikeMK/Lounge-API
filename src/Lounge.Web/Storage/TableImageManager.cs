using Lounge.Web.Data;
using Lounge.Web.Data.Entities;
using Lounge.Web.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lounge.Web.Storage
{
    /// <summary>
    /// What /TableImage/{id}.png should respond with: a redirect to the bucket, or (when the image had to be
    /// generated on the spot) the PNG itself.
    /// </summary>
    public sealed record TableImageResponse(string? RedirectUrl, byte[]? Image);

    /// <summary>
    /// Table image logic: generating images (via gb.hlorenzi.com), storing them in the bucket, keeping the
    /// TableImages rows (which images exist) in sync, and purging Cloudflare's cache when an image changes.
    /// Scoped, because it uses the request's DbContext.
    /// </summary>
    public class TableImageManager
    {
        private readonly ApplicationDbContext _context;
        private readonly ITableImageStorage _storage;
        private readonly ICdnCachePurger _cachePurger;
        private readonly ILogger<TableImageManager> _logger;

        public TableImageManager(
            ApplicationDbContext context,
            ITableImageStorage storage,
            ICdnCachePurger cachePurger,
            ILogger<TableImageManager> logger)
        {
            _context = context;
            _storage = storage;
            _cachePurger = cachePurger;
            _logger = logger;
        }

        public Task<bool> TableExistsAsync(int tableId) =>
            _context.Tables.AnyAsync(t => t.Id == tableId);

        /// <summary>
        /// Resolves an image request without ever downloading from the bucket:
        /// 1. TableImages row exists        -> redirect to the bucket URL
        /// 2. no row, but file is in bucket -> record it, then redirect
        /// 3. not in bucket                 -> generate, upload, record, purge, and return the PNG directly
        /// If the bucket is unreachable, the image is generated and returned without storing it.
        /// </summary>
        public async Task<TableImageResponse> GetImageForRequestAsync(int tableId)
        {
            bool recorded = await _context.TableImages.AnyAsync(ti => ti.TableId == tableId);
            if (recorded)
                return new TableImageResponse(_storage.GetPublicUrl(tableId), null);

            bool storageAvailable = true;
            try
            {
                if (await _storage.ExistsAsync(tableId))
                {
                    await RecordImageAsync(tableId);
                    return new TableImageResponse(_storage.GetPublicUrl(tableId), null);
                }
            }
            catch (Exception e) when (e is not DbUpdateException)
            {
                _logger.LogWarning(e, "Could not check image bucket for table {TableId}; generating without storing", tableId);
                storageAvailable = false;
            }

            var image = await GenerateFromDatabaseAsync(tableId);
            if (storageAvailable)
                await TryStoreAsync(tableId, image);

            return new TableImageResponse(null, image);
        }

        /// <summary>
        /// Generates the image from the given scores, uploads it, records it and purges the old one from Cloudflare.
        /// Returns false (and logs) if generating or uploading fails. Call after the table's changes are saved.
        /// </summary>
        public async Task<bool> TryGenerateAndStoreAsync(int tableId, string tier, (string Player, string? CountryCode, int Score)[][] scores)
        {
            byte[] image;
            try
            {
                image = await TableUtils.GetImageDataAsync(TableUtils.BuildUrl(tier, scores));
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to generate image for table {TableId}", tableId);
                return false;
            }

            return await TryStoreAsync(tableId, image);
        }

        /// <summary>
        /// Removes the stored image, its row and Cloudflare's cached copy, so a stale image isn't served.
        /// The next request regenerates it.
        /// </summary>
        public async Task InvalidateAsync(int tableId)
        {
            // Row first, so requests stop redirecting to the old image; then the file, so the HEAD fallback
            // doesn't find it; then the cached copy at Cloudflare.
            try
            {
                await _context.TableImages.Where(ti => ti.TableId == tableId).ExecuteDeleteAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to delete TableImages row for table {TableId}", tableId);
            }

            try
            {
                await _storage.DeleteAsync(tableId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to delete stored image for table {TableId}", tableId);
            }

            await TryPurgeAsync(tableId);
        }

        /// <summary>
        /// Uploads, records and purges. Returns true once the image is stored, even if the purge fails:
        /// the new image is in the bucket, Cloudflare may just keep serving the previous one until purged.
        /// </summary>
        private async Task<bool> TryStoreAsync(int tableId, byte[] image)
        {
            try
            {
                await _storage.UploadAsync(tableId, image);
                await RecordImageAsync(tableId);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to store image for table {TableId}", tableId);
                return false;
            }

            // Always purge after an upload: an edited table has an old image cached, and even a brand new table
            // may have a cached 404 from someone requesting its URL before the image existed.
            await TryPurgeAsync(tableId);
            return true;
        }

        private async Task TryPurgeAsync(int tableId)
        {
            try
            {
                await _cachePurger.PurgeAsync(_storage.GetPublicUrl(tableId));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to purge Cloudflare cache for table {TableId}; a stale image may be served until it is purged", tableId);
            }
        }

        /// <summary>Inserts or updates the TableImages row for this table (UpdatedOn = now).</summary>
        private async Task RecordImageAsync(int tableId)
        {
            var now = DateTime.UtcNow;

            int updated = await _context.TableImages
                .Where(ti => ti.TableId == tableId)
                .ExecuteUpdateAsync(s => s.SetProperty(ti => ti.UpdatedOn, now));
            if (updated > 0)
                return;

            var row = new TableImage { TableId = tableId, UpdatedOn = now };
            _context.TableImages.Add(row);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Another request inserted the row at the same moment; that's fine, it exists now.
            }
            finally
            {
                _context.Entry(row).State = EntityState.Detached;
            }
        }

        private async Task<byte[]> GenerateFromDatabaseAsync(int tableId)
        {
            var table = await _context.Tables
                .AsNoTracking()
                .Where(t => t.Id == tableId)
                .Select(t => new { t.Tier, t.NumTeams })
                .FirstAsync();

            var tableScores = await _context.TableScores
                .AsNoTracking()
                .Where(s => s.TableId == tableId)
                .Select(s => new { s.Team, s.Score, s.Player.Name, s.Player.CountryCode })
                .ToListAsync();

            var scores = new (string Player, string? CountryCode, int Score)[table.NumTeams][];
            for (int i = 0; i < table.NumTeams; i++)
            {
                scores[i] = tableScores
                    .Where(score => score.Team == i)
                    .Select(score => (score.Name, score.CountryCode, score.Score))
                    .ToArray();
            }

            return await TableUtils.GetImageDataAsync(TableUtils.BuildUrl(table.Tier, scores));
        }
    }
}
