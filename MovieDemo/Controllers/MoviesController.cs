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

        // Constructor: Inject the DB context to access your SQL tables
        public MoviesController(AppDbContext context)
        {
            _context = context;
        }

        // MAIN PAGE: Grid of movies with Search and Filter
        public async Task<IActionResult> IndexM(string search, int? genreId)
        {
            // 1. Fetch all genres from the database for the dropdown filter
            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();

            // 2. Start the query and INCLUDE the Genres
            var moviesQuery = _context.Movies.Include(m => m.Genres).AsQueryable();

            // 3. Apply Text Search (Title or Director)
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                moviesQuery = moviesQuery.Where(m =>
                    m.Title.ToLower().Contains(search) ||
                    m.Director.ToLower().Contains(search));
            }

            // 4. Apply Category Filter (Many-to-Many filtering)
            if (genreId.HasValue)
            {
                moviesQuery = moviesQuery.Where(m => m.Genres.Any(g => g.Id == genreId));
            }

            // 5. Execute the final combined query
            var movies = await moviesQuery.ToListAsync();

            // Keep track of current search/filter for the View
            ViewBag.Search = search;
            ViewBag.SelectedGenre = genreId;

            return View(movies);
        }

        // NEW DETAILS PAGE: Shows all info for one specific movie
        public async Task<IActionResult> Details(int id)
        {
            // Find the movie by ID and include its Genres for the list
            var movie = await _context.Movies
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);

            // If the movie doesn't exist (e.g. someone types a wrong ID in the URL), show 404
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }
    }
}