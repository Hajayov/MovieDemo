using System;

namespace MovieDemo.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int Rating { get; set; } // 1 to 10 stars
        public string Comment { get; set; }
        public DateTime DatePosted { get; set; } = DateTime.Now;

        // The Movie being reviewed
        public int MovieId { get; set; }
        public Movie Movie { get; set; }

        // The User who wrote the review
        public int UserId { get; set; }
        public User User { get; set; }
    }
}