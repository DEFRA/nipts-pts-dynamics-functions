using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Defra.PTS.Common.ApiServices.Configuration;
using Defra.PTS.Common.Models.Options;
using Defra.PTS.Dynamics.Functions.Configuration;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using static Defra.PTS.Common.Models.ConfigKeys;


[assembly: FunctionsStartup(typeof(Defra.PTS.Dynamics.Functions.Startup))]
namespace Defra.PTS.Dynamics.Functions
{
    [ExcludeFromCodeCoverage]
    public class Startup : FunctionsStartup
    {
        private static IConfiguration Configuration { get; set; }
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            Configuration = builder.ConfigurationBuilder
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables().Build();
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            try
            {
                Configuration = builder.GetContext().Configuration;

                string clientSecret = string.Empty;
                string clientId = string.Empty;
                string tenantId = string.Empty;
                string keyVaultEndpoint = Configuration["KeyVaultUri"];
                if (!string.IsNullOrEmpty(keyVaultEndpoint))
                {
                    var secretClient = new SecretClient(new Uri(keyVaultEndpoint), new DefaultAzureCredential());
                    builder.Services.AddSingleton(secretClient);
                    KeyVaultSecret keyVaultClientSecret = secretClient.GetSecret("Pts-Dynamics-Tenant-ClientSecret");
                    KeyVaultSecret keyVaultClientId = secretClient.GetSecret("Pts-Dynamics-Tenant-ClientId");
                    KeyVaultSecret keyVaultTenantId = secretClient.GetSecret("Pts-Dynamics-Tenant-TenantId");

                    clientSecret = keyVaultClientSecret.Value.ToString();
                    tenantId = keyVaultTenantId.Value.ToString();
                    clientId = keyVaultClientId.Value.ToString();
                }

                string authority = Configuration["DynamicOptions:Authority"] + tenantId;

                var confidentialClient = ConfidentialClientApplicationBuilder
                    .Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority(new Uri(authority))
                    .Build();

                builder.Services.AddSingleton(confidentialClient);
                builder.Services.Configure<DynamicOptions>(options => Configuration.GetSection("DynamicOptions").Bind(options));
                builder.Services.Configure<AzureServiceBusOptions>(options => Configuration.GetSection("AzureServiceBusOptions").Bind(options));

                var sqlconnection = string.Empty;
                var serviceBusConnection = string.Empty;
                ServiceBusClient serviceBusClient = null;

#if DEBUG
                sqlconnection = Configuration["sql_db"];
                serviceBusConnection = Configuration["ServiceBusConnection"];
                serviceBusClient = new ServiceBusClient(serviceBusConnection, new ServiceBusClientOptions() { TransportType = ServiceBusTransportType.AmqpWebSockets });
#else
                sqlconnection = Configuration.GetConnectionString("sql_db");
                serviceBusConnection = Configuration.GetValue<string>(ServiceBusNamespace);
                serviceBusClient = new ServiceBusClient(serviceBusConnection, new DefaultAzureCredential());
#endif


                builder.Services.AddTransient(_ => serviceBusClient);
                builder.Services.AddDefraRepositoryServices(sqlconnection);
                builder.Services.AddDefraApiServices();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Stack: " + ex.StackTrace);
                Console.WriteLine("Exception Message: " + ex.Message);
                throw;
            }
        }
    }
}
