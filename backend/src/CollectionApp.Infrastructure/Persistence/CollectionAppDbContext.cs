using Microsoft.EntityFrameworkCore;

namespace CollectionApp.Infrastructure.Persistence
{
    /// <summary>
    /// EF Core context of the application. No entity yet (walking skeleton).
    /// </summary>
    public class CollectionAppDbContext : DbContext
    {
        public CollectionAppDbContext(DbContextOptions<CollectionAppDbContext> options) : base(options)
        {
        }
    }
}
