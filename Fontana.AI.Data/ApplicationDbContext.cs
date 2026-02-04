using Microsoft.EntityFrameworkCore;
using Fontana.AI.Models;

namespace Fontana.AI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<FaqItem> Faqs { get; set; }
    }
}
