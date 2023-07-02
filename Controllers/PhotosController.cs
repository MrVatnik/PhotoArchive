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

namespace PhotoArchive.Controllers
{
    public class PhotosController : Controller
    {
        private readonly PhotoContext _context;

        public PhotosController(PhotoContext context)
        {
            _context = context;
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


            int maxPage, maxLine;
            List<int> pages, lines;

            if (_context.Photos.Where(p => p.Id == Id).Count() != 0)
            {
                pages = _context.Photos.Where(p => p.Id == Id).OrderByDescending(p => p.Page).Select(p => p.Page).ToList();
                maxPage = pages[0];
                lines = _context.Photos.Where(p => p.Id == Id).Where(p => p.Page == maxPage).OrderByDescending(p => p.Line).Select(p => p.Line).ToList();
                maxLine = lines[0] + 1;
            }
            else
            {
                if (_context.Photos.Count() != 0)
                {
                    pages = _context.Photos.OrderByDescending(p => p.Page).Select(p => p.Page).ToList();

                    maxPage = pages[0];

                    lines = _context.Photos.Where(p => p.Page == maxPage).OrderByDescending(p => p.Line).Select(p => p.Line).ToList();

                    maxLine = lines[0] + 1;
                }
                else
                {
                    maxPage = 0;
                    maxLine = 0;
                }
            }
            ViewBag.maxPage = maxPage;
            ViewBag.maxLine = maxLine;

            ViewData["FilmId"] = new SelectList(Films, "Id", null, p.FilmId);
            return View(p);
        }


        //POST
        [RequestSizeLimit(1024L * 1024L * 1024L)] //1Gb
        [RequestFormLimits(MultipartBodyLengthLimit = 1024L * 1024L * 1024L)] //1Gb
        public async Task<IActionResult> AddToFilmByFiles(int FilmId,int page,int line, List<IFormFile> uploadedFiles)
        {
            
            if (uploadedFiles!=null)
            {
                if (uploadedFiles.Count() != 0)
                {
                    foreach (IFormFile uploadedFile in uploadedFiles)
                    {
                        Photo photo = new Photo();
                        photo.Name = uploadedFile.FileName;
                        photo.Pic = uploadedFile.FileName;
                        photo.FilmId = FilmId;
                        photo.Film = _context.Films.Find(photo.FilmId);
                        photo.Page = page;
                        photo.Line = line;

                        _context.Add(photo);
                        await _context.SaveChangesAsync();


                        photo.Miniature();      //to generate a miniature
                    }
                    return RedirectToAction("Details", "Films", new { Id = FilmId });
                }
                else
                {
                    List<Film> Films = _context.Films                
                        .Include(p => p.Camera).Include(p => p.Recipe)                
                        .Include(p => p.Recipe.Developer).Include(p => p.Recipe.FilmType)                
                        .Include(p => p.Camera.Format)
                        .ToList();
                    ViewData["FilmId"] = new SelectList(Films, "Id", null, FilmId);
                    return RedirectToAction("AddToFilmByFile", new { Id = FilmId });
                }
            }
            else
            {
                List<Film> Films = _context.Films
                .Include(p => p.Camera).Include(p => p.Recipe)
                .Include(p => p.Recipe.Developer).Include(p => p.Recipe.FilmType)
                .Include(p => p.Camera.Format)
                .ToList();
                ViewData["FilmId"] = new SelectList(Films, "Id", null, FilmId);
                return RedirectToAction("AddToFilmByFile", new { Id = FilmId });
            }      
        }


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
            return RedirectToAction("Details", photo);
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

            foreach (int key in PageContentDic.Keys)
            {
                PageContent.Add(PageContentDic[key].OrderBy(p => p.Place_In_Line).ToList());
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
