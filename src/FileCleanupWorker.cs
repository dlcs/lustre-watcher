namespace LustreCollector;

public class FileCleanupWorker : BackgroundService
{
    private readonly SortedSet<FileRecord> _activeFiles;
    private readonly FileCleanupConfiguration _configuration;

    public FileCleanupWorker(SortedSet<FileRecord> activeFiles, FileCleanupConfiguration configuration)
    {
        _activeFiles = activeFiles;
        _configuration = configuration;
    }

    private bool IsUnderFreeSpaceThreshold()
    {
        var mountInfo = new DriveInfo(_configuration.MountPoint);
        return mountInfo.AvailableFreeSpace / mountInfo.TotalSize * 100 < _configuration.CleanupThreshold;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_configuration.CleanupPeriod, stoppingToken);

            if (!IsUnderFreeSpaceThreshold()) continue;

            var removalSet = new HashSet<FileRecord>();
            foreach (var file in _activeFiles.Reverse())
            {
                if (!IsUnderFreeSpaceThreshold() || stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                File.Delete(file.FullPath);
                removalSet.Add(file);
            }

            lock (_activeFiles)
            {
                _activeFiles.RemoveWhere(item => removalSet.Contains(item));
            }
        }
    }
}