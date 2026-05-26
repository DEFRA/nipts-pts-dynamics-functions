using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Defra.PTS.Common.ApiServices.Configuration;
using Defra.PTS.Common.Models.Options;
using Defra.PTS.Dynamics.Functions.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using static Defra.PTS.Common.Models.ConfigKeys;

namespace Defra.PTS.Dynamics.Functions;

[ExcludeFromCodeCoverage]
public static class Program
{
    public static void Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWebApplication()
            .ConfigureOpenApi()
            .ConfigureAppConfiguration((context, builder) =>
            {
                builder
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;

                try
                {
                    string clientSecret = string.Empty;
                    string clientId = string.Empty;
                    string tenantId = string.Empty;
                    string keyVaultEndpoint = configuration["KeyVaultUri"] ?? "https://devtrdinfkv1001.vault.azure.net/";

                    if (!string.IsNullOrEmpty(keyVaultEndpoint))
                    {
                        var secretClient = new SecretClient(new Uri(keyVaultEndpoint), new DefaultAzureCredential());
                        services.AddSingleton(secretClient);
                        KeyVaultSecret keyVaultClientSecret = secretClient.GetSecret("Pts-Dynamics-Tenant-ClientSecret");
                        KeyVaultSecret keyVaultClientId = secretClient.GetSecret("Pts-Dynamics-Tenant-ClientId");
                        KeyVaultSecret keyVaultTenantId = secretClient.GetSecret("Pts-Dynamics-Tenant-TenantId");

                        clientSecret = keyVaultClientSecret.Value.ToString();
                        tenantId = keyVaultTenantId.Value.ToString();
                        clientId = keyVaultClientId.Value.ToString();
                    }

                    string authority = "https://login.microsoftonline.com/" + tenantId;

                    var confidentialClient = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithClientSecret(clientSecret)
                        .WithAuthority(new Uri(authority))
                        .Build();

                    services.AddSingleton(confidentialClient);
                    services.Configure<DynamicOptions>(options => configuration.GetSection("DynamicOptions").Bind(options));
                    services.Configure<AzureServiceBusOptions>(options => configuration.GetSection("AzureServiceBusOptions").Bind(options));

                    var sqlconnection = string.Empty;
                    var serviceBusConnection = string.Empty;
                    ServiceBusClient? serviceBusClient = null;

#if DEBUG
                    sqlconnection = configuration["sql_db"];
                    serviceBusConnection = configuration["ServiceBusConnection"];
                    serviceBusClient = new ServiceBusClient(serviceBusConnection, new ServiceBusClientOptions() { TransportType = ServiceBusTransportType.AmqpWebSockets });
#else
                    sqlconnection = configuration.GetConnectionString("sql_db");
                    serviceBusConnection = configuration.GetValue<string>(ServiceBusNamespace);
                    serviceBusClient = new ServiceBusClient(serviceBusConnection, new DefaultAzureCredential());
#endif

                    services.AddSingleton(serviceBusClient);
                    services.AddDefraRepositoryServices(sqlconnection ?? string.Empty);
                    services.AddDefraApiServices();
                    services.AddHttpClient();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: {0}", ex);
                    throw;
                }
            })
            .Build();

        host.Run();
    }
}
