using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieDemo.Data;
using MovieDemo.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace MovieDemo.Controllers
{
    public class MoviesController : Controller
    {
        private readonly AppDbContext _context;

        public MoviesController(AppDbContext context)
        {
            _context = context;
        }

        // --- USER SIDE: GALLERY ---
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

        // --- NEW USER SIDE: MY LIBRARY (DASHBOARD OF LISTS) ---
        public async Task<IActionResult> MyLibrary()
        {
            var userEmail = User.Identity.Name;
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null) return RedirectToAction("Login", "Account");

            // Fetch ALL lists for this user, including movie data for the previews
            var lists = await _context.MovieLists
                .Include(l => l.Items)
                    .ThenInclude(i => i.Movie)
                .Where(l => l.UserId == user.Id)
                .ToListAsync();

            // Ensure "Seen Content" and "Watchlist" exist (auto-create if missing)
            if (!lists.Any(l => l.Title == "Seen Content" && l.IsSystemList))
            {
                var seenList = new MovieList { Title = "Seen Content", IsSystemList = true, UserId = user.Id };
                _context.MovieLists.Add(seenList);
                await _context.SaveChangesAsync();
                lists.Add(seenList);
            }
            if (!lists.Any(l => l.Title == "Watchlist" && l.IsSystemList))
            {
                var watchList = new MovieList { Title = "Watchlist", IsSystemList = true, UserId = user.Id };
                _context.MovieLists.Add(watchList);
                await _context.SaveChangesAsync();
                lists.Add(watchList);
            }

            return View(lists);
        }

        // --- VIEW SPECIFIC LIST DETAILS ---
        public async Task<IActionResult> ListDetails(int id)
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null) return Unauthorized();

            var list = await _context.MovieLists
                .Include(l => l.Items)
                    .ThenInclude(i => i.Movie)
                .FirstOrDefaultAsync(l => l.Id == id && l.UserId == user.Id);

            if (list == null) return NotFound();

            return View(list);
        }

        // --- CREATE CUSTOM LIST ---
        [HttpPost]
        public async Task<IActionResult> CreateCustomList(string title)
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user != null && !string.IsNullOrWhiteSpace(title))
            {
                var newList = new MovieList
                {
                    Title = title,
                    IsSystemList = false,
                    UserId = user.Id
                };
                _context.MovieLists.Add(newList);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(MyLibrary));
        }

        // --- DELETE ENTIRE LIST ---
        [HttpPost]
        public async Task<IActionResult> DeleteList(int listId)
        {
            var list = await _context.MovieLists.FindAsync(listId);
            if (list != null && !list.IsSystemList) // Protect system lists
            {
                _context.MovieLists.Remove(list);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(MyLibrary));
        }

        // --- REMOVE SINGLE MOVIE FROM LIST ---
        [HttpPost]
        public async Task<IActionResult> RemoveFromList(int listItemId)
        {
            var item = await _context.MovieListItems.FindAsync(listItemId);
            if (item != null)
            {
                int listId = item.MovieListId;
                _context.MovieListItems.Remove(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ListDetails), new { id = listId });
            }
            return RedirectToAction(nameof(MyLibrary));
        }

        // --- AJAX: TOGGLE SEEN STATUS ---
        [HttpPost]
        public async Task<IActionResult> ToggleSeen(int movieId)
        {
            var userEmail = User.Identity.Name;
            if (string.IsNullOrEmpty(userEmail)) return Unauthorized();

            var user = await _context.Users
                .Include(u => u.MovieLists)
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null) return Unauthorized();

            var seenList = user.MovieLists.FirstOrDefault(l => l.IsSystemList && l.Title == "Seen Content");

            if (seenList == null)
            {
                seenList = new MovieList { Title = "Seen Content", IsSystemList = true, UserId = user.Id };
                _context.MovieLists.Add(seenList);
                await _context.SaveChangesAsync();
            }

            var existingItem = await _context.MovieListItems
                .FirstOrDefaultAsync(li => li.MovieListId == seenList.Id && li.MovieId == movieId);

            if (existingItem != null) { _context.MovieListItems.Remove(existingItem); }
            else { _context.MovieListItems.Add(new MovieListItem { MovieListId = seenList.Id, MovieId = movieId }); }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // --- AJAX: TOGGLE WATCHLIST STATUS ---
        [HttpPost]
        public async Task<IActionResult> ToggleWatchlist(int movieId)
        {
            var userEmail = User.Identity.Name;
            if (string.IsNullOrEmpty(userEmail)) return Unauthorized();

            var user = await _context.Users
                .Include(u => u.MovieLists)
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null) return Unauthorized();

            var watchList = user.MovieLists.FirstOrDefault(l => l.IsSystemList && l.Title == "Watchlist");

            if (watchList == null)
            {
                watchList = new MovieList { Title = "Watchlist", IsSystemList = true, UserId = user.Id };
                _context.MovieLists.Add(watchList);
                await _context.SaveChangesAsync();
            }

            var existingItem = await _context.MovieListItems
                .FirstOrDefaultAsync(li => li.MovieListId == watchList.Id && li.MovieId == movieId);

            if (existingItem != null) { _context.MovieListItems.Remove(existingItem); }
            else { _context.MovieListItems.Add(new MovieListItem { MovieListId = watchList.Id, MovieId = movieId }); }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // --- REMAINING ADMIN ACTIONS ---
        public async Task<IActionResult> Details(int id)
        {
            var movie = await _context.Movies.Include(m => m.Genres).FirstOrDefaultAsync(m => m.Id == id);
            return movie == null ? NotFound() : View(movie);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage() => View(await _context.Movies.Include(m => m.Genres).ToListAsync());

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();
            return View();
        }

        [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Movie movie, int[] selectedGenres)
        {
            ModelState.Remove("Genres");
            if (ModelState.IsValid)
            {
                if (selectedGenres != null)
                {
                    foreach (var id in selectedGenres)
                    {
                        var genre = await _context.Genres.FindAsync(id);
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

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var movie = await _context.Movies.Include(m => m.Genres).FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null) return NotFound();
            ViewBag.Genres = await _context.Genres.OrderBy(g => g.Name).ToListAsync();
            return View(movie);
        }

        [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Movie movie, int[] selectedGenres)
        {
            if (id != movie.Id) return NotFound();
            ModelState.Remove("Genres");
            if (ModelState.IsValid)
            {
                var movieToUpdate = await _context.Movies.Include(m => m.Genres).FirstOrDefaultAsync(m => m.Id == id);
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
                    foreach (var gId in selectedGenres)
                    {
                        var genre = await _context.Genres.FindAsync(gId);
                        if (genre != null) movieToUpdate.Genres.Add(genre);
                    }
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Manage));
            }
            return View(movie);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var movie = await _context.Movies.Include(m => m.Genres).FirstOrDefaultAsync(m => m.Id == id);
            return movie == null ? NotFound() : View(movie);
        }

        [HttpPost, ActionName("Delete"), Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null) { _context.Movies.Remove(movie); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Manage));
        }
    }
}