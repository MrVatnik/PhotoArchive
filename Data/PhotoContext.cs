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
            // Schema is managed by migrations, applied at startup (see DatabaseInitializer).
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

            // Reference data can't be deleted while something still uses it (no silent subtree deletes).
            // Only Film -> Photos cascades: deleting a film removes its photo records (files on disk are kept).
            modelBuilder.Entity<Camera>().HasOne(c => c.Format).WithMany().HasForeignKey(c => c.FormatId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Film>().HasOne(f => f.Camera).WithMany().HasForeignKey(f => f.CameraId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Film>().HasOne(f => f.Recipe).WithMany().HasForeignKey(f => f.RecipeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Recipe>().HasOne(r => r.FilmType).WithMany().HasForeignKey(r => r.FilmTypeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Recipe>().HasOne(r => r.Developer).WithMany().HasForeignKey(r => r.DeveloperId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Photo>().HasOne(p => p.Film).WithMany().HasForeignKey(p => p.FilmId).OnDelete(DeleteBehavior.Cascade);

            // Same as the v1.0 schema (decimal(18,2)); explicit to avoid the EF precision warning.
            modelBuilder.Entity<FilmType>().Property(f => f.ISO).HasPrecision(18, 2);
            modelBuilder.Entity<Recipe>().Property(r => r.EI).HasPrecision(18, 2);
        }
    }
}
