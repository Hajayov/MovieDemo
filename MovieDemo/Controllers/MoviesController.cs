using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieDemo.Data;
using MovieDemo.Models;
using Microsoft.AspNetCore.Authorization;

namespace MovieDemo.Controllers
{
    [Authorize]
    public class MoviesController : Controller
    {
        private readonly AppDbContext _context;
        public MoviesController(AppDbContext context) { _context = context; }

        // --- PUBLIC ACCESS ---
        [AllowAnonymous]
        public IActionResult Welcome()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("IndexM");
            return View();
        }

        // --- MAIN GALLERY & DISCOVERY ---
        public async Task<IActionResult> IndexM(string search, int[] selectedGenres)
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            // Fetch IDs of movies already in user's system lists to persist the "pressed" state
            var seenMovieIds = await _context.MovieListItems
                .Where(li => li.MovieList.UserId == user.Id && li.MovieList.Title == "Seen Content")
                .Select(li => li.MovieId)
                .ToListAsync();

            var watchlistMovieIds = await _context.MovieListItems
                .Where(li => li.MovieList.UserId == user.Id && li.MovieList.Title == "Watchlist")
                .Select(li => li.MovieId)
                .ToListAsync();

            ViewBag.SeenMovieIds = seenMovieIds;
            ViewBag.WatchlistMovieIds = watchlistMovieIds;

            // Existing filter logic
            var genres = await _context.Genres.Include(g => g.Movies).OrderByDescending(g => g.Movies.Count).ToListAsync();
            ViewBag.Genres = genres;

            var moviesQuery = _context.Movies.Include(m => m.Genres).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                moviesQuery = moviesQuery.Where(m => m.Title.Contains(search) || m.Director.Contains(search));

            if (selectedGenres?.Length > 0)
                moviesQuery = moviesQuery.Where(m => m.Genres.Any(g => selectedGenres.Contains(g.Id)));

            ViewBag.SelectedGenres = selectedGenres ?? new int[0];
            return View(await moviesQuery.ToListAsync());
        }

        public async Task<IActionResult> Details(int id, string returnUrl, int? fromListId)
        {
            var movie = await _context.Movies.Include(m => m.Genres).FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null) return NotFound();
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.FromListId = fromListId;
            return View(movie);
        }

        // --- ADMIN MANAGEMENT ---
        public async Task<IActionResult> Manage()
        {
            var movies = await _context.Movies.Include(m => m.Genres).OrderBy(m => m.Title).ToListAsync();
            return View(movies);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Genres = await _context.Genres.ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Movie movie, int[] genreIds)
        {
            if (genreIds != null)
                movie.Genres = await _context.Genres.Where(g => genreIds.Contains(g.Id)).ToListAsync();

            _context.Add(movie);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Manage));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var movie = await _context.Movies.Include(m => m.Genres).FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null) return NotFound();
            ViewBag.Genres = await _context.Genres.ToListAsync();
            return View(movie);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Movie movie, int[] genreIds)
        {
            var existing = await _context.Movies.Include(m => m.Genres).FirstOrDefaultAsync(m => m.Id == movie.Id);
            if (existing == null) return NotFound();

            existing.Title = movie.Title;
            existing.Director = movie.Director;
            existing.PosterUrl = movie.PosterUrl;

            existing.Genres.Clear();
            if (genreIds != null)
                existing.Genres = await _context.Genres.Where(g => genreIds.Contains(g.Id)).ToListAsync();

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Manage));
        }

        public async Task<IActionResult> Delete(int id) => View(await _context.Movies.FindAsync(id));

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var m = await _context.Movies.FindAsync(id);
            if (m != null) _context.Movies.Remove(m);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Manage));
        }

        // --- USER LIBRARY & LISTS ---
        public async Task<IActionResult> MyLibrary()
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            var lists = await _context.MovieLists.Include(l => l.Items).ThenInclude(i => i.Movie)
                                .Where(l => l.UserId == user.Id).ToListAsync();
            return View(lists);
        }

        public async Task<IActionResult> ListDetails(int id)
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            var list = await _context.MovieLists.Include(l => l.Items).ThenInclude(i => i.Movie)
                                .FirstOrDefaultAsync(l => l.Id == id && l.UserId == user.Id);
            return list == null ? NotFound() : View(list);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomList(string title)
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (!string.IsNullOrWhiteSpace(title))
            {
                _context.MovieLists.Add(new MovieList { Title = title, IsSystemList = false, UserId = user.Id });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("MyLibrary");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteList(int listId)
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            var list = await _context.MovieLists.FirstOrDefaultAsync(l => l.Id == listId && l.UserId == user.Id && !l.IsSystemList);
            if (list != null) { _context.MovieLists.Remove(list); await _context.SaveChangesAsync(); }
            return RedirectToAction("MyLibrary");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromList(int listItemId)
        {
            var item = await _context.MovieListItems.Include(li => li.MovieList).FirstOrDefaultAsync(li => li.Id == listItemId);
            if (item != null)
            {
                int lId = item.MovieListId;
                _context.MovieListItems.Remove(item);
                await _context.SaveChangesAsync();
                return RedirectToAction("ListDetails", new { id = lId });
            }
            return RedirectToAction("MyLibrary");
        }

        // --- AJAX TOGGLES (SEEN/WATCHLIST) ---
        [HttpPost] public async Task<IActionResult> ToggleSeen(int movieId) => await ToggleSystemList("Seen Content", movieId);
        [HttpPost] public async Task<IActionResult> ToggleWatchlist(int movieId) => await ToggleSystemList("Watchlist", movieId);

        private async Task<IActionResult> ToggleSystemList(string title, int mId)
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            var list = await _context.MovieLists.FirstOrDefaultAsync(l => l.UserId == user.Id && l.IsSystemList && l.Title == title);

            if (list == null)
            {
                list = new MovieList { Title = title, IsSystemList = true, UserId = user.Id };
                _context.MovieLists.Add(list);
                await _context.SaveChangesAsync();
            }

            var item = await _context.MovieListItems.FirstOrDefaultAsync(li => li.MovieListId == list.Id && li.MovieId == mId);
            if (item != null) _context.MovieListItems.Remove(item);
            else _context.MovieListItems.Add(new MovieListItem { MovieListId = list.Id, MovieId = mId, DateAdded = DateTime.Now });

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}