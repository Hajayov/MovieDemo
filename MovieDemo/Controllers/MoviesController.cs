using Microsoft.AspNetCore.Mvc;
using MovieDemo.Models;
using System.Collections.Generic;
using System.Linq;

namespace MovieDemo.Controllers
{
    public class MoviesController : Controller
    {
        private List<Movie> GetAllMovies()
        {
            return new List<Movie>
            {
                new Movie { Title="The Godfather", Summary="The aging patriarch of an organized crime dynasty transfers control to his reluctant son.", Director="Francis Ford Coppola", PosterUrl="https://upload.wikimedia.org/wikipedia/en/1/1c/Godfather_ver1.jpg" },
                new Movie { Title="The Dark Knight", Summary="Batman battles the Joker, who pushes Gotham into chaos.", Director="Christopher Nolan", PosterUrl="https://upload.wikimedia.org/wikipedia/en/8/8a/Dark_Knight.jpg" },
                new Movie { Title="The Dictator", Summary="A dictator risks his life to ensure democracy never arrives in his country.", Director="Larry Charles", PosterUrl="https://upload.wikimedia.org/wikipedia/en/9/99/The_Dictator_Poster.jpg" },
                new Movie { Title="Inception", Summary="A thief who steals corporate secrets through dream-sharing technology.", Director="Christopher Nolan", PosterUrl="https://upload.wikimedia.org/wikipedia/en/7/7f/Inception_ver3.jpg" },
                new Movie { Title="Pulp Fiction", Summary="The lives of two mob hitmen, a boxer, and others intertwine in LA.", Director="Quentin Tarantino", PosterUrl="https://upload.wikimedia.org/wikipedia/en/8/82/Pulp_Fiction_cover.jpg" },
                new Movie { Title="Fight Club", Summary="An insomniac office worker and soapmaker form an underground fight club.", Director="David Fincher", PosterUrl="https://upload.wikimedia.org/wikipedia/en/f/fc/Fight_Club_poster.jpg" },
                new Movie { Title="Forrest Gump", Summary="The life journey of Forrest Gump, a man with a low IQ but extraordinary experiences.", Director="Robert Zemeckis", PosterUrl="https://upload.wikimedia.org/wikipedia/en/6/67/Forrest_Gump_poster.jpg" },
                new Movie { Title="The Matrix", Summary="A computer hacker learns about the true nature of reality.", Director="The Wachowskis", PosterUrl="https://upload.wikimedia.org/wikipedia/en/c/c1/The_Matrix_Poster.jpg" },
                new Movie { Title="Gladiator", Summary="A former Roman general seeks vengeance for the murder of his family.", Director="Ridley Scott", PosterUrl="https://upload.wikimedia.org/wikipedia/en/8/8d/Gladiator_ver1.jpg" },
                new Movie { Title="The Shawshank Redemption", Summary="Two imprisoned men bond over several years.", Director="Frank Darabont", PosterUrl="https://upload.wikimedia.org/wikipedia/en/8/81/ShawshankRedemptionMoviePoster.jpg" },
                new Movie { Title="Titanic", Summary="A love story unfolds aboard the doomed RMS Titanic.", Director="James Cameron", PosterUrl="https://upload.wikimedia.org/wikipedia/en/2/2e/Titanic_poster.jpg" },
                new Movie { Title="Jurassic Park", Summary="A theme park with cloned dinosaurs experiences catastrophic failure.", Director="Steven Spielberg", PosterUrl="https://upload.wikimedia.org/wikipedia/en/e/e7/Jurassic_Park_poster.jpg" },
                new Movie { Title="The Avengers", Summary="Earth's mightiest heroes assemble to stop Loki.", Director="Joss Whedon", PosterUrl="https://upload.wikimedia.org/wikipedia/en/f/f9/TheAvengers2012Poster.jpg" },
                new Movie { Title="Iron Man", Summary="A billionaire builds a high-tech suit to fight evil.", Director="Jon Favreau", PosterUrl="https://upload.wikimedia.org/wikipedia/en/0/00/Iron_Man_poster.jpg" },
                new Movie { Title="Deadpool", Summary="A mercenary gains superpowers and a twisted sense of humor.", Director="Tim Miller", PosterUrl="https://upload.wikimedia.org/wikipedia/en/4/46/Deadpool_poster.jpg" },
                new Movie { Title="Guardians of the Galaxy", Summary="A group of intergalactic criminals must save the galaxy.", Director="James Gunn", PosterUrl="https://upload.wikimedia.org/wikipedia/en/8/8f/GOTG-poster.jpg" },
                new Movie { Title="Avatar", Summary="Humans invade Pandora and clash with the Na'vi.", Director="James Cameron", PosterUrl="https://upload.wikimedia.org/wikipedia/en/b/b0/Avatar-Teaser-Poster.jpg" },
                new Movie { Title="The Wolf of Wall Street", Summary="The rise and fall of stockbroker Jordan Belfort.", Director="Martin Scorsese", PosterUrl="https://upload.wikimedia.org/wikipedia/en/1/1f/Wolf_of_Wall_Street.png" },
                new Movie { Title="Toy Story", Summary="Toys come to life when humans aren't around.", Director="John Lasseter", PosterUrl="https://upload.wikimedia.org/wikipedia/en/1/13/Toy_Story.jpg" },
                new Movie { Title="Shrek", Summary="An ogre rescues a princess with the help of a talkative donkey.", Director="Andrew Adamson", PosterUrl="https://upload.wikimedia.org/wikipedia/en/3/39/Shrek.jpg" }
            };
        }

        public IActionResult IndexM(string search)
        {
            var movies = GetAllMovies();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                movies = movies.Where(m =>
                    m.Director.ToLower().Contains(search) ||
                    m.Title.ToLower().Contains(search)
                ).ToList();
            }

            ViewBag.Search = search; // Keep value in the search box
            return View(movies);
        }
    }
}
