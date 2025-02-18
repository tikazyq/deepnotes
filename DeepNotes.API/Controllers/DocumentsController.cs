namespace DeepNotes.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using DeepNotes.API.Models;
using DeepNotes.Core.Interfaces;
using DeepNotes.Core.Models;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IKnowledgeGraphService _graphService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentRepository documentRepository,
        IKnowledgeGraphService graphService,
        ILogger<DocumentsController> logger)
    {
        _documentRepository = documentRepository;
        _graphService = graphService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<DocumentResponse>> UploadDocument([FromBody] DocumentUploadRequest request)
    {
        try
        {
            var document = new DocumentMetadata
            {
                Source = request.Source,
                SourceType = request.SourceType,
                Title = request.Title,
                Properties = request.Properties,
                Status = ProcessingStatus.Pending
            };

            var created = await _documentRepository.CreateAsync(document);
            
            // Start processing asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await _documentRepository.UpdateStatusAsync(created.Id, ProcessingStatus.Processing);
                    
                    // Process document and update knowledge graph
                    // This would typically be handled by a background job service in production
                    var doc = new Document
                    {
                        Source = created.Source,
                        SourceType = created.SourceType,
                        // Load content based on source type
                    };
                    
                    await _graphService.ProcessDocumentsAsync(new[] { doc });
                    await _documentRepository.UpdateStatusAsync(created.Id, ProcessingStatus.Completed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing document {Id}", created.Id);
                    await _documentRepository.UpdateStatusAsync(created.Id, ProcessingStatus.Failed, ex.Message);
                }
            });

            return Ok(MapToResponse(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return StatusCode(500, "Error uploading document");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentResponse>> GetDocument(Guid id)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(document));
    }

    [HttpGet]
    public async Task<ActionResult<DocumentListResponse>> ListDocuments(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var documents = await _documentRepository.GetAllAsync(skip, take);
        var total = await _documentRepository.GetAllAsync(); // This should be optimized in production

        return Ok(new DocumentListResponse
        {
            Documents = documents.Select(MapToResponse),
            Total = total.Count(),
            Skip = skip,
            Take = take
        });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteDocument(Guid id)
    {
        await _documentRepository.DeleteAsync(id);
        return NoContent();
    }

    private static DocumentResponse MapToResponse(DocumentMetadata document)
    {
        return new DocumentResponse
        {
            Id = document.Id,
            Source = document.Source,
            SourceType = document.SourceType,
            Title = document.Title,
            Status = document.Status.ToString(),
            Properties = document.Properties,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        };
    }
} 