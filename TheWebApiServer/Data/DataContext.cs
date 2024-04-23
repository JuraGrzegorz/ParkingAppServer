using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TheWebApiServer.Data
{
    public class DataContext : IdentityDbContext
    {
        public DataContext(DbContextOptions options) : base(options)
        {
        }
    }
}
