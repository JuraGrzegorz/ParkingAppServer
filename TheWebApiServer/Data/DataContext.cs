using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TheWebApiServer.Model;

namespace TheWebApiServer.Data
{
    public class DataContext : IdentityDbContext
    {
        public DataContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Cars> cars { get; set; }
        public DbSet<OccupiedParkingPlace> occupiedParkingPlace { get; set; }
        public DbSet<ParkingPlace> parkingPlace {  get; set; }
        public DbSet<Payments> payments {  get; set; }
    }
}
