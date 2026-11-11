using FluentAssertions;
using HomeAffairs.Domain.Documents;

namespace HomeAffairs.UnitTests.Documents;

public sealed class DocumentTests
{
    [Theory]
    [InlineData(".pdf")]
    [InlineData(".docx")]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".png")]
    public void Supported_extension_should_be_allowed(
        string extension)
    {
        SupportedDocumentFormats.Extensions
            .Should()
            .Contain(extension);
    }

    [Fact]
    public void Executable_extension_should_not_be_allowed()
    {
        SupportedDocumentFormats.Extensions
            .Should()
            .NotContain(".exe");
    }

    [Fact]
    public void Maximum_size_should_be_15_megabytes()
    {
        SupportedDocumentFormats
            .MaximumFileSizeBytes
            .Should()
            .Be(15 * 1024 * 1024);
    }
}
