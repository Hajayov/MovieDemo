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
        // Updated to accept an array for multiple genre selection
        public async Task<IActionResult> IndexM(string search, int[] selectedGenres)
        {
            // 1. Get genres sorted by popularity (most movies first)
            var genres = await _context.Genres
                .Include(g => g.Movies)
                .OrderByDescending(g => g.Movies.Count)
                .ToListAsync();

            ViewBag.Genres = genres;

            var moviesQuery = _context.Movies.Include(m => m.Genres).AsQueryable();

            // 2. Filter by Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                moviesQuery = moviesQuery.Where(m =>
                    m.Title.ToLower().Contains(search) ||
                    m.Director.ToLower().Contains(search));
            }

            // 3. Filter by Multiple Categories
            if (selectedGenres != null && selectedGenres.Length > 0)
            {
                // Finds movies that have at least one of the selected genres
                moviesQuery = moviesQuery.Where(m => m.Genres.Any(g => selectedGenres.Contains(g.Id)));
            }

            var movies = await moviesQuery.ToListAsync();

            ViewBag.Search = search;
            // Ensure we don't pass a null array to the view
            ViewBag.SelectedGenres = selectedGenres ?? new int[0];

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

            ModelState.Remove("Genres");

            if (ModelState.IsValid)
            {
                try
                {
                    var movieToUpdate = await _context.Movies
                        .Include(m => m.Genres)
                        .FirstOrDefaultAsync(m => m.Id == id);

                    if (movieToUpdate == null) return NotFound();

                    movieToUpdate.Title = movie.Title;
                    movieToUpdate.Director = movie.Director;
                    movieToUpdate.Summary = movie.Summary;
                    movieToUpdate.PosterUrl = movie.PosterUrl;
                    movieToUpdate.ReleaseDate = movie.ReleaseDate;
                    movieToUpdate.Runtime = movie.Runtime;

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