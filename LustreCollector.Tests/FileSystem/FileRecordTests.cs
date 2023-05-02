using LustreCollector.FileSystem;

namespace LustreCollector.Tests.FileSystem;

public class FileRecordTests
{
    [Fact]
    public void FileRecord_False_CompareNull()
    {
        // Arrange
        var record = new FileRecord("foo", 123456);
        
        // Act
        var result = record.Equals(null);
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public void FileRecord_True_SameObject()
    {
        // Arrange
        var record = new FileRecord("foo", 123456);
        
        // Act
        var result = record.Equals(record);
        
        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(123456)]
    [InlineData(9876123)]
    public void FileRecord_Equal_SamePath(long accessTime)
    {
        // Arrange
        var record = new FileRecord("foo", 123456);
        var other = new FileRecord("foo", accessTime);
        
        // Act
        var result = record.Equals(other);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Theory]
    [InlineData("foo", "Foo")]
    [InlineData("foo", "bar")]
    public void FileRecord_NotEqual_DifferentPath(string path, string otherPath)
    {
        // Arrange
        var record = new FileRecord(path, 123456);
        var other = new FileRecord(otherPath, 123456);
        
        // Act
        var result = record.Equals(other);
        
        // Assert
        result.Should().BeFalse();
    }
}