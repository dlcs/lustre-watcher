namespace LustreCollector.FileSystem;

public class LustreFileAccessTimeComparer : IComparer<FileRecord>
{
    public int Compare(FileRecord? x, FileRecord? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (ReferenceEquals(null, y)) return 1;
        if (ReferenceEquals(null, x)) return -1;
        return x.AccessTime.CompareTo(y.AccessTime);
    }
}

/// <summary>
/// Represents a file change record 
/// </summary>
public record FileRecord(string FullPath, long AccessTime)
{
    public virtual bool Equals(FileRecord? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return FullPath == other.FullPath;
    }

    public override int GetHashCode()
    {
        return FullPath.GetHashCode();
    }
}