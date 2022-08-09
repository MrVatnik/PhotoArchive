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
    public class FilmsController : Controller
    {
        private readonly PhotoContext _context;

        public FilmsController(PhotoContext context)
        {
            _context = context;
        }

        // GET: Films
        public async Task<IActionResult> Index()
        {
            var photoContext = _context.Films
                .Include(f => f.Camera).Include(f => f.Recipe)
                .Include(f => f.Recipe.FilmType)
                .Include(f => f.Recipe.Developer)
                .Include(f => f.Camera.Format);
            return View(await photoContext.ToListAsync());
        }

        // GET: Films/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Films == null)
            {
                return NotFound();
            }

            var film = await _context.Films
                .Include(f => f.Camera).Include(f => f.Recipe)
                .Include(f => f.Recipe.FilmType)
                .Include(f => f.Recipe.Developer)
                .Include(f => f.Camera.Format)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (film == null)
            {
                return NotFound();
            }


            ViewData["Photos"] = _context.Photos
                .Where(p=>p.FilmId==film.Id)
                .OrderBy(p => p.Pic).ToList();

            return View(film);
        }

        // GET: Films/Create
        public IActionResult Create()
        {

            if (_context.Recipes == null || _context.Cameras == null)
            {
                return NotFound();
            }

            List<Recipe> Recipes = _context.Recipes
                .Include(f => f.FilmType)
                .Include(f => f.Developer)
                .ToList();


            ViewData["CameraId"] = new SelectList(_context.Cameras, "Id", null);
            ViewData["RecipeId"] = new SelectList(Recipes, "Id", null);
            return View();
        }

        // POST: Films/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,RecipeId,CameraId,Date,FolderName")] Film film)
        {
            if (ModelState.IsValid)
            {
                film.Recipe = _context.Recipes.Find(film.RecipeId);
                
                film.Camera = _context.Cameras.Find(film.CameraId);

                _context.Add(film);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            List<Recipe> Recipes = _context.Recipes
                .Include(f => f.FilmTypeId).Include(f => f.FilmType)
                .Include(f => f.DeveloperId).Include(f => f.Developer)
                .ToList();

            ViewData["CameraId"] = new SelectList(_context.Cameras, "Id", null, film.CameraId);
            ViewData["RecipeId"] = new SelectList(Recipes, "Id", null, film.RecipeId);
            return View(film);
        }

        // GET: Films/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Films == null)
            {
                return NotFound();
            }

            var film = await _context.Films.FindAsync(id);
            if (film == null)
            {
                return NotFound();
            }

            List<Recipe> Recipes = _context.Recipes
                .Include(f => f.FilmType)
                .Include(f => f.Developer)
                .ToList();

            ViewData["CameraId"] = new SelectList(_context.Cameras, "Id", null, film.CameraId);
            ViewData["RecipeId"] = new SelectList(Recipes, "Id", null, film.RecipeId);
            return View(film);
        }

        // POST: Films/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,RecipeId,CameraId,Date,FolderName")] Film film)
        {
            if (id != film.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    film.Recipe = _context.Recipes.Find(film.RecipeId);

                    film.Recipe.Developer = _context.Developers.Find(film.Recipe.DeveloperId);
                    film.Recipe.FilmType = _context.FilmTypes.Find(film.Recipe.FilmTypeId);

                    film.Camera = _context.Cameras.Find(film.CameraId);

                    _context.Update(film);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FilmExists(film.Id))
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

            List<Recipe> Recipes = _context.Recipes
                .Include(f => f.FilmType)
                .Include(f => f.Developer)
                .ToList();

            ViewData["CameraId"] = new SelectList(_context.Cameras, "Id", null, film.CameraId);
            ViewData["RecipeId"] = new SelectList(Recipes, "Id", null, film.RecipeId);
            return View(film);
        }

        // GET: Films/Delete/5
        public async Task<IActionResult> Delete(int? Id)
        {
            if (Id == null || _context.Films == null)
            {
                return NotFound();
            }

            var film = await _context.Films
                .Include(f => f.Camera).Include(f => f.Recipe)
                .Include(f=>f.Recipe.Developer).Include(f => f.Recipe.FilmType)
                .FirstOrDefaultAsync(m => m.Id == Id);
            if (film == null)
            {
                return NotFound();
            }

            var photos = _context.Photos.Where(p => p.FilmId == film.Id).ToList();

            ViewData["Message"] = "It contains " + photos.Count + " photos.";

            return View(film);
        }

        // POST: Films/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            if (_context.Films == null)
            {
                return Problem("Entity set 'PhotoContext.Films'  is null.");
            }
            var film = await _context.Films.FindAsync(Id);
            if (film != null)
            {
                _context.Films.Remove(film);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FilmExists(int Id)
        {
          return (_context.Films?.Any(e => e.Id == Id)).GetValueOrDefault();
        }




        public async Task<IActionResult> AddFilmFromRec(int? Id)
        {
            if (Id == null || _context.Recipes == null||_context.Cameras == null)
                return NotFound();

            List<Recipe> Recipes = _context.Recipes
                .Include(f => f.FilmType)
                .Include(f => f.Developer)
                .ToList();

            Film F = new Film();

            if(F == null)
                return NotFound();

            var R = await _context.Recipes.FindAsync(Id);
            if(R==null)
                return NotFound();

            F.Recipe = R;
            F.RecipeId = F.Recipe.Id;


            ViewData["CameraId"] = new SelectList(_context.Cameras, "Id", null);
            ViewData["RecipeId"] = new SelectList(Recipes, "Id", null, F.RecipeId);
            return View(F);
        }



        public async Task<IActionResult> AddFilmFromCam(int? Id)
        {
            if (Id == null || _context.Recipes == null || _context.Cameras == null)
                return NotFound();

            List<Recipe> Recipes = _context.Recipes
                .Include(f => f.FilmType)
                .Include(f => f.Developer)
                .ToList();

            Film F = new Film();

            if (F == null)
                return NotFound();

            var C = await _context.Cameras.FindAsync(Id);
            if (C == null)
                return NotFound();

            F.Camera = C;
            F.CameraId = F.Camera.Id;


            ViewData["CameraId"] = new SelectList(_context.Cameras, "Id",null, F.CameraId);
            ViewData["RecipeId"] = new SelectList(Recipes, "Id", null);
            return View(F);
        }

    }
}
