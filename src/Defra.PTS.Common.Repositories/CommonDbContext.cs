using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Entity = Defra.PTS.Common.Entities;

namespace Defra.PTS.Common.Repositories
{
    [ExcludeFromCodeCoverage]
    public class CommonDbContext : DbContext
    {
        public CommonDbContext(DbContextOptions<CommonDbContext> options) : base(options)
        {
                
        }

        public DbSet<Entity.User>? User { get; set; }
        public DbSet<Entity.Owner> Owner { get; set; }
        public DbSet<Entity.Address> Address { get; set; }
        public DbSet<Entity.Application> Application { get; set; }
        public DbSet<Entity.Pet> Pet { get; set; }
        public DbSet<Entity.Breed> Breed { get; set; }
        public DbSet<Entity.Colour> Colour { get; set; }
        public DbSet<Entity.TravelDocument> TravelDocument { get; set; }
    }
}