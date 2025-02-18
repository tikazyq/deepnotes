namespace DeepNotes.Core.Interfaces;

using DeepNotes.Core.Models;

public interface IDocumentRepository
{
    Task<DocumentMetadata> CreateAsync(DocumentMetadata document);
    Task<DocumentMetadata?> GetByIdAsync(Guid id);
    Task<IEnumerable<DocumentMetadata>> GetAllAsync(int skip = 0, int take = 50);
    Task<DocumentMetadata> UpdateAsync(DocumentMetadata document);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<DocumentMetadata>> FindByStatusAsync(ProcessingStatus status);
    Task<int> UpdateStatusAsync(Guid id, ProcessingStatus status, string? errorMessage = null);
} 