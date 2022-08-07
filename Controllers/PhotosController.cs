using System;
using System.Collections.Generic;
using System.Linq;
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
                .FirstOrDefaultAsync(m => m.id == id);
            if (photo == null)
            {
                return NotFound();
            }

            return View(photo);
        }

        // GET: Photos/Create
        public IActionResult Create()
        {
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
        public async Task<IActionResult> Create([Bind("id,Name,Pic,FilmId,Is_Liked,Page,Line,Place_In_Line")] Photo photo)
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
        public async Task<IActionResult> Edit(int id, [Bind("id,Name,Pic,FilmId,Is_Liked,Page,Line,Place_In_Line")] Photo photo)
        {
            if (id != photo.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    photo.Film = _context.Films.Find(photo.FilmId);
                    _context.Update(photo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhotoExists(photo.id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
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
                .FirstOrDefaultAsync(m => m.id == id);
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
          return (_context.Photos?.Any(e => e.id == id)).GetValueOrDefault();
        }




        public async Task<IActionResult> AddPhotoToFilm(int? FilmId)
        {
            //ViewBag.FilmId = FilmId.ToString();

            //ViewData["FilmId"] = new SelectList(_context.Films, "Id", null, FilmId);
            //return View();

            if (FilmId == null || _context.Films == null)
            {
                return NotFound();
            }

            var Film = await _context.Films.FindAsync(FilmId);
            Photo photo = new Photo();
            photo.Film = Film;
            if (photo == null)
            {
                return NotFound();
            }
            ViewData["FilmId"] = new SelectList(_context.Films, "Id", null, photo.FilmId);
            return View(photo);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPhotoToFilm([Bind("id,Name,Pic,FilmId,Is_Liked,Page,Line,Place_In_Line")] Photo photo)
        {
            if (ModelState.IsValid)
            {
                photo.Film = _context.Films.Find(photo.FilmId);
                _context.Add(photo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["FilmId"] = new SelectList(_context.Films, "Id", null, photo.FilmId);
            return View(photo);
        }


    }
}
