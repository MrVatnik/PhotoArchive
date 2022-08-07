using Microsoft.EntityFrameworkCore;
using PhotoArchive.Models;

namespace PhotoArchive.Data
{
    public class PhotoContext : DbContext
    {
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Film> Films { get; set; }
        public DbSet<FilmType> FilmTypes { get; set; }
        public DbSet<Developer> Developers { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Camera> Cameras { get; set; }
        public DbSet<Format> Formats { get; set; }


        public PhotoContext(DbContextOptions<PhotoContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Photo>().ToTable("Photos");
            modelBuilder.Entity<Film>().ToTable("Films");
            modelBuilder.Entity<FilmType>().ToTable("FilmTypes");
            modelBuilder.Entity<Developer>().ToTable("Developers");
            modelBuilder.Entity<Recipe>().ToTable("Recipes");
            modelBuilder.Entity<Camera>().ToTable("Cameras");
            modelBuilder.Entity<Format>().ToTable("Formats");
        }
    }
}
