using AISupportTriage.Models.Entities;
using AISupportTriage.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AISupportTriage.Data;

public static class SeedData
{
    public static async Task SeedKnownIssuesAsync(AppDbContext context)
    {
        // Check if data already exists
        if (await context.KnownIssues.AnyAsync())
        {
            return;
        }

        var knownIssues = new List<KnownIssue>
        {
            new()
            {
                Code = "ORD-1042",
                Title = "Database migration mismatch after deployment",
                Description = "Application deployments may require database schema changes that were not applied to the target environment.",
                Category = TicketCategory.Deployment,
                Symptoms = "HTTP 500 errors following deployment; missing table or column exceptions; InvalidColumnName errors in application logs.",
                Resolution = "Verify the production database migration history matches the deployed application version. Apply missing database migrations using the deployment pipeline or manually. Consider rollback if business operations remain blocked.",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Code = "AUTH-2001",
                Title = "Expired authentication signing certificate",
                Description = "Authentication fails when the JWT signing certificate expires without being renewed.",
                Category = TicketCategory.Authentication,
                Symptoms = "Users cannot sign in; token validation errors appear in logs; 401 Unauthorized responses from API.",
                Resolution = "Replace the expired signing certificate in the configuration store. Restart the authentication service. Verify certificate expiration monitoring is configured.",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Code = "DB-3010",
                Title = "Connection pool exhaustion",
                Description = "Database connection pool is exhausted due to connections not being properly disposed or excessive concurrent load.",
                Category = TicketCategory.Database,
                Symptoms = "Timeout errors acquiring database connections; slow application response times; PoolExhaustedException in logs.",
                Resolution = "Review application code for proper DbContext disposal. Increase connection pool size if legitimate concurrent load is high. Implement connection retry logic. Monitor connection pool metrics.",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Code = "DEPLOY-1004",
                Title = "Configuration override not applied",
                Description = "Environment-specific configuration values are not being applied during deployment, causing the application to use default or incorrect settings.",
                Category = TicketCategory.Configuration,
                Symptoms = "Application connects to wrong database; incorrect API endpoints; features behave as if in different environment.",
                Resolution = "Verify environment variables are correctly set in the deployment target. Check configuration precedence order. Ensure configuration transformations are applied during build/deployment.",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Code = "PERF-5001",
                Title = "Missing database index on frequently queried column",
                Description = "Queries become extremely slow due to missing database indexes on columns used in WHERE clauses or JOIN conditions.",
                Category = TicketCategory.Performance,
                Symptoms = "Slow page load times; database CPU spikes; query execution plans show table scans; timeout errors during high load.",
                Resolution = "Identify slow queries using database monitoring tools. Add appropriate indexes to frequently queried columns. Update statistics. Consider composite indexes for multi-column filters.",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Code = "INT-6001",
                Title = "Third-party API rate limit exceeded",
                Description = "Application exceeds the rate limit imposed by a third-party API, causing integration failures.",
                Category = TicketCategory.Integration,
                Symptoms = "429 Too Many Requests responses; integration failures during peak usage; API error messages indicating rate limit.",
                Resolution = "Implement request throttling and backoff strategy. Add caching layer for frequently requested data. Consider upgrading API tier if business needs require higher limits. Implement circuit breaker pattern.",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Code = "NET-7001",
                Title = "Firewall blocking required outbound port",
                Description = "Network firewall rules block required outbound connections from the application to external services.",
                Category = TicketCategory.Networking,
                Symptoms = "Connection timeout errors; cannot reach external APIs or services; SocketException or network unreachable errors.",
                Resolution = "Verify firewall rules allow outbound traffic on required ports. Check network security group configuration. Confirm DNS resolution works. Test connectivity using network diagnostic tools.",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Code = "APP-8001",
                Title = "Null reference exception in order processing",
                Description = "Order processing fails when optional customer reference field is not provided, due to missing null check.",
                Category = TicketCategory.ApplicationError,
                Symptoms = "NullReferenceException in logs; order submission returns HTTP 500; specific to orders without customer reference.",
                Resolution = "Add null check for optional CustomerReference field. Deploy hotfix to production. Add unit tests to prevent regression. Consider enabling nullable reference types.",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Code = "AUTHZ-9001",
                Title = "Role assignment not syncing from identity provider",
                Description = "User role assignments are not being synchronized from the external identity provider, causing authorization failures.",
                Category = TicketCategory.Authorization,
                Symptoms = "Users receive 403 Forbidden errors despite having correct permissions; role claims missing from JWT token; access denied to authorized features.",
                Resolution = "Verify identity provider group mappings are configured correctly. Force role synchronization for affected users. Check token claim mappings. Restart authorization service if cache is stale.",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Code = "DATA-1001",
                Title = "Data corruption from concurrent updates",
                Description = "Concurrent updates to the same record without proper concurrency control result in data corruption or lost updates.",
                Category = TicketCategory.Data,
                Symptoms = "Data inconsistencies; user reports changes being lost; OptimisticConcurrencyException in some cases; race condition errors.",
                Resolution = "Implement optimistic or pessimistic concurrency control. Add RowVersion/Timestamp column to affected tables. Use EF Core concurrency tokens. Review transaction isolation levels.",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Code = "DEPLOY-2001",
                Title = "Application binary missing from deployment package",
                Description = "Deployment package is incomplete, missing required assemblies or static assets due to build configuration issue.",
                Category = TicketCategory.Deployment,
                Symptoms = "Application fails to start after deployment; FileNotFoundException for specific assemblies; 404 errors for static resources.",
                Resolution = "Review build configuration to ensure all required files are included. Check .csproj file CopyToOutputDirectory settings. Verify publish profile settings. Redeploy with corrected build output.",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Code = "PERF-5002",
                Title = "Memory leak from unclosed event handlers",
                Description = "Application memory usage continuously grows due to event handlers not being properly unsubscribed, preventing garbage collection.",
                Category = TicketCategory.Performance,
                Symptoms = "Gradually increasing memory usage; OutOfMemoryException after extended runtime; application restart required periodically.",
                Resolution = "Review code for event subscription patterns. Ensure event handlers are properly unsubscribed in Dispose methods. Use weak event patterns where appropriate. Implement memory profiling to identify leak sources.",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        };

        context.KnownIssues.AddRange(knownIssues);
        await context.SaveChangesAsync();
    }
}
