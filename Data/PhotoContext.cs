using Microsoft.EntityFrameworkCore;
using PhotoArchive.Models;

namespace PhotoArchive.Data
{
    public class PhotoContext : DbContext
    {
        public DbSet<Photo> Photos { get; set; }

        public PhotoContext(DbContextOptions<PhotoContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
    }
}
