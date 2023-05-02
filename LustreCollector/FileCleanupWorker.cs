using LustreCollector.FileSystem;
using Microsoft.Extensions.Options;

namespace LustreCollector;

/// <summary>
/// This class periodically checks free disk space - if threshold breached then it will delete files to clear disk space
/// </summary>
public class FileCleanupWorker : BackgroundService
{
    private readonly SortedSet<FileRecord> _activeFiles;
    private readonly FileSystemWalker _fileSystemWalker;
    private readonly IOptionsMonitor<FileCleanupConfiguration> _configuration;
    private readonly ILogger<FileCleanupWorker> _logger;

    public FileCleanupWorker(
        SortedSet<FileRecord> activeFiles,
        FileSystemWalker fileSystemWalker,
        IOptionsMonitor<FileCleanupConfiguration> configuration,
        ILogger<FileCleanupWorker> logger)
    {
        _activeFiles = activeFiles;
        _fileSystemWalker = fileSystemWalker;
        _configuration = configuration;
        _logger = logger;
    }

    private bool HaveEnoughFreeSpace()
    {
        var configurationCurrentValue = _configuration.CurrentValue;
        var mountInfo = new DriveInfo(configurationCurrentValue.MountPoint);
        var mountInfoAvailableFreeSpace = (float)mountInfo.AvailableFreeSpace / mountInfo.TotalSize * 100;

        _logger.LogTrace("Free space available: {AvailableSpace}%, threshold: {CleanupThreshold}%",
            mountInfoAvailableFreeSpace, configurationCurrentValue.FreeSpaceThreshold);
        return mountInfoAvailableFreeSpace > configurationCurrentValue.FreeSpaceThreshold;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FileCleanupWorker starting");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_configuration.CurrentValue.CleanupPeriod, stoppingToken);
            
            // Check if there are known files that could be deleted - avoids infinite loop
            bool haveActiveFiles;
            lock (_activeFiles)
            {
                haveActiveFiles = _activeFiles.Count > 0;    
            }
            
            while (!HaveEnoughFreeSpace() && haveActiveFiles && !stoppingToken.IsCancellationRequested)
            {
                CleanupStaleFiles(stoppingToken);
                lock (_activeFiles)
                {
                    haveActiveFiles = _activeFiles.Count > 0;    
                }
            }

            if (!haveActiveFiles)
            {
                _logger.LogWarning("No active files, trying to walk files again - has watcher stopped?");
                _fileSystemWalker.Walk(stoppingToken);
            }
        }
        
        _logger.LogInformation("FileCleanupWorker stopping");
    }

    private void CleanupStaleFiles(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Not enough free space. Running cleanup.");
        List<FileRecord> deleteCandidates;
        lock (_activeFiles)
        {
            deleteCandidates = _activeFiles.Reverse().Take(_configuration.CurrentValue.CleanupBatchSize).ToList();
        }

        FileRecord? lastDeleted = null;
        var removalSet = new HashSet<FileRecord>();
        var minimiseDeletions = _configuration.CurrentValue.MinimiseDeletions;
        foreach (var file in deleteCandidates)
        {
            if ((minimiseDeletions && HaveEnoughFreeSpace()) || stoppingToken.IsCancellationRequested)
            {
                break;
            }

            _logger.LogTrace("Deleting file {FilePath}", file.FullPath);
            lastDeleted = file;
            DeleteFile(file, removalSet);
        }
        
        lock (_activeFiles)
        {
            _activeFiles.RemoveWhere(item => removalSet.Contains(item));
        }

        if (lastDeleted != null)
        {
            var age = DateTime.UtcNow - DateTime.FromFileTimeUtc(lastDeleted.AccessTime);
            _logger.LogDebug("Youngest file deleted from batch was {AgeSecs}s", age.TotalSeconds);
        }
    }

    private void DeleteFile(FileRecord file, HashSet<FileRecord> removalSet)
    {
        try
        {
            var fileInfo = new FileInfo(file.FullPath);
            var di = fileInfo.Directory;
            fileInfo.Delete();
            removalSet.Add(file);

            // Walk up, deleting dirs until there are no more
            while (di != null
                   && di.FullName != _configuration.CurrentValue.MountPoint
                   && !di.EnumerateFiles().Any()
                   && !di.EnumerateDirectories().Any())
            {
                di.Delete();
                di = di.Parent;
            }
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogError("UnauthorizedAccessException deleting file {FilePath}", file.FullPath);
        }
        catch (DirectoryNotFoundException)
        {
            removalSet.Add(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FilePath}", file.FullPath);
        }
    }
}