using Defra.PTS.Common.Repositories;
using Defra.PTS.Common.Repositories.Implementation;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Common.ApiServices.Configuration
{
    [ExcludeFromCodeCoverage]
    public static class ConfigureRepositories
    {
        public static IServiceCollection AddDefraRepositoryServices(this IServiceCollection services, string conn)
        {
            services.AddDbContext<CommonDbContext>(options =>
            {
                options.UseSqlServer(conn);
            });

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IOwnerRepository, OwnerRepository>();
            services.AddScoped<IApplicationRepository, ApplicationRepository>();
            services.AddScoped<IAddressRepository, AddressRepository>();
            services.AddScoped<IPetRepository, PetRepository>();
            services.AddScoped<IBreedRepository, BreedRepository>();
            services.AddScoped<IColourRepository, ColourRepository>();
            services.AddScoped<ITravelDocumentRepository, TravelDocumentRepository>();

            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));

            return services;
        }
    }
}
