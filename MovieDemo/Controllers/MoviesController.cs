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

        // --- USER SIDE: GALLERY & DETAILS ---

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

        // --- ADMIN SIDE: MANAGE TABLE ---

        // This action feeds the Manager Table view
        public async Task<IActionResult> Manage()
        {
            var movies = await _context.Movies.Include(m => m.Genres).ToListAsync();
            return View(movies);
        }

        // --- ADMIN SIDE: CREATE ---

        public async Task<IActionResult> Create()
        {
            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Movie movie, int[] selectedGenres)
        {
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
                return RedirectToAction(nameof(Manage)); // Redirect to Manager after creating
            }

            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();
            return View(movie);
        }

        // --- ADMIN SIDE: EDIT ---

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Movie movie, int[] selectedGenres)
        {
            if (id != movie.Id) return NotFound();

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
                return RedirectToAction(nameof(Manage)); // Redirect to Manager after editing
            }

            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();
            return View(movie);
        }

        // --- ADMIN SIDE: DELETE ---

        // GET: Show confirmation page
        public async Task<IActionResult> Delete(int id)
        {
            var movie = await _context.Movies
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null) return NotFound();

            return View(movie);
        }

        // POST: Actually remove the movie
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Manage)); // Redirect to Manager after deleting
        }
    }
}