namespace LustreCollector;

public class LustreFileAccessTimeComparer : IComparer<LustreFile>
{
    public int Compare(LustreFile? x, LustreFile? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (ReferenceEquals(null, y)) return 1;
        if (ReferenceEquals(null, x)) return -1;
        return x.AccessTime.CompareTo(y.AccessTime);
    }
}

public record LustreFile(string Parent, string Name, long Size, long AccessTime);