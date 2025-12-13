using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using MovieDemo.Models;
using MovieDemo.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace MovieDemo.Data
{
    public class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            try
            {
                // 1. Wipe the database so we can fix the broken images
                context.Movies.RemoveRange(context.Movies);
                context.Genres.RemoveRange(context.Genres);
                context.SaveChanges();
                Console.WriteLine(">>> DATABASE CLEARED. RE-IMPORTING WITH NEW IMAGE URLS...");
            }
            catch (Exception ex) { Console.WriteLine(">>> CLEAR ERROR: " + ex.Message); }

            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "movies.csv");
            if (!File.Exists(filePath))
            {
                Console.WriteLine(">>> ERROR: movies.csv not found at " + filePath);
                return;
            }

            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    HeaderValidated = null,
                    MissingFieldFound = null,
                    PrepareHeaderForMatch = args => args.Header.ToLower().Trim()
                };

                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, config))
                {
                    csv.Read();
                    csv.ReadHeader();
                    var genreMap = new Dictionary<string, Genre>();

                    while (csv.Read())
                    {
                        var title = csv.GetField("title")?.Trim() ?? "Untitled";

                        // --- THE NEW IMAGE URL LOGIC ---
                        // This uses the w600_and_h900_face path you found!
                        var rawPath = csv.GetField("poster_path")?.Trim() ?? "";
                        var cleanPath = rawPath.TrimStart('/');
                        var finalPosterUrl = $"https://image.tmdb.org/t/p/w600_and_h900_face/{cleanPath}";

                        var movie = new Movie
                        {
                            Title = title,
                            Summary = csv.GetField("overview") ?? "",
                            Director = csv.GetField("director") ?? "Unknown",
                            PosterUrl = finalPosterUrl,
                            ReleaseDate = csv.GetField("release_date") ?? ""
                        };

                        if (int.TryParse(csv.GetField("runtime"), out int r)) { movie.Runtime = r; }

                        // Console log to check the generated links in the Output window
                        Console.WriteLine($">>> Loading Movie: {title} | URL: {finalPosterUrl}");

                        // --- GENRE PROCESSING ---
                        var genreJson = csv.GetField("genres");
                        if (!string.IsNullOrEmpty(genreJson))
                        {
                            try
                            {
                                var validJson = genreJson.Replace("'", "\"");
                                var rawGenres = JsonSerializer.Deserialize<List<GenreJsonData>>(validJson);
                                if (rawGenres != null)
                                {
                                    foreach (var g in rawGenres)
                                    {
                                        if (!genreMap.ContainsKey(g.Name))
                                        {
                                            var newGenre = new Genre { Name = g.Name };
                                            genreMap[g.Name] = newGenre;
                                            context.Genres.Add(newGenre);
                                        }
                                        movie.Genres.Add(genreMap[g.Name]);
                                    }
                                }
                            }
                            catch { /* Skip bad genre data */ }
                        }
                        context.Movies.Add(movie);
                    }
                    context.SaveChanges();
                    Console.WriteLine(">>> SUCCESS! DATABASE SYNCED WITH WORKING IMAGES.");
                }
            }
            catch (Exception ex) { Console.WriteLine(">>> IMPORT ERROR: " + ex.Message); }
        }

        private class GenreJsonData
        {
            [JsonPropertyName("id")] public int Id { get; set; }
            [JsonPropertyName("name")] public string Name { get; set; } = "";
        }
    }
}