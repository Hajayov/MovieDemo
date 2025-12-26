namespace MovieDemo.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // In a real app, we'd hash this!
        public string Role { get; set; } = "User"; // Default role is User
    }
}