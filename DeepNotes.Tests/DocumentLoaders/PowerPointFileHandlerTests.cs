namespace DeepNotes.Tests.DocumentLoaders;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DeepNotes.DataLoaders.FileHandlers;
using A = DocumentFormat.OpenXml.Drawing;
using Xunit;

public class PowerPointFileHandlerTests : IDisposable
{
    private readonly string _testFilePath;

    public PowerPointFileHandlerTests()
    {
        _testFilePath = Path.GetTempFileName() + ".pptx";
        CreateTestPresentation(_testFilePath, "Test PowerPoint content");
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public async Task ExtractTextAsync_ValidPowerPoint_ReturnsContent()
    {
        // Arrange
        var handler = new PowerPointFileHandler();

        // Act
        var document = await handler.LoadDocumentAsync(_testFilePath);

        // Assert
        Assert.Contains("Test Slide Content", document.Content);
        Assert.Contains("Slide 1", document.Content);
    }

    [Fact]
    public async Task LoadDocumentAsync_ValidPowerPoint_ExtractsContent()
    {
        // Arrange
        var handler = new PowerPointFileHandler();

        // Act
        var document = await handler.LoadDocumentAsync(_testFilePath);

        // Assert
        Assert.NotNull(document);
        Assert.Contains("Test PowerPoint content", document.Content);
        Assert.Equal(_testFilePath, document.Source);
        Assert.Equal("File", document.SourceType);
    }

    [Fact]
    public async Task LoadDocumentAsync_WithMultipleSlides_ExtractsAllContent()
    {
        // Arrange
        var handler = new PowerPointFileHandler();
        CreateMultiSlidePresentation(_testFilePath);

        // Act
        var document = await handler.LoadDocumentAsync(_testFilePath);

        // Assert
        Assert.NotNull(document);
        Assert.Contains("Slide 1", document.Content);
        Assert.Contains("Slide 2", document.Content);
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

    [Fact]
    public async Task LoadDocument_PowerPointWithShapes_ExtractsShapeText()
    {
        // Arrange
        var handler = new PowerPointFileHandler();
        var testFilePath = "TestFiles/presentation-with-shapes.pptx";
        
        // Act
        var result = await handler.LoadDocumentAsync(testFilePath);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Content, "SmartArt diagram content");
        Assert.Contains(result.Content, "Text box content");
        Assert.Contains(result.Content, "Table cell content");
    }

    [Fact]
    public async Task LoadDocument_PowerPointWithNotes_ExtractsNotes()
    {
        // Arrange
        var handler = new PowerPointFileHandler();
        var testFilePath = "TestFiles/presentation-with-notes.pptx";
        
        // Act
        var document = await handler.LoadDocumentAsync(testFilePath);

        // Assert
        Assert.NotNull(document);
        Assert.Contains(document.Content, "Speaker notes for slide 1");
        Assert.Contains(document.Content, "Additional talking points");
    }

    private void CreateTestPresentation(string filePath, string content)
    {
        using var presentation = PresentationDocument.Create(filePath, DocumentFormat.OpenXml.PresentationDocumentType.Presentation);
        var presentationPart = presentation.AddPresentationPart();
        presentationPart.Presentation = new Presentation();
        
        var slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.Slide = new Slide(new CommonSlideData(new ShapeTree()));
        
        // Add text to slide
        var paragraph = new DocumentFormat.OpenXml.Drawing.Paragraph(
            new DocumentFormat.OpenXml.Drawing.Run(
                new DocumentFormat.OpenXml.Drawing.Text { Text = content }
            )
        );
        
        var shape = slidePart.Slide.CommonSlideData.ShapeTree.AppendChild(new Shape());
        shape.TextBody = new TextBody(paragraph);
    }

    private void CreateMultiSlidePresentation(string filePath)
    {
        using var presentation = PresentationDocument.Create(filePath, DocumentFormat.OpenXml.PresentationDocumentType.Presentation);
        var presentationPart = presentation.AddPresentationPart();
        presentationPart.Presentation = new Presentation();

        // Create two slides
        for (int i = 1; i <= 2; i++)
        {
            var slidePart = presentationPart.AddNewPart<SlidePart>();
            slidePart.Slide = new Slide(new CommonSlideData(new ShapeTree()));
            
            var paragraph = new DocumentFormat.OpenXml.Drawing.Paragraph(
                new DocumentFormat.OpenXml.Drawing.Run(
                    new DocumentFormat.OpenXml.Drawing.Text { Text = $"Slide {i} content" }
                )
            );
            
            var shape = slidePart.Slide.CommonSlideData.ShapeTree.AppendChild(new Shape());
            shape.TextBody = new TextBody(paragraph);
        }
    }
} 