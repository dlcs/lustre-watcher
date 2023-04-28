using LustreCollector.Filesystem;
using Microsoft.Extensions.Options;

namespace LustreCollector;

public class FileStatisticsCollectionWorker : BackgroundService
{
    private readonly ILogger<FileStatisticsCollectionWorker> _logger;
    private readonly string _mountPoint;
    private readonly IFilesystemChangeWatcher _changeWatcher;

    private readonly SortedSet<FileRecord> _activeFiles;

    public FileStatisticsCollectionWorker(ILogger<FileStatisticsCollectionWorker> logger,
        SortedSet<FileRecord> activeFiles, IOptionsMonitor<FileCleanupConfiguration> config,
        IFilesystemChangeWatcher changeWatcher)
    {
        _logger = logger;
        _activeFiles = activeFiles;
        _mountPoint = config.CurrentValue.MountPoint;
        _changeWatcher = changeWatcher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FileStatisticsCollectionWorker starting");
        
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
            _logger.LogInformation("Created initial set of {InitialFiles} filesystem records", _activeFiles.Count);
        }
        else
        {
            _logger.LogInformation("Filesystem is empty. Created empty set of filesystem records");
        }

        await foreach (var change in _changeWatcher.Watch(new DirectoryInfo(_mountPoint)).WithCancellation(stoppingToken))
        {
            var fileRecord = new FileRecord(change.Path, DateTime.UtcNow.ToFileTimeUtc());
            lock (_activeFiles)
            {
                if (change.Kind is FilesystemChangeEventKind.Accessed or FilesystemChangeEventKind.Created)
                {
                    _logger.LogDebug("File Record added {FileRecord}", fileRecord);
                    _activeFiles.Add(fileRecord);
                }
                else
                {
                    _logger.LogDebug("File Record removed {FileRecord}", fileRecord);
                    _activeFiles.Remove(fileRecord);
                }
            }
        }
        
        _logger.LogInformation("FileStatisticsCollectionWorker stopping");
    }
}