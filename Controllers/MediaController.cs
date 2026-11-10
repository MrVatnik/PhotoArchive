using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using PhotoArchive.Data;
using PhotoArchive.Services;

namespace PhotoArchive.Controllers
{
    /// <summary>Serves generated thumbnails and previews, rendering them on first request.</summary>
    public class MediaController : Controller
    {
        private readonly PhotoContext _context;
        private readonly IPhotoFileService _files;
        private readonly IWebHostEnvironment _env;

        public MediaController(PhotoContext context, IPhotoFileService files, IWebHostEnvironment env)
        {
            _context = context;
            _files = files;
            _env = env;
        }

        [HttpGet("/media/photo/{id:int}/{kind:regex(^(thumb|preview)$)}")]
        public async Task<IActionResult> Photo(int id, string kind, CancellationToken ct)
        {
            var photo = await _context.Photos.AsNoTracking().Include(p => p.Film)
                .FirstOrDefaultAsync(p => p.Id == id, ct);
            if (photo == null)
                return NotFound();

            var path = await _files.GetOrCreateAsync(photo, kind == "thumb" ? ImageKind.Thumb : ImageKind.Preview, ct);
            if (path == null)
            {
                // original missing: show the placeholder, but don't let the browser keep it
                Response.Headers.CacheControl = "no-store";
                return PhysicalFile(Path.Combine(_env.WebRootPath, "images", "No_image.JPG"), "image/jpeg");
            }

            // the file name carries a content stamp ({id}_{stamp}.jpg), so it doubles as the ETag
            Response.Headers.CacheControl = "public, max-age=300";
            var etag = new EntityTagHeaderValue("\"" + Path.GetFileNameWithoutExtension(path) + "\"");
            return PhysicalFile(path, "image/jpeg", System.IO.File.GetLastWriteTimeUtc(path), etag);
        }
    }
}
