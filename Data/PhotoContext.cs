using Microsoft.EntityFrameworkCore;
using PhotoArchive.Models;

namespace PhotoArchive.Data
{
    public class PhotoContext : DbContext
    {
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Film> Films { get; set; }

        public PhotoContext(DbContextOptions<PhotoContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Photo>().ToTable("Photo");
            modelBuilder.Entity<Film>().ToTable("Film");
        }
    }
}
