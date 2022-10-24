namespace LustreCollector;

public class FileCleanupConfiguration
{
    public string MountPoint { get; set; }

    public int CleanupThreshold { get; set; } = 10;
    public int CleanupPeriod { get; set; } = 2_000;
}