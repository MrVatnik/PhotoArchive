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
    public class RecipesController : Controller
    {
        private readonly PhotoContext _context;

        public RecipesController(PhotoContext context)
        {
            _context = context;
        }

        // GET: Recipes
        public async Task<IActionResult> Index()
        {
            var photoContext = _context.Recipes.Include(r => r.Developer).Include(r => r.FilmType);
            return View(await photoContext.ToListAsync());
        }

        // GET: Recipes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Recipes == null)
            {
                return NotFound();
            }

            var recipe = await _context.Recipes
                .Include(r => r.Developer)
                .Include(r => r.FilmType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (recipe == null)
            {
                return NotFound();
            }

            ViewData["Films"] = _context.Films
                .Include(f => f.Recipe).Include(f => f.Recipe.FilmType)
                .Include(f => f.Camera)
                .Where(f => f.RecipeId.Equals(recipe.Id))
                .OrderBy(f => f.Date).ToList<Film>();

            return View(recipe);
        }

        // GET: Recipes/Create
        public IActionResult Create()
        {
            ViewData["DeveloperId"] = new SelectList(_context.Developers, "Id", null);
            ViewData["FilmTypeId"] = new SelectList(_context.FilmTypes, "Id", null);
            return View();
        }

        // POST: Recipes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,EI,FilmTypeId,DeveloperId,Min,Sec,Color")] Recipe recipe)
        {
            if (ModelState.IsValid)
            {
                recipe.FilmType = _context.FilmTypes.Find(recipe.FilmTypeId);
                recipe.Developer = _context.Developers.Find(recipe.DeveloperId);
                _context.Add(recipe);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DeveloperId"] = new SelectList(_context.Developers, "Id", null, recipe.DeveloperId);
            ViewData["FilmTypeId"] = new SelectList(_context.FilmTypes, "Id", null, recipe.FilmTypeId);
            return View(recipe);
        }

        // GET: Recipes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Recipes == null)
            {
                return NotFound();
            }

            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
            {
                return NotFound();
            }
            ViewData["DeveloperId"] = new SelectList(_context.Developers, "Id", null, recipe.DeveloperId);
            ViewData["FilmTypeId"] = new SelectList(_context.FilmTypes, "Id", null, recipe.FilmTypeId);
            return View(recipe);
        }

        // POST: Recipes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EI,FilmTypeId,DeveloperId,Min,Sec,Color")] Recipe recipe)
        {
            if (id != recipe.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    recipe.FilmType = _context.FilmTypes.Find(recipe.FilmTypeId);
                    recipe.Developer = _context.Developers.Find(recipe.DeveloperId);
                    _context.Update(recipe);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RecipeExists(recipe.Id))
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
            ViewData["DeveloperId"] = new SelectList(_context.Developers, "Id", null, recipe.DeveloperId);
            ViewData["FilmTypeId"] = new SelectList(_context.FilmTypes, "Id", null, recipe.FilmTypeId);
            return View(recipe);
        }

        // GET: Recipes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Recipes == null)
            {
                return NotFound();
            }

            var recipe = await _context.Recipes
                .Include(r => r.Developer)
                .Include(r => r.FilmType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (recipe == null)
            {
                return NotFound();
            }
            
            var films = _context.Films.Where(f=>f.RecipeId == recipe.Id).ToList();
            List<Photo> photos = new List<Photo>();
            foreach(Film film in films)
            {
                if (film!= null)
                {
                    photos.AddRange(_context.Photos.Where(p => p.FilmId == film.Id).ToList());
                }
            }


            ViewData["Message"] = "It contains " + films.Count + " films, " +
                photos.Count + " photos in total.";


            return View(recipe);
        }

        // POST: Recipes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Recipes == null)
            {
                return Problem("Entity set 'PhotoContext.Recipes'  is null.");
            }
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe != null)
            {
                _context.Recipes.Remove(recipe);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RecipeExists(int id)
        {
            return (_context.Recipes?.Any(e => e.Id == id)).GetValueOrDefault();
        }




        public async Task<IActionResult> AddRecFromFT(int? Id)
        {
            if (Id == null || _context.FilmTypes == null || _context.Developers == null)
                return NotFound();


            Recipe R = new Recipe();

            if (R == null)
                return NotFound();

            var filmType = await _context.FilmTypes.FindAsync(Id);

            if (filmType == null)
                return NotFound();

            R.FilmType = filmType;
            R.FilmTypeId = R.FilmType.Id;


            ViewData["DeveloperId"] = new SelectList(_context.Developers, "Id", null);
            ViewData["FilmTypeId"] = new SelectList(_context.FilmTypes, "Id", null, R.FilmType.Id);

            return View(R);
        }




        public async Task<IActionResult> AddRecFromDev(int? Id)
        {
            if (Id == null || _context.FilmTypes == null || _context.Developers == null)
                return NotFound();


            Recipe R = new Recipe();

            if (R == null)
                return NotFound();

            var Dev = await _context.Developers.FindAsync(Id);

            if (Dev == null)
                return NotFound();

            R.Developer = Dev;
            R.DeveloperId = R.Developer.Id;


            ViewData["DeveloperId"] = new SelectList(_context.Developers, "Id", null, R.DeveloperId);
            ViewData["FilmTypeId"] = new SelectList(_context.FilmTypes, "Id", null);

            return View(R);
        }
    }
}
