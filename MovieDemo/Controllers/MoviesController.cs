using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieDemo.Data;
using MovieDemo.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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

            ViewBag.TargetAction = "IndexM";
            ViewBag.PageTitle = "Explore the Network";

            var moviesQuery = GetFilteredMovies(search, selectedGenres);
            return View(await moviesQuery.ToListAsync());
        }

        // --- ADMIN MANAGEMENT (Admin View) ---
        public async Task<IActionResult> Manage(string search, int[] selectedGenres)
        {
            await PopulateCommonViewData(search, selectedGenres);

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
                query = query.Where(m => m.Title.Contains(search) || (m.Director != null && m.Director.Contains(search)));

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

            ViewBag.Genres = await _context.Genres.Include(g => g.Movies).OrderByDescending(g => g.Movies.Count).ToListAsync();
            ViewBag.SelectedGenres = selectedGenres ?? new int[0];
            ViewBag.Search = search;
        }

        // --- CRUD OPERATIONS ---

        public async Task<IActionResult> Details(int id, string returnUrl, int? fromListId)
        {
            var movie = await _context.Movies
                .Include(m => m.Genres)
                .Include(m => m.Reviews)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null) return NotFound();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value);

                var userReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.MovieId == id && r.UserId == userId);

                ViewBag.UserRating = userReview?.Rating ?? 0;
                ViewBag.UserComment = userReview?.Comment ?? "";

                ViewBag.IsSeen = await _context.MovieListItems.AnyAsync(li => li.MovieId == id && li.MovieList.UserId == userId && li.MovieList.Title == "Seen Content");
                ViewBag.InWatchlist = await _context.MovieListItems.AnyAsync(li => li.MovieId == id && li.MovieList.UserId == userId && li.MovieList.Title == "Watchlist");
            }

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.FromListId = fromListId;
            return View(movie);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReview(int movieId, int rating, string comment)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Json(new { success = false, message = "Session Expired" });

            int userId = int.Parse(userIdClaim.Value);

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.MovieId == movieId && r.UserId == userId);

            // TOGGLE / RESET LOGIC: If rating is 0, user cleared it
            if (rating == 0)
            {
                if (existingReview != null) _context.Reviews.Remove(existingReview);
            }
            else if (existingReview != null)
            {
                existingReview.Rating = rating;
                existingReview.Comment = comment;
                existingReview.DatePosted = DateTime.Now;
            }
            else
            {
                _context.Reviews.Add(new Review
                {
                    MovieId = movieId,
                    UserId = userId,
                    Rating = rating,
                    Comment = comment,
                    DatePosted = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
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
            existing.Summary = movie.Summary;
            existing.ReleaseDate = movie.ReleaseDate;
            existing.Runtime = movie.Runtime;

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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Welcome");
            int userId = int.Parse(userIdClaim.Value);

            var lists = await _context.MovieLists.Include(l => l.Items).ThenInclude(i => i.Movie)
                                .Where(l => l.UserId == userId).ToListAsync();
            return View(lists);
        }

        public async Task<IActionResult> ListDetails(int id)
        {
            var list = await _context.MovieLists
                .Include(l => l.Items)
                    .ThenInclude(li => li.Movie)
                        .ThenInclude(m => m.Genres)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (list == null) return NotFound();

            ViewBag.FromListId = id;
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomList(string title)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && !string.IsNullOrWhiteSpace(title))
            {
                _context.MovieLists.Add(new MovieList
                {
                    Title = title,
                    UserId = int.Parse(userIdClaim.Value),
                    IsSystemList = false
                });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(MyLibrary));
        }

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

        // --- FIXED DELETE LIST ACTION ---
        [HttpPost]
        public async Task<IActionResult> DeleteList(int listId) // Matches the "name" in your hidden form input
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim.Value);

            var list = await _context.MovieLists.FirstOrDefaultAsync(l => l.Id == listId && l.UserId == userId);

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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Json(new { success = false });
            int userId = int.Parse(userIdClaim.Value);

            var list = await _context.MovieLists.FirstOrDefaultAsync(l => l.UserId == userId && l.IsSystemList && l.Title == title);

            if (list == null)
            {
                list = new MovieList { Title = title, IsSystemList = true, UserId = userId };
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