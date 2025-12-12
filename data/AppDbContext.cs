using Microsoft.EntityFrameworkCore;

namespace RealEstateApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // لا يوجد DbSet هنا مؤقتًا
    }
}
