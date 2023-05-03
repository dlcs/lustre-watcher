using LustreCollector.FileSystem;

namespace LustreCollector.Tests.FileSystem;

public class FileRecordComparerTests
{
    private FileRecordComparer sut;
    
    public FileRecordComparerTests()
    {
        sut = new FileRecordComparer();
    }
    
    [Fact]
    public void Compare_0_IfSameObject()
    {
        // Arrange
        var record = new FileRecord("foo", 123456);
        
        // Act
        var result = sut.Compare(record, record);

        // Assert
        result.Should().Be(0);
    }
    
    [Fact]
    public void Compare_1_IfSecondNull()
    {
        // Arrange
        var earlier = new FileRecord("foo", 123456);
        
        // Act
        var result = sut.Compare(earlier, null);

        // Assert
        result.Should().Be(1);
    }
    
    [Fact]
    public void Compare_Neg1_IfFirstNull()
    {
        // Arrange
        var later = new FileRecord("foo", 123457);
        
        // Act
        var result = sut.Compare(null, later);

        // Assert
        result.Should().Be(-1);
    }
    
    [Theory]
    [InlineData(12345, 12344, 1)]
    [InlineData(12345, 12345, 0)]
    [InlineData(12345, 12346, -1)]
    public void Compare_Correct_BasedOnAccessTime(long accessTimeX, long accessTimeY, int expected)
    {
        // Arrange
        var earlier = new FileRecord("foo", accessTimeX);
        var later = new FileRecord("bar", accessTimeY);
        
        // Act
        var result = sut.Compare(earlier, later);

        // Assert
        result.Should().Be(expected);
    }
}