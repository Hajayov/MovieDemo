using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieDemo.Data;
using MovieDemo.Models;
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

        // --- EXISTING: USER SIDE ---

        public async Task<IActionResult> IndexM(string search, int? genreId)
        {
            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();
            var moviesQuery = _context.Movies.Include(m => m.Genres).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                moviesQuery = moviesQuery.Where(m =>
                    m.Title.ToLower().Contains(search) ||
                    m.Director.ToLower().Contains(search));
            }

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

        // --- NEW: ADMIN SIDE (CREATE MOVIE) ---

        // 1. GET: Show the "Add Movie" Form
        public async Task<IActionResult> Create()
        {
            // We fetch the genres so you can show checkboxes in the View
            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();
            return View();
        }

        // 2. POST: Save the new movie to the Database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Movie movie, int[] selectedGenres)
        {
            if (ModelState.IsValid)
            {
                // Link the selected Genres to the Movie object
                if (selectedGenres != null)
                {
                    foreach (var genreId in selectedGenres)
                    {
                        var genre = await _context.Genres.FindAsync(genreId);
                        if (genre != null)
                        {
                            movie.Genres.Add(genre);
                        }
                    }
                }

                _context.Add(movie);
                await _context.SaveChangesAsync();

                // After saving, go back to the main list
                return RedirectToAction(nameof(IndexM));
            }

            // If something failed, reload genres and show the form again with errors
            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();
            return View(movie);
        }
    }
}