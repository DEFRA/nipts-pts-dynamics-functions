using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Defra.PTS.Common.ApiServices.Configuration;
using Defra.PTS.Common.Models.Options;
using Defra.PTS.Dynamics.Functions.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using static Defra.PTS.Common.Models.ConfigKeys;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureOpenApi()
    .ConfigureAppConfiguration(builder =>
    {
        builder
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        string clientSecret = string.Empty;
        string clientId = string.Empty;
        string tenantId = string.Empty;
        string keyVaultEndpoint = configuration["KeyVaultUri"];
        if (!string.IsNullOrEmpty(keyVaultEndpoint))
        {
            var secretClient = new SecretClient(new Uri(keyVaultEndpoint), new DefaultAzureCredential());
            services.AddSingleton(secretClient);

            clientSecret = secretClient.GetSecret("Pts-Dynamics-Tenant-ClientSecret").Value.Value;
            clientId = secretClient.GetSecret("Pts-Dynamics-Tenant-ClientId").Value.Value;
            tenantId = secretClient.GetSecret("Pts-Dynamics-Tenant-TenantId").Value.Value;
        }

        string authority = configuration["DynamicOptions:Authority"] + tenantId;

        var confidentialClient = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(new Uri(authority))
            .Build();

        services.AddSingleton(confidentialClient);
        services.Configure<DynamicOptions>(options => configuration.GetSection("DynamicOptions").Bind(options));
        services.Configure<AzureServiceBusOptions>(options => configuration.GetSection("AzureServiceBusOptions").Bind(options));

        string sqlconnection;
        string serviceBusConnection;
        ServiceBusClient serviceBusClient;

#if DEBUG
        sqlconnection = configuration["sql_db"];
        serviceBusConnection = configuration["ServiceBusConnection"];
        serviceBusClient = new ServiceBusClient(serviceBusConnection, new ServiceBusClientOptions { TransportType = ServiceBusTransportType.AmqpWebSockets });
#else
        sqlconnection = configuration.GetConnectionString("sql_db");
        serviceBusConnection = configuration.GetValue<string>(ServiceBusNamespace);
        serviceBusClient = new ServiceBusClient(serviceBusConnection, new DefaultAzureCredential());
#endif

        // Register ServiceBusClient as a SINGLETON. ServiceBusClient is IAsyncDisposable, so
        // registering a captured instance with a disposing lifetime (e.g. AddTransient(_ => client))
        // makes the DI container dispose the shared client at the end of the first invocation scope.
        // Every subsequent send then fails permanently with
        // ObjectDisposedException: "ServiceBusConnection has already been closed".
        services.AddSingleton(serviceBusClient);

        services.AddHttpClient();
        services.AddDefraRepositoryServices(sqlconnection);
        services.AddDefraApiServices();
    })
    .Build();

await host.RunAsync();
