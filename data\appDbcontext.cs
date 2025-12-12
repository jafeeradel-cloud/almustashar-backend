using Microsoft.EntityFrameworkCore;

namespace RealEstateApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // لاحقاً تضيف DbSet مثل:
        // public DbSet<Property> Properties { get; set; }
    }
}
