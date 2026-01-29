namespace MovieDemo.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "User";

        // Navigation property for the user's lists
        public virtual ICollection<MovieList> MovieLists { get; set; } = new List<MovieList>();
        // Connection to Reviews
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}