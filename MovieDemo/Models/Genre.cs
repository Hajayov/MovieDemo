namespace MovieDemo.Models
{
    public class Genre
    {
        public int Id { get; set; } // This will store the ID from your CSV (e.g., 16)
        public string Name { get; set; }

        // Navigation property for the relationship
        public List<Movie> Movies { get; set; } = new();
    }
}