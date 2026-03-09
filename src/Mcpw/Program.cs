using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mcpw;
using Mcpw.Tools;
using Mcpw.Windows;

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options => { options.ServiceName = "mcpw"; })
    .ConfigureServices((_, services) =>
    {
        // Config
        var config = ConfigLoader.Load();
        services.AddSingleton(config);

        // Windows abstractions
        services.AddSingleton<IWmiClient,      WmiClient>();
        services.AddSingleton<IEventLogAccess, EventLogAccess>();
        services.AddSingleton<IRegistryAccess, RegistryAccess>();
        services.AddSingleton<IServiceControl, ServiceControl>();
        services.AddSingleton<IPowerShellHost, PowerShellHost>();

        // Tool handlers — registered conditionally by config
        RegisterIfEnabled<SystemTools>(services,   config, "system");
        RegisterIfEnabled<ProcessTools>(services,  config, "process");
        RegisterIfEnabled<ServiceTools>(services,  config, "service");
        RegisterIfEnabled<LogTools>(services,      config, "log");
        RegisterIfEnabled<NetworkTools>(services,  config, "network");
        RegisterIfEnabled<FileTools>(services,     config, "file");
        RegisterIfEnabled<IdentityTools>(services, config, "identity");
        RegisterIfEnabled<StorageTools>(services,  config, "storage");
        RegisterIfEnabled<SecurityTools>(services, config, "security");
        RegisterIfEnabled<ContainerTools>(services,config, "container");
        RegisterIfEnabled<HardwareTools>(services, config, "hardware");
        RegisterIfEnabled<ScheduleTools>(services, config, "schedule");
        RegisterIfEnabled<RegistryTools>(services, config, "registry");
        RegisterIfEnabled<IISTools>(services,      config, "iis");
        RegisterIfEnabled<ADTools>(services,       config, "ad");
        RegisterIfEnabled<HyperVTools>(services,   config, "hyperv");
        RegisterIfEnabled<GPOTools>(services,      config, "gpo");

        // Server + service host
        services.AddSingleton<McpServer>();
        services.AddHostedService<Daemon>();
    })
    .Build();

await host.RunAsync();

// ── helpers ────────────────────────────────────────────────────────────────

static void RegisterIfEnabled<T>(IServiceCollection services, McpwConfig config, string domain)
    where T : class, IToolHandler
{
    if (config.IsDomainEnabled(domain))
        services.AddSingleton<IToolHandler, T>();
}
