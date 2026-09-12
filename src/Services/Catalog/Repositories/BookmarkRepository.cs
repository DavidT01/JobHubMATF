using Catalog.Data;
using Catalog.Entities;
using MongoDB.Driver;

namespace Catalog.Repositories;

public class BookmarkRepository : IBookmarkRepository
{
    private readonly ICatalogContext _context;

    public BookmarkRepository(ICatalogContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<Bookmark>> GetByUserIdAsync(string userId)
    {
        return await _context.Bookmarks.Find(b => b.UserId == userId).ToListAsync();
    }

    public async Task<bool> IsBookmarkedAsync(string userId, string jobId)
    {
        return await _context.Bookmarks
            .Find(b => b.UserId == userId && b.JobId == jobId)
            .AnyAsync();
    }

    public async Task AddAsync(string userId, string jobId)
    {
        var bookmark = new Bookmark { UserId = userId, JobId = jobId };
        await _context.Bookmarks.InsertOneAsync(bookmark);
    }

    public async Task<bool> RemoveAsync(string userId, string jobId)
    {
        var result = await _context.Bookmarks.DeleteOneAsync(b => b.UserId == userId && b.JobId == jobId);
        return result.DeletedCount > 0;
    }
}