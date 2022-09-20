using LustreCollector;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => { services.AddHostedService<LustreCollectionWorker>(); })
    .Build();

await host.RunAsync();