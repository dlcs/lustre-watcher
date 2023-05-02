using LustreCollector.FileSystem;

namespace LustreCollector;

/// <summary>
/// Monitors folder for changes raised by <see cref="IFileSystemChangeWatcher"/>
/// </summary>
public class FileStatisticsCollectionWorker : BackgroundService
{
    private readonly ILogger<FileStatisticsCollectionWorker> _logger;
    private readonly IFileSystemChangeWatcher _changeWatcher;
    private readonly FileSystemWalker _fileSystemWalker;

    private readonly SortedSet<FileRecord> _activeFiles;

    public FileStatisticsCollectionWorker(
        SortedSet<FileRecord> activeFiles, 
        IFileSystemChangeWatcher changeWatcher,
        FileSystemWalker fileSystemWalker,
        ILogger<FileStatisticsCollectionWorker> logger)
    {
        _logger = logger;
        _activeFiles = activeFiles;
        _changeWatcher = changeWatcher;
        _fileSystemWalker = fileSystemWalker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FileStatisticsCollectionWorker starting");
        
        BuildInitialView(stoppingToken);

        await foreach (var change in _changeWatcher.Watch(stoppingToken))
        {
            var fileRecord = new FileRecord(change.Path, DateTime.UtcNow.ToFileTimeUtc());
            lock (_activeFiles)
            {
                if (change.Kind is FileSystemChangeEventKind.Accessed or FileSystemChangeEventKind.Created)
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

    private void BuildInitialView(CancellationToken stoppingToken)
    {
        _fileSystemWalker.Walk(stoppingToken);

        var activeFileCount = 0;
        lock (_activeFiles)
        {
            activeFileCount = _activeFiles.Count;
        }

        if (activeFileCount > 0)
        {
            _logger.LogInformation("Created initial set of {InitialFiles} filesystem records",
                activeFileCount);
        }
        else
        {
            _logger.LogInformation("Filesystem is empty. Created empty set of filesystem records");
        }
    }
}