namespace MovieDemo.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty; // Required
        public string? Summary { get; set; }
        public string? Director { get; set; }
        public string? PosterUrl { get; set; }
        public string? ReleaseDate { get; set; }
        public int? Runtime { get; set; }

        // Initialized as an empty list to prevent null errors
        public List<Genre> Genres { get; set; } = new List<Genre>();
    }
}