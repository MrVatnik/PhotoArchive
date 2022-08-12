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
    public class FilmTypesController : Controller
    {
        private readonly PhotoContext _context;

        public FilmTypesController(PhotoContext context)
        {
            _context = context;
        }

        // GET: FilmTypes
        public async Task<IActionResult> Index()
        {
            List<FilmType> FilmTypes = await _context
                .FilmTypes.OrderBy(ft => ft.Name).ToListAsync();
              return FilmTypes != null ? 
                          View(FilmTypes) :
                          Problem("Entity set 'PhotoContext.FilmTypes'  is null.");
        }

        // GET: FilmTypes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.FilmTypes == null)
            {
                return NotFound();
            }

            var filmType = await _context.FilmTypes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (filmType == null)
            {
                return NotFound();
            }
            ViewData["Recipes"] = _context.Recipes.Include(p => p.FilmType).Include(p => p.Developer).Where(p => p.FilmTypeId.Equals(id))
                .OrderBy(rec => rec.EI).ToList<Recipe>();

            return View(filmType);
        }

        // GET: FilmTypes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: FilmTypes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,ISO,Color_Is_Possible")] FilmType filmType)
        {
            if (ModelState.IsValid)
            {
                _context.Add(filmType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(filmType);
        }

        // GET: FilmTypes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.FilmTypes == null)
            {
                return NotFound();
            }

            var filmType = await _context.FilmTypes.FindAsync(id);
            if (filmType == null)
            {
                return NotFound();
            }
            return View(filmType);
        }

        // POST: FilmTypes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ISO,Color_Is_Possible")] FilmType filmType)
        {
            if (id != filmType.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(filmType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FilmTypeExists(filmType.Id))
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
            return View(filmType);
        }

        // GET: FilmTypes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.FilmTypes == null)
            {
                return NotFound();
            }

            var filmType = await _context.FilmTypes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (filmType == null)
            {
                return NotFound();
            }



            List<Photo> photos = new List<Photo>();
            List<Film> films = new List<Film>();

            var recipes = _context.Recipes.Where(r => r.FilmTypeId == filmType.Id).ToList();

            foreach (Recipe recipe in recipes)
            {
                if (recipe != null)
                    films.AddRange(_context.Films.Where(f => f.RecipeId == recipe.Id).ToList());
            }

            foreach (Film film in films)
            {
                if (film != null)
                {
                    photos.AddRange(_context.Photos.Where(p => p.FilmId == film.Id).ToList());
                }
            }


            ViewData["Message"] = "It contains " + recipes.Count + " recipes, " + films.Count + " films, " +
                photos.Count + " photos in total.";



            return View(filmType);
        }

        // POST: FilmTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.FilmTypes == null)
            {
                return Problem("Entity set 'PhotoContext.FilmTypes'  is null.");
            }
            var filmType = await _context.FilmTypes.FindAsync(id);
            if (filmType != null)
            {
                _context.FilmTypes.Remove(filmType);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FilmTypeExists(int id)
        {
          return (_context.FilmTypes?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
