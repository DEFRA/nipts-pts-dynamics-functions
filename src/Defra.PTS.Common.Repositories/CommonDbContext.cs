using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using entity = Defra.PTS.Common.Entities;

namespace Defra.PTS.Common.Repositories
{
    [ExcludeFromCodeCoverage]
    public class CommonDbContext : DbContext
    {
        public CommonDbContext(DbContextOptions<CommonDbContext> options) : base(options)
        {
                
        }

        public DbSet<entity.User> User { get; set; }
        public DbSet<entity.Owner> Owner { get; set; }
        public DbSet<entity.Address> Address { get; set; }
        public DbSet<entity.Application> Application { get; set; }
        public DbSet<entity.Pet> Pet { get; set; }
        public DbSet<entity.Breed> Breed { get; set; }
        public DbSet<entity.Colour> Colour { get; set; }
        public DbSet<entity.TravelDocument> TravelDocument { get; set; }
    }
}