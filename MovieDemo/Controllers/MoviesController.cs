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

        // --- USER SIDE: MY LIBRARY ---
        public async Task<IActionResult> MyLibrary()
        {
            // FIX: Identify user via Identity instead of Session
            var userEmail = User.Identity.Name;
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null) return RedirectToAction("Login", "Account");

            var seenMovies = await _context.MovieListItems
                .Include(li => li.Movie).ThenInclude(m => m.Genres)
                .Where(li => li.MovieList.UserId == user.Id && li.MovieList.Title == "Seen Content")
                .Select(li => li.Movie)
                .ToListAsync();

            return View(seenMovies);
        }

        // --- PREMIUM AJAX: TOGGLE SEEN STATUS ---
        [HttpPost]
        public async Task<IActionResult> ToggleSeen(int movieId)
        {
            // FIX: Identify user via Identity instead of Session
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

        public async Task<IActionResult> Details(int id)
        {
            var movie = await _context.Movies.Include(m => m.Genres).FirstOrDefaultAsync(m => m.Id == id);
            return movie == null ? NotFound() : View(movie);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            return View(await _context.Movies.Include(m => m.Genres).ToListAsync());
        }

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

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
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

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null) { _context.Movies.Remove(movie); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Manage));
        }
    }
}