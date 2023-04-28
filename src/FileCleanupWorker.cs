using LustreCollector.Filesystem;
using Microsoft.Extensions.Options;

namespace LustreCollector;

public class FileCleanupWorker : BackgroundService
{
    private readonly SortedSet<FileRecord> _activeFiles;
    private readonly IOptionsMonitor<FileCleanupConfiguration> _configuration;
    private readonly ILogger<FileCleanupWorker> _logger;

    public FileCleanupWorker(
        SortedSet<FileRecord> activeFiles,
        IOptionsMonitor<FileCleanupConfiguration> configuration,
        ILogger<FileCleanupWorker> logger)
    {
        _activeFiles = activeFiles;
        _configuration = configuration;
        _logger = logger;
    }

    private bool IsUnderFreeSpaceThreshold()
    {
        var configurationCurrentValue = _configuration.CurrentValue;
        var mountInfo = new DriveInfo(configurationCurrentValue.MountPoint);
        var mountInfoAvailableFreeSpace = (float)mountInfo.AvailableFreeSpace / mountInfo.TotalSize * 100;

        _logger.LogTrace("Free space available: {AvailableSpace}%, threshold: {CleanupThreshold}%",
            mountInfoAvailableFreeSpace, configurationCurrentValue.CleanupThreshold);
        return mountInfoAvailableFreeSpace < configurationCurrentValue.CleanupThreshold;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FileCleanupWorker starting");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_configuration.CurrentValue.CleanupPeriod, stoppingToken);

            //if (!IsUnderFreeSpaceThreshold()) continue;
            
            //var removalSet = new HashSet<FileRecord>();
            var haveActiveFiles = _activeFiles.Count > 0;
            while (!IsUnderFreeSpaceThreshold() && haveActiveFiles && !stoppingToken.IsCancellationRequested)
            {
                CleanupStaleFiles(stoppingToken);
            }

            /*if (!haveActiveFiles)
            {
                _logger.LogInformation("No active files, trying to walk files again");
                await FilesystemWalker.Walk(_configuration.CurrentValue.MountPoint, stoppingToken, file =>
                {
                    lock (_activeFiles)
                    {
                        _activeFiles.Add(file);
                    }
                });
            }*/

            /*lock (_activeFiles)
            {
                _activeFiles.RemoveWhere(item => removalSet.Contains(item));
            }*/
        }
        
        _logger.LogInformation("FileCleanupWorker stopping");
    }

    private void CleanupStaleFiles(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Not enough free space. Running cleanup.");
        List<FileRecord> deleteCandidates;
        lock (_activeFiles)
        {
            deleteCandidates = _activeFiles.TakeLast(_configuration.CurrentValue.CleanupBatchSize).ToList();
        }

        FileRecord? lastDeleted = null;
        var removalSet = new HashSet<FileRecord>();
        foreach (var file in deleteCandidates)
        {
            if (IsUnderFreeSpaceThreshold() || stoppingToken.IsCancellationRequested)
            {
                break;
            }

            _logger.LogTrace("Deleting file {FilePath}", file.FullPath);
            lastDeleted = file;
            File.Delete(file.FullPath);
            removalSet.Add(file);
        }
        
        lock (_activeFiles)
        {
            _activeFiles.RemoveWhere(item => removalSet.Contains(item));
        }

        if (lastDeleted != null)
        {
            var age = DateTime.UtcNow - DateTime.FromFileTimeUtc(lastDeleted.AccessTime);
            _logger.LogDebug("Oldest file deleted from batch was {AgeSecs}s", age.TotalSeconds);
        }
    }
}