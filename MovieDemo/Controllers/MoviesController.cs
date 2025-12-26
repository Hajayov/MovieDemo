using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieDemo.Data;
using MovieDemo.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace MovieDemo.Controllers
{
    public class MoviesController : Controller
    {
        private readonly AppDbContext _context;

        public MoviesController(AppDbContext context)
        {
            _context = context;
        }

        // --- USER SIDE: GALLERY & DETAILS ---
        public async Task<IActionResult> IndexM(string search, int? genreId)
        {
            // Get genres for the filter menu
            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();

            var moviesQuery = _context.Movies.Include(m => m.Genres).AsQueryable();

            // Filter by Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                moviesQuery = moviesQuery.Where(m =>
                    m.Title.ToLower().Contains(search) ||
                    m.Director.ToLower().Contains(search));
            }

            // Filter by Category
            if (genreId.HasValue)
            {
                moviesQuery = moviesQuery.Where(m => m.Genres.Any(g => g.Id == genreId));
            }

            var movies = await moviesQuery.ToListAsync();

            ViewBag.Search = search;
            ViewBag.SelectedGenre = genreId;

            return View(movies);
        }

        public async Task<IActionResult> Details(int id)
        {
            var movie = await _context.Movies
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null) return NotFound();
            return View(movie);
        }

        // --- ADMIN SIDE: MANAGE TABLE ---
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            var movies = await _context.Movies.Include(m => m.Genres).ToListAsync();
            return View(movies);
        }

        // --- ADMIN SIDE: CREATE ---
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Movie movie, int[] selectedGenres)
        {
            // Essential for many-to-many saving
            ModelState.Remove("Genres");

            if (ModelState.IsValid)
            {
                if (selectedGenres != null)
                {
                    foreach (var genreId in selectedGenres)
                    {
                        var genre = await _context.Genres.FindAsync(genreId);
                        if (genre != null) movie.Genres.Add(genre);
                    }
                }

                _context.Add(movie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Manage));
            }

            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();
            return View(movie);
        }

        // --- ADMIN SIDE: EDIT ---
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var movie = await _context.Movies
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null) return NotFound();

            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();
            return View(movie);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Movie movie, int[] selectedGenres)
        {
            if (id != movie.Id) return NotFound();

            // Essential: Ignores validation mismatch on the Genres object list
            ModelState.Remove("Genres");

            if (ModelState.IsValid)
            {
                try
                {
                    var movieToUpdate = await _context.Movies
                        .Include(m => m.Genres)
                        .FirstOrDefaultAsync(m => m.Id == id);

                    if (movieToUpdate == null) return NotFound();

                    // Update Properties
                    movieToUpdate.Title = movie.Title;
                    movieToUpdate.Director = movie.Director;
                    movieToUpdate.Summary = movie.Summary;
                    movieToUpdate.PosterUrl = movie.PosterUrl;
                    movieToUpdate.ReleaseDate = movie.ReleaseDate;
                    movieToUpdate.Runtime = movie.Runtime;

                    // Update Many-to-Many relationship
                    movieToUpdate.Genres.Clear();
                    if (selectedGenres != null)
                    {
                        foreach (var genreId in selectedGenres)
                        {
                            var genre = await _context.Genres.FindAsync(genreId);
                            if (genre != null) movieToUpdate.Genres.Add(genre);
                        }
                    }

                    _context.Update(movieToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Movies.Any(e => e.Id == movie.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Manage));
            }

            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();
            return View(movie);
        }

        // --- ADMIN SIDE: DELETE ---
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var movie = await _context.Movies
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null) return NotFound();
            return View(movie);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Manage));
        }
    }
}