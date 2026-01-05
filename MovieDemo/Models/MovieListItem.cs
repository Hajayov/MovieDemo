namespace MovieDemo.Models
{
    public class MovieListItem
    {
        public int Id { get; set; }

        public int MovieListId { get; set; }
        public virtual MovieList MovieList { get; set; }

        public int MovieId { get; set; }
        public virtual Movie Movie { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.Now;
    }
}