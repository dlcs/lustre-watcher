using LustreCollector;
using LustreCollector.Filesystem;
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

            services.AddSingleton<IFilesystemChangeWatcher, NativeFilesystemChangeWatcher>(provider =>
                ActivatorUtilities.CreateInstance<NativeFilesystemChangeWatcher>(provider, new FileSystemWatcher())
            );
            services.AddSingleton(x => new SortedSet<FileRecord>(new LustreFileAccessTimeComparer()));
            services.AddHostedService<FileCleanupWorker>();
            services.AddHostedService<FileStatisticsCollectionWorker>();
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