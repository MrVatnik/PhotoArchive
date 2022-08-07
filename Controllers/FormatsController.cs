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
    public class FormatsController : Controller
    {
        private readonly PhotoContext _context;

        public FormatsController(PhotoContext context)
        {
            _context = context;
        }

        // GET: Formats
        public async Task<IActionResult> Index()
        {
              return _context.Formats != null ? 
                          View(await _context.Formats.ToListAsync()) :
                          Problem("Entity set 'PhotoContext.Formats'  is null.");
        }

        // GET: Formats/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Formats == null)
            {
                return NotFound();
            }

            var format = await _context.Formats
                .FirstOrDefaultAsync(m => m.Id == id);
            if (format == null)
            {
                return NotFound();
            }

            return View(format);
        }

        // GET: Formats/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Formats/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Format format)
        {
            if (ModelState.IsValid)
            {
                _context.Add(format);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(format);
        }

        // GET: Formats/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Formats == null)
            {
                return NotFound();
            }

            var format = await _context.Formats.FindAsync(id);
            if (format == null)
            {
                return NotFound();
            }
            return View(format);
        }

        // POST: Formats/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Format format)
        {
            if (id != format.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(format);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FormatExists(format.Id))
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
            return View(format);
        }

        // GET: Formats/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Formats == null)
            {
                return NotFound();
            }

            var format = await _context.Formats
                .FirstOrDefaultAsync(m => m.Id == id);
            if (format == null)
            {
                return NotFound();
            }

            return View(format);
        }

        // POST: Formats/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Formats == null)
            {
                return Problem("Entity set 'PhotoContext.Formats'  is null.");
            }
            var format = await _context.Formats.FindAsync(id);
            if (format != null)
            {
                _context.Formats.Remove(format);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FormatExists(int id)
        {
          return (_context.Formats?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
