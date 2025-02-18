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
        CreateTestPresentation(_testFilePath);
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
        var content = await handler.ExtractTextAsync(_testFilePath);

        // Assert
        Assert.Contains("Test Slide Content", content);
        Assert.Contains("Slide 1", content);
    }

    [Fact]
    public void ExtractMetadata_ValidPowerPoint_ReturnsMetadata()
    {
        // Arrange
        var handler = new PowerPointFileHandler();

        // Act
        var metadata = handler.ExtractMetadata(_testFilePath);

        // Assert
        Assert.Equal("1", metadata["SlideCount"]);
    }

    private void CreateTestPresentation(string filePath)
    {
        using var presentation = PresentationDocument.Create(filePath, DocumentFormat.OpenXml.PresentationDocumentType.Presentation);
        var presentationPart = presentation.AddPresentationPart();
        presentationPart.Presentation = new Presentation();

        var slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>();
        var slideMaster = new SlideMaster(new CommonSlideData(new ShapeTree()));
        slideMasterPart.SlideMaster = slideMaster;

        var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>();
        var slideLayout = new SlideLayout(new CommonSlideData(new ShapeTree()));
        slideLayoutPart.SlideLayout = slideLayout;

        var slidePart = presentationPart.AddNewPart<SlidePart>();
        var slide = new Slide(new CommonSlideData(new ShapeTree()));
        
        var paragraph = new A.Paragraph(new A.Run(new A.Text("Test Slide Content")));
        var shape = new Shape(new TextBody(paragraph));
        slide.CommonSlideData!.ShapeTree!.AppendChild(shape);
        
        slidePart.Slide = slide;

        presentationPart.Presentation.SlideIdList = new SlideIdList(new SlideId { Id = 256U, RelationshipId = presentationPart.GetIdOfPart(slidePart) });
    }
} 