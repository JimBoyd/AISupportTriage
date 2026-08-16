using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using AISupportTriage.Data;
using AISupportTriage.Models.Entities;
using AISupportTriage.Models.Enums;
using AISupportTriage.Services.Interfaces;

namespace AISupportTriage.Services;

public class KnownIssueService : IKnownIssueService
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<KnownIssueService> _logger;
    private const string CacheKeyPrefix = "KnownIssue_";
    private const int CacheExpirationMinutes = 30;

    public KnownIssueService(AppDbContext context, IMemoryCache cache, ILogger<KnownIssueService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<KnownIssue>> SearchKnownIssuesAsync(string? category, string? symptoms, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching known issues: category={Category}, symptoms={Symptoms}", category, symptoms);

        var query = _context.KnownIssues.Where(k => k.IsActive);

        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<TicketCategory>(category, true, out var categoryEnum))
        {
            query = query.Where(k => k.Category == categoryEnum);
        }

        if (!string.IsNullOrWhiteSpace(symptoms))
        {
            var searchTerms = symptoms.ToLower();
            query = query.Where(k =>
                k.Title.ToLower().Contains(searchTerms) ||
                k.Description.ToLower().Contains(searchTerms) ||
                k.Symptoms.ToLower().Contains(searchTerms));
        }

        var results = await query.Take(5).ToListAsync(cancellationToken);
        _logger.LogInformation("Found {Count} matching known issues", results.Count);
        return results;
    }

    public async Task<KnownIssue?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}Code_{code}";

        if (_cache.TryGetValue(cacheKey, out KnownIssue? cached))
        {
            _logger.LogDebug("Known issue {Code} retrieved from cache", code);
            return cached;
        }

        var issue = await _context.KnownIssues
            .FirstOrDefaultAsync(k => k.Code == code, cancellationToken);

        if (issue != null)
        {
            _cache.Set(cacheKey, issue, TimeSpan.FromMinutes(CacheExpirationMinutes));
        }

        return issue;
    }

    public async Task<KnownIssue?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";

        if (_cache.TryGetValue(cacheKey, out KnownIssue? cached))
        {
            return cached;
        }

        var issue = await _context.KnownIssues.FindAsync([id], cancellationToken);

        if (issue != null)
        {
            _cache.Set(cacheKey, issue, TimeSpan.FromMinutes(CacheExpirationMinutes));
        }

        return issue;
    }

    public async Task<List<KnownIssue>> GetAllAsync(TicketCategory? category = null, bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var query = _context.KnownIssues.AsQueryable();

        if (category.HasValue)
        {
            query = query.Where(k => k.Category == category.Value);
        }

        if (activeOnly == true)
        {
            query = query.Where(k => k.IsActive);
        }

        return await query.OrderBy(k => k.Code).ToListAsync(cancellationToken);
    }

    public async Task<KnownIssue> CreateAsync(KnownIssue knownIssue, CancellationToken cancellationToken = default)
    {
        knownIssue.CreatedAtUtc = DateTime.UtcNow;
        knownIssue.UpdatedAtUtc = DateTime.UtcNow;
        knownIssue.IsActive = true;

        _context.KnownIssues.Add(knownIssue);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created known issue {Code}", knownIssue.Code);
        return knownIssue;
    }

    public async Task<KnownIssue> UpdateAsync(KnownIssue knownIssue, CancellationToken cancellationToken = default)
    {
        knownIssue.UpdatedAtUtc = DateTime.UtcNow;
        _context.KnownIssues.Update(knownIssue);
        await _context.SaveChangesAsync(cancellationToken);

        InvalidateCache(knownIssue.Id, knownIssue.Code);
        _logger.LogInformation("Updated known issue {Code}", knownIssue.Code);
        return knownIssue;
    }

    public async Task DisableAsync(int id, CancellationToken cancellationToken = default)
    {
        var issue = await _context.KnownIssues.FindAsync([id], cancellationToken);
        if (issue == null) return;

        issue.IsActive = false;
        issue.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        InvalidateCache(issue.Id, issue.Code);
        _logger.LogInformation("Disabled known issue {Code}", issue.Code);
    }

    private void InvalidateCache(int id, string code)
    {
        _cache.Remove($"{CacheKeyPrefix}{id}");
        _cache.Remove($"{CacheKeyPrefix}Code_{code}");
    }
}
