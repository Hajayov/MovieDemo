using Microsoft.EntityFrameworkCore;
using MovieDemo.Models;

namespace MovieDemo.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Movie> Movies { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<MovieList> MovieLists { get; set; }
        public DbSet<MovieListItem> MovieListItems { get; set; }

        // --- NEW: Table for the Rating System ---
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Existing: Many-to-Many Movies and Genres
            modelBuilder.Entity<Movie>()
                .HasMany(m => m.Genres)
                .WithMany(g => g.Movies);

            // Existing: User and MovieLists
            modelBuilder.Entity<MovieList>()
                .HasOne(l => l.User)
                .WithMany(u => u.MovieLists)
                .HasForeignKey(l => l.UserId);

            // Existing: MovieListItem bridge
            modelBuilder.Entity<MovieListItem>()
                .HasOne(mi => mi.MovieList)
                .WithMany(l => l.Items)
                .HasForeignKey(mi => mi.MovieListId);

            modelBuilder.Entity<MovieListItem>()
                .HasOne(mi => mi.Movie)
                .WithMany(m => m.ListItems)
                .HasForeignKey(mi => mi.MovieId);

            // --- NEW: Review Relationships ---

            // Link Review to Movie (One Movie has many Reviews)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Movie)
                .WithMany(m => m.Reviews) // Note: You'll need to add public ICollection<Review> Reviews { get; set; } to your Movie model
                .HasForeignKey(r => r.MovieId);

            // Link Review to User (One User has many Reviews)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews) // Note: You'll need to add public ICollection<Review> Reviews { get; set; } to your User model
                .HasForeignKey(r => r.UserId);
        }
    }
}