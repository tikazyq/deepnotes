using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DeepNotes.DataLoaders.FileHandlers;
using DocumentFormat.OpenXml.Drawing;
using Xunit;
using Path = System.IO.Path;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;
using Text = DocumentFormat.OpenXml.Math.Text;
using TextBody = DocumentFormat.OpenXml.Presentation.TextBody;

namespace DeepNotes.Tests.DocumentLoaders;

public class PowerPointFileHandlerTests
{
    [Fact]
    public async Task LoadDocumentAsync_ValidPowerPoint_ExtractsContent()
    {
        // Arrange
        var handler = new PowerPointFileHandler();
        var testFilePath = Path.GetTempFileName() + ".pptx";
        CreateTestPresentation(testFilePath, "Test PowerPoint content");

        // Act
        var document = await handler.LoadDocumentAsync(testFilePath);

        // Assert
        Assert.NotNull(document);
        Assert.Contains("Test PowerPoint content", document.Content);
        Assert.Equal(testFilePath, document.Source);
        Assert.Equal("File", document.SourceType);
    }

    [Fact]
    public async Task LoadDocumentAsync_WithMultipleSlides_ExtractsAllContent()
    {
        // Arrange
        var handler = new PowerPointFileHandler();
        var testFilePath = Path.GetTempFileName() + ".pptx";
        CreateMultiSlidePresentation(testFilePath);

        // Act
        var document = await handler.LoadDocumentAsync(testFilePath);

        // Assert
        Assert.NotNull(document);
        Assert.Contains("Slide 1", document.Content);
        Assert.Contains("Slide 2", document.Content);
        Assert.Contains("Speaker notes for slide 1", document.Content);
        Assert.Contains("Additional talking points", document.Content);
        Assert.Contains("SlideCount", document.Metadata.Keys);
    }

    [Theory]
    [InlineData(".pptx")]
    [InlineData(".ppt")]
    public void CanHandle_SupportedExtensions_ReturnsTrue(string extension)
    {
        // Arrange
        var handler = new PowerPointFileHandler();

        // Act
        var result = handler.CanHandle(extension);

        // Assert
        Assert.True(result);
    }

    private void CreateTestPresentation(string filePath, string content)
    {
        using var presentation =
            PresentationDocument.Create(filePath, PresentationDocumentType.Presentation);
        var presentationPart = presentation.AddPresentationPart();
        presentationPart.Presentation = new Presentation();

        var slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.Slide = new Slide(
            new CommonSlideData(
                new ShapeTree(
                    new Shape(
                        new TextBody(
                            new Paragraph(
                                new Run(
                                    new Text { Text = content }
                                )
                            )
                        )
                    )
                )
            )
        );

        presentation.Save();
    }

    private void CreateMultiSlidePresentation(string filePath)
    {
        using var presentation =
            PresentationDocument.Create(filePath, PresentationDocumentType.Presentation);
        var presentationPart = presentation.AddPresentationPart();
        presentationPart.Presentation = new Presentation();

        var texts = new[]
        {
            "Speaker notes for slide 1",
            "Additional talking points",
        };

        // Create two slides
        foreach (var text in texts)
        {
            var slidePart = presentationPart.AddNewPart<SlidePart>();
            slidePart.Slide = new Slide(
                new CommonSlideData(
                    new ShapeTree(
                        new Shape(
                            new TextBody(
                                new Paragraph(
                                    new Run(
                                        new Text { Text = text }
                                    )
                                )
                            )
                        )
                    )
                )
            );
        }

        presentation.Save();
    }
}