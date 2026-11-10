using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhotoArchive.Data;
using PhotoArchive.Models;
using PhotoArchive.Services;

namespace PhotoArchive.Controllers
{
    public class PhotosController : Controller
    {
        private readonly PhotoContext _context;
        private readonly IPhotoFileService _photoFiles;
        private readonly IThumbnailQueue _thumbnails;

        public PhotosController(PhotoContext context, IPhotoFileService photoFiles, IThumbnailQueue thumbnails)
        {
            _context = context;
            _photoFiles = photoFiles;
            _thumbnails = thumbnails;
        }

        // GET: Photos
        public async Task<IActionResult> Index()
        {
            var photoContext = _context.Photos
                .Include(p => p.Film)
                .Include(p => p.Film.Camera).Include(p => p.Film.Recipe)
                .Include(p => p.Film.Recipe.Developer).Include(p => p.Film.Recipe.FilmType)
                .Include(p => p.Film.Camera.Format)
                ;
            return View(await photoContext.ToListAsync());
        }

        // GET: Photos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Photos == null)
            {
                return NotFound();
            }

            var photo = await _context.Photos
                .Include(p => p.Film)
                .Include(p => p.Film.Camera).Include(p => p.Film.Recipe)
                .Include(p => p.Film.Recipe.Developer).Include(p => p.Film.Recipe.FilmType)
                .Include(p => p.Film.Camera.Format)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (photo == null)
            {
                return NotFound();
            }

            return View(photo);
        }

        // GET: Photos/Create
        public IActionResult Create()
        {
            if (_context.Films == null)
            {
                return NotFound();
            }

            List<Film> films = _context.Films
                .Include(p => p.Camera).Include(p => p.Recipe)
                .Include(p => p.Recipe.Developer).Include(p => p.Recipe.FilmType)
                .Include(p => p.Camera.Format).ToList();
            ViewData["FilmId"] = new SelectList(films, "Id", null);
            return View();
        }

        // POST: Photos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Pic,FilmId,Is_Liked,Page,Line,Place_In_Line")] Photo photo)
        {
            if (ModelState.IsValid)
            {
                photo.Film = _context.Films.Find(photo.FilmId);

                _context.Add(photo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            List<Film> Films = _context.Films
                .Include(p => p.Camera).Include(p => p.Recipe)
                .Include(p => p.Recipe.Developer).Include(p => p.Recipe.FilmType)
                .Include(p => p.Camera.Format).ToList();
            ViewData["FilmId"] = new SelectList(Films, "Id", null, photo.FilmId);
            return View(photo);
        }

        // GET: Photos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Photos == null)
            {
                return NotFound();
            }

            var photo = await _context.Photos.FindAsync(id);
            if (photo == null)
            {
                return NotFound();
            }

            List<Film> Films = _context.Films
                .Include(p => p.Camera).Include(p => p.Recipe)
                .Include(p => p.Recipe.Developer).Include(p => p.Recipe.FilmType)
                .Include(p => p.Camera.Format).ToList();
            ViewData["FilmId"] = new SelectList(Films, "Id", null, photo.FilmId);
            return View(photo);
        }

        // POST: Photos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int Id, [Bind("Id,Name,Pic,FilmId,Is_Liked,Page,Line,Place_In_Line")] Photo photo)
        {
            if (Id != photo.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    photo.Film = _context.Films.Find(photo.FilmId);
                    if (photo.Film != null)
                    {
                        photo.FilmId = photo.Film.Id;
                        _context.Update(photo);
                        await _context.SaveChangesAsync();
                        return RedirectToAction("Page", new { Num = photo.Page });
                    }
                    else
                        return NotFound();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhotoExists(photo.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                
            }

            List<Film> Films = _context.Films
                .Include(p => p.Camera).Include(p => p.Recipe)
                .Include(p => p.Recipe.Developer).Include(p => p.Recipe.FilmType)
                .Include(p => p.Camera.Format).ToList();
            ViewData["FilmId"] = new SelectList(Films, "Id", null, photo.FilmId);
            return View(photo);
        }

        // GET: Photos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Photos == null)
            {
                return NotFound();
            }

            var photo = await _context.Photos
                .Include(p => p.Film)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (photo == null)
            {
                return NotFound();
            }

            return View(photo);
        }

        // POST: Photos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Photos == null)
            {
                return Problem("Entity set 'PhotoContext.Photos'  is null.");
            }
            var photo = await _context.Photos.FindAsync(id);
            if (photo != null)
            {
                _context.Photos.Remove(photo);
            }

            await _context.SaveChangesAsync();
            _photoFiles.DeleteCached(id); // the original file on disk is kept
            return RedirectToAction(nameof(Index));
        }

        private bool PhotoExists(int id)
        {
          return (_context.Photos?.Any(e => e.Id == id)).GetValueOrDefault();
        }




        public async Task<IActionResult> AddPhotoToFilm(int? Id)
        {
            if (Id == null || _context.Films == null)
                return NotFound();

            List<Film> Films = _context.Films
                .Include(p => p.Camera).Include(p => p.Recipe)
                .Include(p => p.Recipe.Developer).Include(p => p.Recipe.FilmType)
                .Include(p => p.Camera.Format).ToList();

            Photo p = new Photo();
            
            if (p == null)
                return NotFound();

            var F = await _context.Films.FindAsync(Id);
            if (F == null)
                return NotFound();

            p.Film = F;
            p.FilmId = p.Film.Id;

            
            ViewData["FilmId"] = new SelectList(Films, "Id", null, p.FilmId);
            return View(p);
        }


        //GET
        public async Task<IActionResult> AddToFilmByFile(int? Id)
        {
            if (Id == null || _context.Films == null)
                return NotFound();

            List<Film> Films = _context.Films
                .Include(p => p.Camera).Include(p => p.Recipe)
                .Include(p => p.Recipe.Developer).Include(p => p.Recipe.FilmType)
                .Include(p => p.Camera.Format).ToList();

            Photo p = new Photo();

            if (p == null)
                return NotFound();

            var F = await _context.Films.FindAsync(Id);
            if (F == null)
                return NotFound();

            p.Film = F;
            p.FilmId = p.Film.Id;


            // Suggest the next free line: on this film's last page if it already has photos,
            // otherwise on the last page of the archive.
            int maxPage = 0, maxLine = 0;
            var filmPages = _context.Photos.Where(ph => ph.FilmId == F.Id).Select(ph => (int?)ph.Page);
            int? page = await filmPages.MaxAsync() ?? await _context.Photos.Select(ph => (int?)ph.Page).MaxAsync();
            if (page != null)
            {
                maxPage = page.Value;
                maxLine = (await _context.Photos.Where(ph => ph.Page == maxPage).MaxAsync(ph => (int?)ph.Line) ?? -1) + 1;
            }
            ViewBag.maxPage = maxPage;
            ViewBag.maxLine = maxLine;
            ViewBag.Accept = string.Join(",", _photoFiles.SupportedExtensions);

            ViewData["FilmId"] = new SelectList(Films, "Id", null, p.FilmId);
            return View(p);
        }


        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(1024L * 1024L * 1024L)] //1Gb
        [RequestFormLimits(MultipartBodyLengthLimit = 1024L * 1024L * 1024L)] //1Gb
        public async Task<IActionResult> AddToFilmByFiles(int FilmId, int page, int line, List<IFormFile> uploadedFiles, CancellationToken ct)
        {
            var film = await _context.Films.FindAsync(FilmId);
            if (film == null)
                return NotFound();

            if (uploadedFiles == null || uploadedFiles.Count == 0)
            {
                TempData["UploadResult"] = "No files were selected.";
                return RedirectToAction("AddToFilmByFile", new { Id = FilmId });
            }

            var existing = (await _context.Photos.Where(p => p.FilmId == FilmId).Select(p => p.Pic).ToListAsync(ct))
                .Where(n => n != null).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var place = (await _context.Photos.Where(p => p.Page == page && p.Line == line)
                .MaxAsync(p => (int?)p.Place_In_Line, ct) ?? -1) + 1;

            var added = new List<Photo>();
            var skipped = new List<string>();
            foreach (var file in uploadedFiles.OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase))
            {
                var name = Path.GetFileName(file.FileName);
                if (existing.Contains(name))
                {
                    skipped.Add(name + " (already in this film)");
                    continue;
                }

                var outcome = await _photoFiles.SaveOriginalAsync(film, file, ct);
                switch (outcome)
                {
                    case UploadOutcome.UnsupportedType:
                        skipped.Add(name + " (unsupported type)");
                        continue;
                    case UploadOutcome.NotAnImage:
                        skipped.Add(name + " (not a readable image)");
                        continue;
                    case UploadOutcome.InvalidFolder:
                        TempData["UploadResult"] = $"Film folder name \"{film.FolderName}\" is not valid. Fix it on the film's Edit page.";
                        return RedirectToAction("AddToFilmByFile", new { Id = FilmId });
                }

                // Saved, or AlreadyOnDisk (copied in by hand earlier): either way the file is there, link it
                var photo = new Photo
                {
                    Name = name,
                    Pic = name,
                    FilmId = FilmId,
                    Page = page,
                    Line = line,
                    Place_In_Line = place++
                };
                _context.Add(photo);
                added.Add(photo);
                existing.Add(name);
            }

            await _context.SaveChangesAsync(ct);
            foreach (var photo in added)
                _thumbnails.Enqueue(photo.Id);

            TempData["UploadResult"] = $"Added {added.Count} photo(s)."
                + (skipped.Count > 0 ? $" Skipped {skipped.Count}: {string.Join(", ", skipped)}." : "");
            return RedirectToAction("Details", "Films", new { Id = FilmId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(int Id)
        {
            Photo photo;
            photo = _context.Photos.Find(Id);
            try
            {
                if (photo != null)
                {
                    photo.Is_Liked = !photo.Is_Liked;
                    _context.Update(photo);
                    await _context.SaveChangesAsync();
                }
                else
                    return NotFound();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PhotoExists(Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction("Details", new { id = Id });
        }


        public async Task<IActionResult> Page(int Num)
        {
            List<Photo> Photos = _context.Photos
                .Include(p => p.Film)
                .Include(p => p.Film.Camera).Include(p => p.Film.Recipe)
                .Include(p => p.Film.Recipe.Developer).Include(p => p.Film.Recipe.FilmType)
                .Include(p => p.Film.Camera.Format)
                
                .Where(p=>p.Page==Num).ToList();
            Dictionary<int, List<Photo>> PageContentDic = new Dictionary<int, List<Photo>>();

            foreach(Photo p in Photos)
            {
                try
                {
                    PageContentDic[p.Line].Add(p);
                }
                catch (KeyNotFoundException)
                {
                    PageContentDic.Add(p.Line, new List<Photo>());
                    PageContentDic[p.Line].Add(p);
                }
            }

            List<List<Photo>> PageContent = new List<List<Photo>>();

            foreach (int key in PageContentDic.Keys.OrderBy(k => k))
            {
                PageContent.Add(PageContentDic[key]
                    .OrderBy(p => p.Place_In_Line).ThenBy(p => p.Pic).ThenBy(p => p.Id)
                    .ToList());
            }






            ViewBag.Current = Num;

            List<int> pages = _context.Photos.Select(p => p.Page).Distinct().ToList();
            pages.Sort();
            int CurIndex = pages.FindIndex(i => i == Num);

            List<int> Prev = new List<int>();
            for (int i = CurIndex - 1; (i >= 0) && (i >= CurIndex - 3); i--)
            {
                Prev.Add(pages[i]);
            }

            List<int> Next = new List<int>();
            for (int i = CurIndex + 1; (i < pages.Count) && (i <= CurIndex + 3); i++)
            {
                Next.Add(pages[i]);
            }

            Next.Sort();
            Prev.Sort();

            ViewBag.Next = Next;
            ViewBag.Prev = Prev;




            return View(PageContent);
        }
    }
}
