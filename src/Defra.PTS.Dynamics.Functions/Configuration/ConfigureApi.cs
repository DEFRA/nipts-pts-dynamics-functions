using Defra.PTS.Common.ApiServices.Implementation;
using Defra.PTS.Common.ApiServices.Interface;
using Defra.PTS.Common.Repositories.Implementation;
using Defra.PTS.Common.Repositories.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Dynamics.Functions.Configuration
{
    [ExcludeFromCodeCoverage]
    public static class ConfigureApi
    {
        public static IServiceCollection AddDefraApiServices(this IServiceCollection services)
        {
            services.AddScoped<IdcomsMappingValidator>();
            services.AddScoped<IIdcomsMappingValidator, IdcomsMappingValidator>();

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IOwnerService, OwnerService>();
            services.AddScoped<IDynamicsService, DynamicsService>();
            services.AddScoped<IKeyVaultAccess, KeyVaultAccess>();
            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddScoped<IServiceBusService, ServiceBusService>();

            services.AddScoped<IApplicationRepository, ApplicationRepository>();
            services.AddScoped<IOwnerRepository, OwnerRepository>();
            services.AddScoped<IAddressRepository, AddressRepository>();
            services.AddScoped<IPetRepository, PetRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITravelDocumentRepository, TravelDocumentRepository>();
            services.AddScoped<IBreedRepository, BreedRepository>();

            services.AddScoped(sp => new OfflineApplicationServiceOptions
            {
                ApplicationRepository = sp.GetRequiredService<IApplicationRepository>(),
                OwnerRepository = sp.GetRequiredService<IOwnerRepository>(),
                AddressRepository = sp.GetRequiredService<IAddressRepository>(),
                PetRepository = sp.GetRequiredService<IPetRepository>(),
                UserRepository = sp.GetRequiredService<IUserRepository>(),
                TravelDocumentRepository = sp.GetRequiredService<ITravelDocumentRepository>(),
                MappingValidator = sp.GetRequiredService<IdcomsMappingValidator>(),
                BreedRepository = sp.GetRequiredService<IBreedRepository>(),
                Logger = sp.GetRequiredService<ILogger<OfflineApplicationService>>()
            });

            services.AddScoped<IOfflineApplicationService, OfflineApplicationService>();

            return services;
        }
    }
}
