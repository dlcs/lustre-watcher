using Microsoft.Extensions.Options;

namespace LustreCollector;

public class FileCleanupWorker : BackgroundService
{
    private readonly SortedSet<FileRecord> _activeFiles;
    private readonly IOptionsMonitor<FileCleanupConfiguration> _configuration;

    public FileCleanupWorker(SortedSet<FileRecord> activeFiles, IOptionsMonitor<FileCleanupConfiguration> configuration)
    {
        _activeFiles = activeFiles;
        _configuration = configuration;
    }

    private bool IsUnderFreeSpaceThreshold()
    {
        var configurationCurrentValue = _configuration.CurrentValue;
        var mountInfo = new DriveInfo(configurationCurrentValue.MountPoint);
        return mountInfo.AvailableFreeSpace / mountInfo.TotalSize * 100 < configurationCurrentValue.CleanupThreshold;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_configuration.CurrentValue.CleanupPeriod, stoppingToken);

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