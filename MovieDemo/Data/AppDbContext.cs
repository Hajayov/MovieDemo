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

        // --- New Tables for Lists ---
        public DbSet<MovieList> MovieLists { get; set; }
        public DbSet<MovieListItem> MovieListItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Existing: Many-to-Many relationship between Movies and Genres
            modelBuilder.Entity<Movie>()
                .HasMany(m => m.Genres)
                .WithMany(g => g.Movies);

            // New: Relationship between User and MovieLists
            modelBuilder.Entity<MovieList>()
                .HasOne(l => l.User)
                .WithMany(u => u.MovieLists)
                .HasForeignKey(l => l.UserId);

            // New: Many-to-Many bridge (MovieListItem)
            modelBuilder.Entity<MovieListItem>()
                .HasOne(mi => mi.MovieList)
                .WithMany(l => l.Items)
                .HasForeignKey(mi => mi.MovieListId);

            modelBuilder.Entity<MovieListItem>()
                .HasOne(mi => mi.Movie)
                .WithMany(m => m.ListItems)
                .HasForeignKey(mi => mi.MovieId);
        }
    }
}