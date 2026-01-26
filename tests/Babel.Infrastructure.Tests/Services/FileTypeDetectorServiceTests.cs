using Babel.Domain.Enums;
using Babel.Infrastructure.Services;

namespace Babel.Infrastructure.Tests.Services;

public class FileTypeDetectorServiceTests
{
    private readonly FileTypeDetectorService _detector = new();

    [Theory]
    [InlineData("document.txt", FileExtensionType.TextBased)]
    [InlineData("readme.md", FileExtensionType.TextBased)]
    [InlineData("data.json", FileExtensionType.TextBased)]
    [InlineData("config.xml", FileExtensionType.TextBased)]
    [InlineData("log.csv", FileExtensionType.TextBased)]
    public void DetectFileType_TextBasedFiles_ShouldReturnTextBased(string fileName, FileExtensionType expected)
    {
        var result = _detector.DetectFileType(fileName);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("photo.jpg", FileExtensionType.ImageBased)]
    [InlineData("image.jpeg", FileExtensionType.ImageBased)]
    [InlineData("screenshot.png", FileExtensionType.ImageBased)]
    [InlineData("scan.tiff", FileExtensionType.ImageBased)]
    [InlineData("icon.bmp", FileExtensionType.ImageBased)]
    public void DetectFileType_ImageBasedFiles_ShouldReturnImageBased(string fileName, FileExtensionType expected)
    {
        var result = _detector.DetectFileType(fileName);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("report.pdf", FileExtensionType.Pdf)]
    public void DetectFileType_PdfFiles_ShouldReturnPdf(string fileName, FileExtensionType expected)
    {
        var result = _detector.DetectFileType(fileName);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("document.docx", FileExtensionType.OfficeDocument)]
    [InlineData("document.doc", FileExtensionType.OfficeDocument)]
    [InlineData("spreadsheet.xlsx", FileExtensionType.OfficeDocument)]
    [InlineData("presentation.pptx", FileExtensionType.OfficeDocument)]
    public void DetectFileType_OfficeFiles_ShouldReturnOfficeDocument(string fileName, FileExtensionType expected)
    {
        var result = _detector.DetectFileType(fileName);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("unknown.xyz")]
    [InlineData("file.exe")]
    [InlineData("archive.zip")]
    [InlineData("")]
    [InlineData(null)]
    public void DetectFileType_UnknownOrInvalidFiles_ShouldReturnUnknown(string? fileName)
    {
        var result = _detector.DetectFileType(fileName!);
        result.Should().Be(FileExtensionType.Unknown);
    }

    [Theory]
    [InlineData(FileExtensionType.ImageBased, true)]
    [InlineData(FileExtensionType.Pdf, true)]
    [InlineData(FileExtensionType.TextBased, false)]
    [InlineData(FileExtensionType.OfficeDocument, false)]
    [InlineData(FileExtensionType.Unknown, false)]
    public void RequiresOcr_ShouldReturnCorrectValue(FileExtensionType fileType, bool expected)
    {
        var result = _detector.RequiresOcr(fileType);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("file.txt", "text/plain")]
    [InlineData("file.json", "application/json")]
    [InlineData("file.pdf", "application/pdf")]
    [InlineData("file.jpg", "image/jpeg")]
    [InlineData("file.png", "image/png")]
    [InlineData("file.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public void GetMimeType_ShouldReturnCorrectMimeType(string fileName, string expected)
    {
        var result = _detector.GetMimeType(fileName);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("file.xyz", "application/octet-stream")]
    [InlineData("", "application/octet-stream")]
    public void GetMimeType_UnknownExtension_ShouldReturnOctetStream(string fileName, string expected)
    {
        var result = _detector.GetMimeType(fileName);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("file.txt", true)]
    [InlineData("file.pdf", true)]
    [InlineData("file.docx", true)]
    [InlineData("file.jpg", true)]
    public void IsSupported_SupportedFiles_ShouldReturnTrue(string fileName, bool expected)
    {
        var result = _detector.IsSupported(fileName);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("file.exe", false)]
    [InlineData("file.xyz", false)]
    [InlineData("", false)]
    public void IsSupported_UnsupportedFiles_ShouldReturnFalse(string fileName, bool expected)
    {
        var result = _detector.IsSupported(fileName);
        result.Should().Be(expected);
    }

    [Fact]
    public void DetectFileType_CaseInsensitive_ShouldWork()
    {
        _detector.DetectFileType("FILE.TXT").Should().Be(FileExtensionType.TextBased);
        _detector.DetectFileType("Document.PDF").Should().Be(FileExtensionType.Pdf);
        _detector.DetectFileType("Image.PNG").Should().Be(FileExtensionType.ImageBased);
    }
}
