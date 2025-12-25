namespace MovieDemo.Models
{
    public class Movie
    {
        public int Id { get; set; } // Database primary key
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Director { get; set; }
        public string PosterUrl { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int Runtime { get; set; }

        // This is the "Connecting" part. EF Core creates the 
        // junction table automatically in the background.
        public List<Genre> Genres { get; set; } = new();
    }
}