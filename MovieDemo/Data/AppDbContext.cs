using Microsoft.EntityFrameworkCore;
using MovieDemo.Models;

namespace MovieDemo.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Movie> Movies { get; set; }

        // Changed from DbSetGenres to Genres to match the Seeder logic
        public DbSet<Genre> Genres { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // This tells EF Core to manage the "Connecting Table" automatically
            modelBuilder.Entity<Movie>()
                .HasMany(m => m.Genres)
                .WithMany(g => g.Movies);
        }
    }
}