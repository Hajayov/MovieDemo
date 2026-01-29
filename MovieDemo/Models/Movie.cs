namespace MovieDemo.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? Director { get; set; }
        public string? PosterUrl { get; set; }
        public string? ReleaseDate { get; set; }
        public int? Runtime { get; set; }

        public List<Genre> Genres { get; set; } = new List<Genre>();

        // Connection to the List bridge table
        public virtual ICollection<MovieListItem> ListItems { get; set; } = new List<MovieListItem>();
        // Connection to Reviews
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}