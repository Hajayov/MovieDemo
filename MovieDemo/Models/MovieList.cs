namespace MovieDemo.Models
{
    public class MovieList
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool IsSystemList { get; set; }

        // REQUIRED: Link this list to a specific User
        public int UserId { get; set; }
        public virtual User User { get; set; }

        public virtual ICollection<MovieListItem> Items { get; set; } = new List<MovieListItem>();
    }
}