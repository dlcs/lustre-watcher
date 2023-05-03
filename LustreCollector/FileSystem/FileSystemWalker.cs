using Microsoft.Extensions.Options;

namespace LustreCollector.FileSystem;

/// <summary>
/// Class will walk FileSystem starting at mountpoint and building a list of all 
/// </summary>
public class FileSystemWalker
{
    private readonly IOptionsMonitor<FileCleanupConfiguration> _config;
    private readonly SortedSet<FileRecord> _activeFiles;

    public FileSystemWalker(IOptionsMonitor<FileCleanupConfiguration> config, SortedSet<FileRecord> activeFiles)
    {
        _config = config;
        _activeFiles = activeFiles;
    }
    
    public void Walk(CancellationToken stoppingToken)
    {
        var roots = new Stack<string>();
        roots.Push(_config.CurrentValue.MountPoint);

        while (roots.Count > 0)
        {
            var root = roots.Pop();
            string[] childRoots;
            string[] childFiles;

            try
            {
                childRoots = Directory.GetDirectories(root);
                childFiles = Directory.GetFiles(root);
            }
            catch (FileNotFoundException)
            {
                // Root has been removed from under us while processing.
                continue;
            }

            foreach (string file in childFiles)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    var info = new FileInfo(file);
                    if (info.LinkTarget == null)
                    {
                        var fr = new FileRecord(info.FullName, info.LastAccessTime.ToFileTimeUtc());
                        lock (_activeFiles)
                        {
                            _activeFiles.Add(fr);
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    // Could have been deleted since we seen it in the listing.
                }
            }

            foreach (var childRoot in childRoots)
            {
                roots.Push(childRoot);
            }
        }
    }
}