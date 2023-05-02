using LustreCollector.FileSystem;

namespace LustreCollector.Tests.FileSystem;

public class FileSystemChangedEventTests
{
    [Fact]
    public void FromNativeEvent_Correct_Created()
    {
        // Arrange
        var args = new FileSystemEventArgs(WatcherChangeTypes.Created, "foo", "bar");
        var expected =
            new FileSystemChangeEvent(FileSystemChangeEventKind.Created, $"foo{Path.DirectorySeparatorChar}bar");

        // Act
        var changeEvent = FileSystemChangeEvent.FromNativeEvent(args);

        // Assert
        changeEvent.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void FromNativeEvent_Correct_Deleted()
    {
        // Arrange
        var args = new FileSystemEventArgs(WatcherChangeTypes.Deleted, "foo", "bar");
        var expected =
            new FileSystemChangeEvent(FileSystemChangeEventKind.Deleted, $"foo{Path.DirectorySeparatorChar}bar");

        // Act
        var changeEvent = FileSystemChangeEvent.FromNativeEvent(args);

        // Assert
        changeEvent.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void FromNativeEvent_Correct_Changed()
    {
        // Arrange
        var args = new FileSystemEventArgs(WatcherChangeTypes.Changed, "foo", "bar");
        var expected =
            new FileSystemChangeEvent(FileSystemChangeEventKind.Accessed, $"foo{Path.DirectorySeparatorChar}bar");

        // Act
        var changeEvent = FileSystemChangeEvent.FromNativeEvent(args);

        // Assert
        changeEvent.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void FromNativeEvent_Correct_All()
    {
        // Arrange
        var args = new FileSystemEventArgs(WatcherChangeTypes.All, "foo", "bar");
        var expected =
            new FileSystemChangeEvent(FileSystemChangeEventKind.Accessed, $"foo{Path.DirectorySeparatorChar}bar");

        // Act
        var changeEvent = FileSystemChangeEvent.FromNativeEvent(args);

        // Assert
        changeEvent.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void FromNativeEvent_ReturnsNull_Renamed()
    {
        // Arrange
        var args = new FileSystemEventArgs(WatcherChangeTypes.Renamed, "foo", "bar");

        // Act
        var changeEvent = FileSystemChangeEvent.FromNativeEvent(args);

        // Assert
        changeEvent.Should().BeNull();
    }
}