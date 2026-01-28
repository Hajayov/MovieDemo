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

        // --- MAIN GALLERY & DISCOVERY (User View) ---
        public async Task<IActionResult> IndexM(string search, int[] selectedGenres)
        {
            await PopulateCommonViewData(search, selectedGenres);

            // This ensures filters stay on the IndexM page
            ViewBag.TargetAction = "IndexM";
            ViewBag.PageTitle = "Explore the Network";

            var moviesQuery = GetFilteredMovies(search, selectedGenres);
            return View(await moviesQuery.ToListAsync());
        }

        // --- ADMIN MANAGEMENT (Admin View) ---
        public async Task<IActionResult> Manage(string search, int[] selectedGenres)
        {
            await PopulateCommonViewData(search, selectedGenres);

            // KEY FIX: Tells the view to submit filters to the Manage action
            ViewBag.TargetAction = "Manage";
            ViewBag.PageTitle = "Manage Library Assets";

            var moviesQuery = GetFilteredMovies(search, selectedGenres);
            return View(await moviesQuery.OrderBy(m => m.Title).ToListAsync());
        }

        // --- SHARED LOGIC HELPERS ---

        private IQueryable<Movie> GetFilteredMovies(string search, int[] selectedGenres)
        {
            var query = _context.Movies.Include(m => m.Genres).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(m => m.Title.Contains(search) || m.Director.Contains(search));

            if (selectedGenres?.Length > 0)
                query = query.Where(m => m.Genres.Any(g => selectedGenres.Contains(g.Id)));

            return query;
        }

        private async Task PopulateCommonViewData(string search, int[] selectedGenres)
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user != null)
            {
                ViewBag.SeenMovieIds = await _context.MovieListItems
                    .Where(li => li.MovieList.UserId == user.Id && li.MovieList.Title == "Seen Content")
                    .Select(li => li.MovieId).ToListAsync();

                ViewBag.WatchlistMovieIds = await _context.MovieListItems
                    .Where(li => li.MovieList.UserId == user.Id && li.MovieList.Title == "Watchlist")
                    .Select(li => li.MovieId).ToListAsync();
            }
            else
            {
                ViewBag.SeenMovieIds = new List<int>();
                ViewBag.WatchlistMovieIds = new List<int>();
            }

            // Fill Genres for the Pills
            ViewBag.Genres = await _context.Genres.Include(g => g.Movies).OrderByDescending(g => g.Movies.Count).ToListAsync();
            ViewBag.SelectedGenres = selectedGenres ?? new int[0];
            ViewBag.Search = search;
        }

        // --- CRUD OPERATIONS ---

        public async Task<IActionResult> Details(int id, string returnUrl, int? fromListId)
        {
            var movie = await _context.Movies.Include(m => m.Genres).FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null) return NotFound();

            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user != null)
            {
                ViewBag.IsSeen = await _context.MovieListItems.AnyAsync(li => li.MovieId == id && li.MovieList.UserId == user.Id && li.MovieList.Title == "Seen Content");
                ViewBag.InWatchlist = await _context.MovieListItems.AnyAsync(li => li.MovieId == id && li.MovieList.UserId == user.Id && li.MovieList.Title == "Watchlist");
            }

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.FromListId = fromListId;
            return View(movie);
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
        // --- USER LIBRARY ---
        public async Task<IActionResult> MyLibrary()
        {
            var userEmail = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null) return RedirectToAction("Welcome");

            var lists = await _context.MovieLists.Include(l => l.Items).ThenInclude(i => i.Movie)
                                .Where(l => l.UserId == user.Id).ToListAsync();
            return View(lists);
        }

        // GET: Movies/ListDetails/5
        public async Task<IActionResult> ListDetails(int id)
        {
            var list = await _context.MovieLists
                .Include(l => l.Items)
                    .ThenInclude(li => li.Movie)
                        .ThenInclude(m => m.Genres)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (list == null) return NotFound();

            // This tells the "Details" page to show a "Back to List" button instead of "Back to Home"
            ViewBag.FromListId = id;
            return View(list);
        }

        // POST: Movies/CreateCustomList
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
                    UserId = user.Id,
                    IsSystemList = false
                };
                _context.MovieLists.Add(newList);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(MyLibrary));
        }

        // GET: Movies/AddMoviesToList
        public async Task<IActionResult> AddMoviesToList(int listId, string search, int[] selectedGenres)
        {
            var list = await _context.MovieLists.FindAsync(listId);
            if (list == null) return NotFound();

            await PopulateCommonViewData(search, selectedGenres);

            ViewBag.ListId = listId;
            ViewBag.ListName = list.Title;
            ViewBag.ExistingMovieIds = await _context.MovieListItems
                .Where(li => li.MovieListId == listId)
                .Select(li => li.MovieId).ToListAsync();

            var moviesQuery = GetFilteredMovies(search, selectedGenres);
            return View(await moviesQuery.ToListAsync());
        }

        // POST: Movies/AddMovieToListAjax
        [HttpPost]
        public async Task<IActionResult> AddMovieToListAjax(int listId, int movieId)
        {
            var exists = await _context.MovieListItems
                .AnyAsync(li => li.MovieListId == listId && li.MovieId == movieId);

            if (!exists)
            {
                _context.MovieListItems.Add(new MovieListItem
                {
                    MovieListId = listId,
                    MovieId = movieId,
                    DateAdded = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        // POST: Movies/RemoveFromList
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

        // POST: Movies/DeleteList
        [HttpPost]
        public async Task<IActionResult> DeleteList(int id)
        {
            var list = await _context.MovieLists.FindAsync(id);
            if (list != null && !list.IsSystemList)
            {
                _context.MovieLists.Remove(list);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(MyLibrary));
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