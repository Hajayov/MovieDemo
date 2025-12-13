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

        // Constructor: Inject the DB context to access your new SQL tables
        public MoviesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> IndexM(string search, int? genreId)
        {
            // 1. Fetch all genres from the database for the dropdown filter
            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();

            // 2. Start the query and INCLUDE the Genres (the Junction Table logic)
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
    }
}