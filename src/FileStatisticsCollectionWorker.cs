using LustreCollector.Filesystem;

namespace LustreCollector;

public class FileStatisticsCollectionWorker : BackgroundService
{
    private readonly ILogger<FileStatisticsCollectionWorker> _logger;
    private readonly string _mountPoint;
    private readonly IFilesystemChangeWatcher _changeWatcher;

    private readonly SortedSet<FileRecord> _activeFiles;

    public FileStatisticsCollectionWorker(ILogger<FileStatisticsCollectionWorker> logger,
        SortedSet<FileRecord> activeFiles, string mountPoint,
        IFilesystemChangeWatcher changeWatcher)
    {
        _logger = logger;
        _activeFiles = activeFiles;
        _mountPoint = mountPoint;
        _changeWatcher = changeWatcher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Build our initial view of the filesystem.
        await FilesystemWalker.Walk(_mountPoint, stoppingToken, file =>
        {
            lock (_activeFiles)
            {
                _activeFiles.Add(file);
            }
        });

        if (_activeFiles.Count > 0)
        {
            _logger.LogInformation($"Created initial set of filesystem records: {string.Join('\n', _activeFiles)}");
        }
        else
        {
            _logger.LogInformation("Filesystem is empty. Created empty set of filesystem records.");
        }

        await foreach (var change in _changeWatcher.Watch(new FileInfo(_mountPoint)).WithCancellation(stoppingToken))
        {
            var fileRecord = new FileRecord(change.Path, DateTime.UtcNow.ToFileTimeUtc());
            lock (_activeFiles)
            {
                if (change.Kind is FilesystemChangeEventKind.Accessed or FilesystemChangeEventKind.Created)
                {
                    _logger.LogInformation($"File Record added {fileRecord}");
                    _activeFiles.Add(fileRecord);
                }
                else
                {
                    _logger.LogInformation($@"File Record removed {fileRecord}");
                    _activeFiles.Remove(fileRecord);
                }
            }
        }
    }
}