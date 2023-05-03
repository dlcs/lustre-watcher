using LustreCollector;
using LustreCollector.FileSystem;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    await Host.CreateDefaultBuilder(args)
        .ConfigureHostConfiguration(builder => builder.AddEnvironmentVariables("LUSTRE_"))
        .ConfigureServices((hostContext, services) =>
        {
            services.AddOptions<FileCleanupConfiguration>()
                .Bind(hostContext.Configuration)
                .ValidateDataAnnotations();
            
            services
                .AddSingleton<FileSystemWalker>()
                .AddSingleton<IFileSystemChangeWatcher, NativeFileSystemChangeWatcher>()
                .AddSingleton(_ => new SortedSet<FileRecord>(new FileRecordComparer()))
                .AddHostedService<FileCleanupWorker>()
                .AddHostedService<FileStatisticsCollectionWorker>();
        })
        .UseSerilog((hostContext, loggerConfiguration)
            => loggerConfiguration
                .ReadFrom.Configuration(hostContext.Configuration)
                .Enrich.FromLogContext()
        )
        .Build()
        .RunAsync();
}
catch (Exception ex)
{
    Log.Logger.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}