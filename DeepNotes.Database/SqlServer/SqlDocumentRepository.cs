namespace DeepNotes.Database.SqlServer;

using Microsoft.EntityFrameworkCore;
using DeepNotes.Core.Interfaces;
using DeepNotes.Core.Models;

public class SqlDocumentRepository : IDocumentRepository
{
    private readonly DeepNotesDbContext _context;

    public SqlDocumentRepository(DeepNotesDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentMetadata> CreateAsync(DocumentMetadata document)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        return document;
    }

    public async Task<DocumentMetadata?> GetByIdAsync(Guid id)
    {
        return await _context.Documents.FindAsync(id);
    }

    public async Task<IEnumerable<DocumentMetadata>> GetAllAsync(int skip = 0, int take = 50)
    {
        return await _context.Documents
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<DocumentMetadata> UpdateAsync(DocumentMetadata document)
    {
        document.UpdatedAt = DateTime.UtcNow;
        _context.Documents.Update(document);
        await _context.SaveChangesAsync();
        return document;
    }

    public async Task DeleteAsync(Guid id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document != null)
        {
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<DocumentMetadata>> FindByStatusAsync(ProcessingStatus status)
    {
        return await _context.Documents
            .Where(d => d.Status == status)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> UpdateStatusAsync(Guid id, ProcessingStatus status, string? errorMessage = null)
    {
        return await _context.Documents
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, status)
                .SetProperty(d => d.ErrorMessage, errorMessage)
                .SetProperty(d => d.UpdatedAt, DateTime.UtcNow));
    }
} 