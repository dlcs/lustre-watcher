using LustreCollector;
using LustreCollector.Filesystem;

await Host.CreateDefaultBuilder(args)
    .ConfigureHostConfiguration(builder => builder.AddEnvironmentVariables("LUSTRE_"))
    .ConfigureServices((hostContext, services) =>
    {
        services.AddOptions<FileCleanupConfiguration>()
            .Bind(hostContext.Configuration)
            .ValidateDataAnnotations();
        
        services.AddSingleton<IFilesystemChangeWatcher, NativeFilesystemChangeWatcher>();
        services.AddSingleton(x => new SortedSet<FileRecord>(new LustreFileAccessTimeComparer()));
        services.AddHostedService<FileCleanupWorker>();
        services.AddHostedService<FileStatisticsCollectionWorker>();
    })
    .Build()
    .RunAsync();