namespace LustreCollector.Filesystem;

public class FilesystemWalker
{
    public static async Task Walk(string mountPoint, CancellationToken stoppingToken, Action<LustreFile> processor)
    {
        var roots = new Stack<string>();
        roots.Push(mountPoint);

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
            catch (FileNotFoundException ex)
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

                try {
                    var info = new FileInfo(file);
                    if (info.LinkTarget != null)
                    {
                        processor(new LustreFile(root, info.Name, info.Length,
                            info.LastAccessTime.ToFileTimeUtc()));
                    }
                }
                catch (FileNotFoundException e)
                {
                    // Could have been deleted since we seen it in the listing.
                    continue;
                }
            }

            foreach (var childRoot in childRoots)
            {
                roots.Push(childRoot);
            }
        }

    }
}