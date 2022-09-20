using LustreCollector.Filesystem;

namespace LustreCollector;

public class LustreCollectionWorker : BackgroundService
{
    private readonly ILogger<LustreCollectionWorker> _logger;
    private readonly string _mountPoint;
    private readonly SortedSet<LustreFile> _activeFiles = new();

    public LustreCollectionWorker(string mountPoint, ILogger<LustreCollectionWorker> logger)
    {
        _mountPoint = mountPoint;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Build our initial view of the filesystem.
        await FilesystemWalker.Walk(_mountPoint, stoppingToken, file => _activeFiles.Add(file));
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}