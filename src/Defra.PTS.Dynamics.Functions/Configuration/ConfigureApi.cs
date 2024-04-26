using Defra.PTS.Common.ApiServices.Implementation;
using Defra.PTS.Common.ApiServices.Interface;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Defra.PTS.Dynamics.Functions.Configuration
{
    [ExcludeFromCodeCoverage]
    public static class ConfigureApi
    {
        public static IServiceCollection AddDefraApiServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IOwnerService, OwnerService>();
            services.AddScoped<IDynamicsService, DynamicsService>();
            services.AddScoped<IKeyVaultAccess, KeyVaultAccess>();
            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddScoped<IServiceBusService, ServiceBusService>();
            return services;
        }
    }
}
