namespace LustreCollector;

public class FileCleanupWorker : BackgroundService
{
    private readonly string _mountPoint;
    private readonly SortedSet<FileRecord> _activeFiles;
    private readonly int _cleanupPeriod;
    private readonly float _cleanupThreshold;

    public FileCleanupWorker(string mountPoint, SortedSet<FileRecord> activeFiles, int cleanupPeriod,
        int cleanupThreshold)
    {
        _mountPoint = mountPoint;
        _activeFiles = activeFiles;
        _cleanupPeriod = cleanupPeriod;
        _cleanupThreshold = cleanupThreshold;
    }

    private bool IsUnderFreeSpaceThreshold()
    {
        var mountInfo = new DriveInfo(_mountPoint);
        return mountInfo.AvailableFreeSpace / mountInfo.TotalSize * 100 < _cleanupThreshold;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_cleanupPeriod, stoppingToken);

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