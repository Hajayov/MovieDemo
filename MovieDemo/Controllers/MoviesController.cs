using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieDemo.Data;
using MovieDemo.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace MovieDemo.Controllers
{
    [Authorize]
    public class MoviesController : Controller
    {
        private readonly AppDbContext _context;

        public MoviesController(AppDbContext context)
        {
            _context = context;
        }

        // --- USER SIDE: GALLERY ---
        [AllowAnonymous]
        public async Task<IActionResult> IndexM(string search, int[] selectedGenres)
        {
            var genres = await _context.Genres
                .Include(g => g.Movies)
                .OrderByDescending(g => g.Movies.Count)
                .ToListAsync();

            ViewBag.Genres = genres;

            var moviesQuery = _context.Movies
                .Include(m => m.Genres)
                .Include(m => m.ListItems)
                    .ThenInclude(li => li.MovieList)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                moviesQuery = moviesQuery.Where(m =>
                    m.Title.ToLower().Contains(search) ||
                    m.Director.ToLower().Contains(search));
            }

            if (selectedGenres != null && selectedGenres.Length > 0)
            {
                moviesQuery = moviesQuery.Where(m => m.Genres.Any(g => selectedGenres.Contains(g.Id)));
            }

            var movies = await moviesQuery.ToListAsync();
            ViewBag.Search = search;
            ViewBag.SelectedGenres = selectedGenres ?? new int[0];

            return View(movies);
        }

        // --- UPDATED: MY LIBRARY (Now Force-Includes Movies) ---
        public async Task<IActionResult> MyLibrary()
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null) return RedirectToAction("Login", "Account");

            // CRITICAL: We need Include(l => l.Items).ThenInclude(i => i.Movie) 
            // so the poster stack on the library cards actually finds the movies.
            var lists = await _context.MovieLists
                .Include(l => l.Items)
                    .ThenInclude(i => i.Movie)
                .Where(l => l.UserId == user.Id)
                .ToListAsync();

            bool changed = false;
            if (!lists.Any(l => l.Title == "Seen Content" && l.IsSystemList))
            {
                _context.MovieLists.Add(new MovieList { Title = "Seen Content", IsSystemList = true, UserId = user.Id });
                changed = true;
            }
            if (!lists.Any(l => l.Title == "Watchlist" && l.IsSystemList))
            {
                _context.MovieLists.Add(new MovieList { Title = "Watchlist", IsSystemList = true, UserId = user.Id });
                changed = true;
            }

            if (changed)
            {
                await _context.SaveChangesAsync();
                lists = await _context.MovieLists
                    .Include(l => l.Items)
                        .ThenInclude(i => i.Movie)
                    .Where(l => l.UserId == user.Id)
                    .ToListAsync();
            }

            return View(lists);
        }

        // --- UPDATED: LIST DETAILS (Now Force-Includes Movies) ---
        public async Task<IActionResult> ListDetails(int id)
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null) return RedirectToAction("Login", "Account");

            // CRITICAL: Fetches the bridge table (Items) and the actual Movie records.
            var list = await _context.MovieLists
                .Include(l => l.Items)
                    .ThenInclude(i => i.Movie)
                .FirstOrDefaultAsync(l => l.Id == id && l.UserId == user.Id);

            if (list == null) return NotFound();
            return View(list);
        }

        // --- ADD MOVIES TO LIST ---
        public async Task<IActionResult> AddMoviesToList(int listId, string search, int[] selectedGenres)
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            var list = await _context.MovieLists.FirstOrDefaultAsync(l => l.Id == listId && l.UserId == user.Id);
            if (list == null) return NotFound();

            ViewBag.ListId = listId;
            ViewBag.ListName = list.Title;
            ViewBag.Genres = await _context.Genres.ToListAsync();

            var moviesQuery = _context.Movies.Include(m => m.Genres).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                moviesQuery = moviesQuery.Where(m => m.Title.ToLower().Contains(search));
            }

            if (selectedGenres != null && selectedGenres.Length > 0)
            {
                moviesQuery = moviesQuery.Where(m => m.Genres.Any(g => selectedGenres.Contains(g.Id)));
            }

            ViewBag.ExistingMovieIds = await _context.MovieListItems
                .Where(li => li.MovieListId == listId)
                .Select(li => li.MovieId)
                .ToListAsync();

            return View(await moviesQuery.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> AddMovieToListAjax(int listId, int movieId)
        {
            var exists = await _context.MovieListItems
                .AnyAsync(li => li.MovieListId == listId && li.MovieId == movieId);

            if (!exists)
            {
                _context.MovieListItems.Add(new MovieListItem { MovieListId = listId, MovieId = movieId, DateAdded = System.DateTime.Now });
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSeen(int movieId) => await ToggleSystemList("Seen Content", movieId);

        [HttpPost]
        public async Task<IActionResult> ToggleWatchlist(int movieId) => await ToggleSystemList("Watchlist", movieId);

        private async Task<IActionResult> ToggleSystemList(string listTitle, int movieId)
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null) return Unauthorized();

            var list = await _context.MovieLists
                .FirstOrDefaultAsync(l => l.UserId == user.Id && l.IsSystemList && l.Title == listTitle);

            if (list == null)
            {
                list = new MovieList { Title = listTitle, IsSystemList = true, UserId = user.Id };
                _context.MovieLists.Add(list);
                await _context.SaveChangesAsync();
            }

            var existing = await _context.MovieListItems
                .FirstOrDefaultAsync(li => li.MovieListId == list.Id && li.MovieId == movieId);

            if (existing != null) _context.MovieListItems.Remove(existing);
            else _context.MovieListItems.Add(new MovieListItem { MovieListId = list.Id, MovieId = movieId, DateAdded = System.DateTime.Now });

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        public async Task<IActionResult> Details(int id, int? fromListId)
        {
            var movie = await _context.Movies.Include(m => m.Genres).FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null) return NotFound();
            ViewBag.FromListId = fromListId;
            return View(movie);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomList(string title)
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user != null && !string.IsNullOrWhiteSpace(title))
            {
                _context.MovieLists.Add(new MovieList { Title = title, IsSystemList = false, UserId = user.Id });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(MyLibrary));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteList(int listId)
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            var list = await _context.MovieLists.FirstOrDefaultAsync(l => l.Id == listId && l.UserId == user.Id);
            if (list != null && !list.IsSystemList)
            {
                _context.MovieLists.Remove(list);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(MyLibrary));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromList(int listItemId)
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            var item = await _context.MovieListItems
                .Include(li => li.MovieList)
                .FirstOrDefaultAsync(li => li.Id == listItemId && li.MovieList.UserId == user.Id);

            if (item != null)
            {
                int listId = item.MovieListId;
                _context.MovieListItems.Remove(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ListDetails), new { id = listId });
            }
            return RedirectToAction(nameof(MyLibrary));
        }

        // --- ADMIN ACTIONS REMOVED FOR BREVITY (LEAVE YOURS AS IS) ---
    }
}