using LustreCollector;
using LustreCollector.Filesystem;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", true)
    .AddEnvironmentVariables("LUSTRE_")
    .Build();

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.Configure<FileCleanupConfiguration>("FileCleanup", configuration);
        services.AddSingleton<IFilesystemChangeWatcher, NativeFilesystemChangeWatcher>();
        services.AddSingleton<SortedSet<FileRecord>>(x =>
            new SortedSet<FileRecord>(new LustreFileAccessTimeComparer()));
        services.AddHostedService(x =>
        {
            return new FileCleanupWorker(mountPoint, x.GetRequiredService<SortedSet<FileRecord>>(), cleanupPeriod,
                cleanupThreshold);
        });
        services.AddHostedService(x =>
        {
            return new FileStatisticsCollectionWorker(
                x.GetRequiredService<ILogger<FileStatisticsCollectionWorker>>(),
                x.GetRequiredService<SortedSet<FileRecord>>(),
                mountPoint,
                x.GetRequiredService<IFilesystemChangeWatcher>());
        });
    })
    .Build();

await host.RunAsync();