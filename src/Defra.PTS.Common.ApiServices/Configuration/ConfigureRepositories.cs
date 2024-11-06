using Defra.PTS.Common.Repositories;
using Defra.PTS.Common.Repositories.Implementation;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Common.ApiServices.Configuration
{
    [ExcludeFromCodeCoverage]
    public static class ConfigureRepositories
    {        
        public static IServiceCollection AddDefraRepositoryServices(this IServiceCollection services, string conn)
        {
            services.AddDbContext<CommonDbContext>((context) =>
            {
                context.UseSqlServer(conn);
            });
            services.AddScoped<DbContext, CommonDbContext>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IOwnerRepository, OwnerRepository>();
            services.AddScoped<IApplicationRepository, ApplicationRepository>();
            services.AddScoped<IAddressRepository, AddressRepository>();
            services.AddScoped<IPetRepository, PetRepository>();
            services.AddScoped<ITravelDocumentRepository, TravelDocumentRepository>();
            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));

            return services;
        }
    }
}
