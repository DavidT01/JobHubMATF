using Catalog.Entities;

namespace Catalog.Repositories;

public interface IBookmarkRepository
{
    Task<IEnumerable<Bookmark>> GetByUserIdAsync(string userId);
    Task<bool> IsBookmarkedAsync(string userId, string jobId);
    Task AddAsync(string userId, string jobId);
    Task<bool> RemoveAsync(string userId, string jobId);
}