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
    public class CamerasController : Controller
    {
        private readonly PhotoContext _context;

        public CamerasController(PhotoContext context)
        {
            _context = context;
        }

        // GET: Cameras
        public async Task<IActionResult> Index()
        {
            var photoContext = _context.Cameras.Include(c => c.Format);
            return View(await photoContext.ToListAsync());
        }

        // GET: Cameras/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Cameras == null)
            {
                return NotFound();
            }

            var camera = await _context.Cameras
                .Include(c => c.Format)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (camera == null)
            {
                return NotFound();
            }

            return View(camera);
        }

        // GET: Cameras/Create
        public IActionResult Create()
        {
            ViewData["FormatId"] = new SelectList(_context.Formats, "Id", null);
            return View();
        }

        // POST: Cameras/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,FormatId")] Camera camera)
        {
            if (ModelState.IsValid)
            {
                camera.Format = _context.Formats.Find(camera.FormatId);
                _context.Add(camera);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["FormatId"] = new SelectList(_context.Formats, "Id", null, camera.FormatId);
            return View(camera);
        }

        // GET: Cameras/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Cameras == null)
            {
                return NotFound();
            }

            var camera = await _context.Cameras.FindAsync(id);
            if (camera == null)
            {
                return NotFound();
            }
            ViewData["FormatId"] = new SelectList(_context.Formats, "Id", null, camera.FormatId);
            return View(camera);
        }

        // POST: Cameras/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,FormatId")] Camera camera)
        {
            if (id != camera.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    camera.Format = _context.Formats.Find(camera.FormatId);
                    _context.Update(camera);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CameraExists(camera.Id))
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
            ViewData["FormatId"] = new SelectList(_context.Formats, "Id", null, camera.FormatId);
            return View(camera);
        }

        // GET: Cameras/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Cameras == null)
            {
                return NotFound();
            }

            var camera = await _context.Cameras
                .Include(c => c.Format)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (camera == null)
            {
                return NotFound();
            }


            var films = _context.Films.Where(f => f.CameraId == camera.Id).ToList();
            List<Photo> photos = new List<Photo>();
            foreach (Film film in films)
            {
                if (film != null)
                {
                    photos.AddRange(_context.Photos.Where(p => p.FilmId == film.Id).ToList());
                }
            }


            ViewData["Message"] = "It contains " + films.Count + " films, " +
                photos.Count + " photos in total.";


            return View(camera);
        }

        // POST: Cameras/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Cameras == null)
            {
                return Problem("Entity set 'PhotoContext.Cameras'  is null.");
            }
            var camera = await _context.Cameras.FindAsync(id);
            if (camera != null)
            {
                _context.Cameras.Remove(camera);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CameraExists(int id)
        {
          return (_context.Cameras?.Any(e => e.Id == id)).GetValueOrDefault();
        }



        public async Task<IActionResult> AddCamFromFormat(int? Id)
        {
            if (Id == null || _context.Formats == null)
                return NotFound();


            Camera C = new Camera();

            if (C == null)
                return NotFound();

            var F = await _context.Formats.FindAsync(Id);

            if (F == null)
                return NotFound();

            C.Format = F;
            C.FormatId = C.Format.Id;



            ViewData["FormatId"] = new SelectList(_context.Formats, "Id", null, C.FormatId);
            return View(C);
        }
        
    }
}
