namespace KeystoneCommerce.Tests.Infrastructure_Services;

[Collection("ImageServiceTests")]
public class ImageServiceTest : IDisposable
{
    private readonly ImageService _sut;
    private readonly string _testDirectory;

    public ImageServiceTest()
    {
        _sut = new ImageService();
        _testDirectory = Path.Combine(Path.GetTempPath(), "ImageServiceTests", Guid.NewGuid().ToString());
    }

    public void Dispose()
    {
        // Cleanup test directory after each test
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    #region SaveImageAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task SaveImageAsync_ShouldCreateFile_WhenValidInputProvided()
    {
        // Arrange
        byte[] imageData = new byte[] { 1, 2, 3, 4, 5 };
        string imageType = ".jpg";

        // Act
        var result = await _sut.SaveImageAsync(imageData, imageType, _testDirectory);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().EndWith(imageType);
        var filePath = Path.Combine(_testDirectory, result);
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveImageAsync_ShouldCreateDirectory_WhenDirectoryDoesNotExist()
    {
        // Arrange
        byte[] imageData = new byte[] { 1, 2, 3, 4, 5 };
        string imageType = ".png";
        string newPath = Path.Combine(_testDirectory, "NewSubDirectory");

        // Act
        var result = await _sut.SaveImageAsync(imageData, imageType, newPath);

        // Assert
        Directory.Exists(newPath).Should().BeTrue();
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SaveImageAsync_ShouldWriteCorrectData_ToFile()
    {
        // Arrange
        byte[] imageData = new byte[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
        string imageType = ".jpg";

        // Act
        var result = await _sut.SaveImageAsync(imageData, imageType, _testDirectory);

        // Assert
        var filePath = Path.Combine(_testDirectory, result);
        var savedData = await File.ReadAllBytesAsync(filePath);
        savedData.Should().BeEquivalentTo(imageData);
    }

    [Fact]
    public async Task SaveImageAsync_ShouldGenerateUniqueFileName_ForEachCall()
    {
        // Arrange
        byte[] imageData = new byte[] { 1, 2, 3 };
        string imageType = ".jpg";

        // Act
        var result1 = await _sut.SaveImageAsync(imageData, imageType, _testDirectory);
        var result2 = await _sut.SaveImageAsync(imageData, imageType, _testDirectory);

        // Assert
        result1.Should().NotBe(result2);
        File.Exists(Path.Combine(_testDirectory, result1)).Should().BeTrue();
        File.Exists(Path.Combine(_testDirectory, result2)).Should().BeTrue();
    }

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".png")]
    [InlineData(".gif")]
    [InlineData(".webp")]
    [InlineData(".jpeg")]
    public async Task SaveImageAsync_ShouldSupportDifferentImageTypes(string imageType)
    {
        // Arrange
        byte[] imageData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var result = await _sut.SaveImageAsync(imageData, imageType, _testDirectory);

        // Assert
        result.Should().EndWith(imageType);
        File.Exists(Path.Combine(_testDirectory, result)).Should().BeTrue();
    }

    [Fact]
    public async Task SaveImageAsync_ShouldHandleLargeImageData()
    {
        // Arrange - 5MB of data
        byte[] imageData = new byte[5 * 1024 * 1024];
        new Random().NextBytes(imageData);
        string imageType = ".jpg";

        // Act
        var result = await _sut.SaveImageAsync(imageData, imageType, _testDirectory);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var filePath = Path.Combine(_testDirectory, result);
        var fileInfo = new FileInfo(filePath);
        fileInfo.Length.Should().Be(imageData.Length);
    }

    [Fact]
    public async Task SaveImageAsync_ShouldHandleMinimalImageData()
    {
        // Arrange - 1 byte
        byte[] imageData = new byte[] { 1 };
        string imageType = ".jpg";

        // Act
        var result = await _sut.SaveImageAsync(imageData, imageType, _testDirectory);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var filePath = Path.Combine(_testDirectory, result);
        var savedData = await File.ReadAllBytesAsync(filePath);
        savedData.Should().HaveCount(1);
    }

    [Fact]
    public async Task SaveImageAsync_ShouldReturnFileNameWithGuidFormat()
    {
        // Arrange
        byte[] imageData = new byte[] { 1, 2, 3 };
        string imageType = ".jpg";

        // Act
        var result = await _sut.SaveImageAsync(imageData, imageType, _testDirectory);

        // Assert
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(result);
        Guid.TryParse(fileNameWithoutExtension, out _).Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task SaveImageAsync_ShouldHandleEmptyByteArray()
    {
        // Arrange
        byte[] imageData = Array.Empty<byte>();
        string imageType = ".jpg";

        // Act
        var result = await _sut.SaveImageAsync(imageData, imageType, _testDirectory);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var filePath = Path.Combine(_testDirectory, result);
        var fileInfo = new FileInfo(filePath);
        fileInfo.Length.Should().Be(0);
    }

    [Fact]
    public async Task SaveImageAsync_ShouldHandlePathWithSpaces()
    {
        // Arrange
        byte[] imageData = new byte[] { 1, 2, 3 };
        string imageType = ".jpg";
        string pathWithSpaces = Path.Combine(_testDirectory, "path with spaces");

        // Act
        var result = await _sut.SaveImageAsync(imageData, imageType, pathWithSpaces);

        // Assert
        result.Should().NotBeNullOrEmpty();
        File.Exists(Path.Combine(pathWithSpaces, result)).Should().BeTrue();
    }

    [Fact]
    public async Task SaveImageAsync_ShouldHandleNestedDirectories()
    {
        // Arrange
        byte[] imageData = new byte[] { 1, 2, 3 };
        string imageType = ".jpg";
        string nestedPath = Path.Combine(_testDirectory, "level1", "level2", "level3");

        // Act
        var result = await _sut.SaveImageAsync(imageData, imageType, nestedPath);

        // Assert
        Directory.Exists(nestedPath).Should().BeTrue();
        File.Exists(Path.Combine(nestedPath, result)).Should().BeTrue();
    }

    [Fact]
    public async Task SaveImageAsync_ShouldHandlePathWithExistingDirectory()
    {
        // Arrange
        byte[] imageData = new byte[] { 1, 2, 3 };
        string imageType = ".jpg";
        Directory.CreateDirectory(_testDirectory);

        // Act
        var result = await _sut.SaveImageAsync(imageData, imageType, _testDirectory);

        // Assert
        result.Should().NotBeNullOrEmpty();
        File.Exists(Path.Combine(_testDirectory, result)).Should().BeTrue();
    }

    #endregion

    #region Concurrent Scenarios

    [Fact]
    public async Task SaveImageAsync_ShouldHandleConcurrentWrites()
    {
        // Arrange
        var tasks = new List<Task<string>>();
        string imageType = ".jpg";

        // Act
        for (int i = 0; i < 10; i++)
        {
            byte[] imageData = new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2) };
            tasks.Add(_sut.SaveImageAsync(imageData, imageType, _testDirectory));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Distinct().Should().HaveCount(10); // All unique file names
        foreach (var result in results)
        {
            File.Exists(Path.Combine(_testDirectory, result)).Should().BeTrue();
        }
    }

    #endregion

    #endregion

    #region DeleteImageAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task DeleteImageAsync_ShouldDeleteFile_WhenFileExists()
    {
        // Arrange
        byte[] imageData = new byte[] { 1, 2, 3 };
        string imageType = ".jpg";
        var fileName = await _sut.SaveImageAsync(imageData, imageType, _testDirectory);
        var filePath = Path.Combine(_testDirectory, fileName);
        File.Exists(filePath).Should().BeTrue(); // Pre-condition

        // Act
        await _sut.DeleteImageAsync(_testDirectory, fileName);

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteImageAsync_ShouldNotThrow_WhenFileDoesNotExist()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        string nonExistentFile = "non-existent-file.jpg";

        // Act
        var act = async () => await _sut.DeleteImageAsync(_testDirectory, nonExistentFile);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteImageAsync_ShouldNotThrow_WhenDirectoryDoesNotExist()
    {
        // Arrange
        string nonExistentDirectory = Path.Combine(_testDirectory, "non-existent");
        string fileName = "test.jpg";

        // Act
        var act = async () => await _sut.DeleteImageAsync(nonExistentDirectory, fileName);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".png")]
    [InlineData(".gif")]
    public async Task DeleteImageAsync_ShouldDeleteDifferentImageTypes(string imageType)
    {
        // Arrange
        byte[] imageData = new byte[] { 1, 2, 3 };
        var fileName = await _sut.SaveImageAsync(imageData, imageType, _testDirectory);
        var filePath = Path.Combine(_testDirectory, fileName);

        // Act
        await _sut.DeleteImageAsync(_testDirectory, fileName);

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task DeleteImageAsync_ShouldNotDeleteOtherFiles_InSameDirectory()
    {
        // Arrange
        byte[] imageData = new byte[] { 1, 2, 3 };
        var fileName1 = await _sut.SaveImageAsync(imageData, ".jpg", _testDirectory);
        var fileName2 = await _sut.SaveImageAsync(imageData, ".jpg", _testDirectory);

        // Act
        await _sut.DeleteImageAsync(_testDirectory, fileName1);

        // Assert
        File.Exists(Path.Combine(_testDirectory, fileName1)).Should().BeFalse();
        File.Exists(Path.Combine(_testDirectory, fileName2)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteImageAsync_ShouldReturnCompletedTask()
    {
        // Arrange
        byte[] imageData = new byte[] { 1, 2, 3 };
        var fileName = await _sut.SaveImageAsync(imageData, ".jpg", _testDirectory);

        // Act
        var task = _sut.DeleteImageAsync(_testDirectory, fileName);

        // Assert
        task.Should().NotBeNull();
        task.IsCompleted.Should().BeTrue();
    }

    #endregion

    #endregion
}
