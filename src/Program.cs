using LustreCollector;
using LustreCollector.Filesystem;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", true)
    .AddEnvironmentVariables("LUSTRE_")
    .Build();

await Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.Configure<FileCleanupConfiguration>("FileCleanup", configuration);
        services.AddSingleton<IFilesystemChangeWatcher, NativeFilesystemChangeWatcher>();
        services.AddSingleton(x => new SortedSet<FileRecord>(new LustreFileAccessTimeComparer()));
        services.AddHostedService<FileCleanupWorker>();
        services.AddHostedService<FileStatisticsCollectionWorker>();
    })
    .Build()
    .RunAsync();