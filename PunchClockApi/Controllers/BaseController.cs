using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace PunchClockApi.Controllers;

/// <summary>
/// Query options parsed from request query string parameters.
/// </summary>
public sealed class QueryOptions
{
    public IDictionary<string, object?>? Where { get; set; }
    public List<SortOption>? OrderBy { get; set; }
    public int? Page { get; set; }
    public int? Limit { get; set; }
    public int? Skip { get; set; }
    public int? Take { get; set; }
    public string? Sort { get; set; }
    public string? Order { get; set; }
    public IDictionary<string, bool>? Select { get; set; }
    public IDictionary<string, object>? Include { get; set; }
}

/// <summary>
/// Represents a sorting option with field name and direction.
/// </summary>
public sealed record SortOption(string Field, string Direction);

/// <summary>
/// Provides shared query parsing and error handling helpers for concrete API controllers.
/// </summary>
public abstract class BaseController<TEntity> : ControllerBase where TEntity : class
{
    private static readonly HashSet<string> ReservedQueryKeys =
        new(["page", "limit", "sort", "order", "select", "include"], StringComparer.OrdinalIgnoreCase);

    protected BaseController(ILogger logger) => Logger = logger;

    protected ILogger Logger { get; }

    /// <summary>
    /// Applies QueryOptions to an IQueryable for pagination, sorting, filtering, and includes.
    /// </summary>
    protected static IQueryable<T> ApplyQueryOptions<T>(IQueryable<T> query, QueryOptions options) where T : class
    {
        // Apply includes (eager loading)
        if (options.Include is not null)
        {
            query = ApplyIncludes(query, options.Include);
        }

        // Apply where filters (basic string contains for now)
        if (options.Where is not null && options.Where.Count > 0)
        {
            query = ApplyFilters(query, options.Where);
        }

        // Apply sorting
        if (options.OrderBy is not null && options.OrderBy.Count > 0)
        {
            query = ApplySorting(query, options.OrderBy);
        }

        // Apply pagination
        if (options.Skip.HasValue)
        {
            query = query.Skip(options.Skip.Value);
        }

        if (options.Take.HasValue)
        {
            query = query.Take(options.Take.Value);
        }

        return query;
    }

    private static IQueryable<T> ApplyIncludes<T>(IQueryable<T> query, IDictionary<string, object> includes) where T : class
    {
        foreach (var include in includes)
        {
            var path = include.Key;
            
            // Handle nested includes
            if (include.Value is IDictionary<string, object> nested && nested.TryGetValue("include", out var nestedIncludes))
            {
                if (nestedIncludes is IDictionary<string, object> nestedDict)
                {
                    foreach (var nestedInclude in nestedDict)
                    {
                        var nestedPath = $"{path}.{nestedInclude.Key}";
                        query = query.Include(nestedPath);
                    }
                }
            }
            else
            {
                query = query.Include(path);
            }
        }

        return query;
    }

    private static IQueryable<T> ApplyFilters<T>(IQueryable<T> query, IDictionary<string, object?> filters) where T : class
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combinedExpression = null;

        foreach (var filter in filters)
        {
            var propertyName = filter.Key;
            var filterValue = filter.Value;

            if (filterValue is not IDictionary<string, object?> filterOp)
                continue;

            // Build property access expression
            var propertyInfo = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo is null)
                continue;

            var property = Expression.Property(parameter, propertyInfo);

            // Handle "contains" operator for string properties
            if (filterOp.TryGetValue("contains", out var containsValue) && containsValue is string stringValue)
            {
                if (propertyInfo.PropertyType == typeof(string))
                {
                    var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
                    var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

                    if (toLowerMethod is not null && containsMethod is not null)
                    {
                        var propertyToLower = Expression.Call(property, toLowerMethod);
                        var searchValue = Expression.Constant(stringValue.ToLower());
                        var containsCall = Expression.Call(propertyToLower, containsMethod, searchValue);

                        combinedExpression = combinedExpression is null
                            ? containsCall
                            : Expression.AndAlso(combinedExpression, containsCall);
                    }
                }
            }
        }

        if (combinedExpression is not null)
        {
            var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    private static IQueryable<T> ApplySorting<T>(IQueryable<T> query, List<SortOption> sortOptions) where T : class
    {
        IOrderedQueryable<T>? orderedQuery = null;

        foreach (var (index, sortOption) in sortOptions.Select((s, i) => (i, s)))
        {
            var propertyInfo = typeof(T).GetProperty(sortOption.Field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo is null)
                continue;

            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propertyInfo);
            var lambda = Expression.Lambda(property, parameter);

            var methodName = index == 0
                ? (sortOption.Direction.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "OrderByDescending" : "OrderBy")
                : (sortOption.Direction.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "ThenByDescending" : "ThenBy");

            var method = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), propertyInfo.PropertyType);

            orderedQuery = (IOrderedQueryable<T>)method.Invoke(null, new object[] { orderedQuery ?? query, lambda })!;
        }

        return orderedQuery ?? query;
    }

    protected QueryOptions ParseQuery(IQueryCollection query)
    {
        var options = new QueryOptions();
        var whereClause = new Dictionary<string, object?>();
        Dictionary<string, object>? includeObject = null;

        foreach (var parameter in query)
        {
            var key = parameter.Key;
            var value = parameter.Value.Count > 0 ? parameter.Value[0] : null;
            if (string.IsNullOrWhiteSpace(key)) continue;

            switch (key.ToLowerInvariant())
            {
                case "page":
                    if (TryParseInt(value, out var page)) options.Page = Math.Max(1, page);
                    continue;
                case "limit":
                    if (TryParseInt(value, out var limit)) options.Limit = Math.Max(1, limit);
                    continue;
                case "sort":
                    options.Sort = value;
                    continue;
                case "order":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        var normalized = value.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
                        options.Order = normalized;
                    }
                    continue;
                case "select":
                    options.Select = ParseSelect(value);
                    continue;
                case "include":
                    includeObject ??= new Dictionary<string, object>();
                    ParseInclude(includeObject, value);
                    continue;
            }

            if (ReservedQueryKeys.Contains(key)) continue;

            if (!string.IsNullOrEmpty(value))
            {
                AddFilter(whereClause, key, value);
            }
        }

        if (whereClause.Count > 0)
        {
            options.Where = whereClause;
        }

        if (options.Sort is not null)
        {
            var direction = options.Order ?? "asc";
            options.OrderBy = new List<SortOption> { new(options.Sort, direction) };
        }

        if (options.Page.HasValue && options.Limit.HasValue)
        {
            var validPage = Math.Max(1, options.Page.Value);
            var limit = Math.Max(1, options.Limit.Value);
            options.Skip = (validPage - 1) * limit;
            options.Take = limit;
        }
        else if (options.Limit.HasValue)
        {
            options.Take = Math.Max(1, options.Limit.Value);
        }

        if (includeObject is not null && includeObject.Count > 0)
        {
            options.Include = includeObject;
        }

        return options;
    }

    protected IActionResult HandleError(Exception error)
    {
        Logger.LogError(error, "Controller Error");

        return error switch
        {
            UnauthorizedAccessException => Unauthorized(new { success = false, error = "User not authenticated" }),
            KeyNotFoundException => NotFound(new { success = false, error = "Record not found" }),
            ValidationException validation => BadRequest(new
            {
                success = false,
                error = "Validation failed",
                details = validation.ValidationResult?.MemberNames ?? Array.Empty<string>()
            }),
            DuplicateNameException duplicate => Conflict(new
            {
                success = false,
                error = duplicate.Message
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                error = "Internal server error"
            })
        };
    }

    protected string? GetUserIdClaim(params string[] claimTypes)
    {
        if (User?.Identity?.IsAuthenticated is not true) return null;

        if (claimTypes.Length == 0)
        {
            claimTypes = new[] { ClaimTypes.NameIdentifier, "sub", "uid" };
        }

        foreach (var claim in claimTypes)
        {
            var value = User.FindFirstValue(claim);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return null;
    }

    protected Guid? GetUserId()
    {
        var userIdString = GetUserIdClaim();
        if (string.IsNullOrWhiteSpace(userIdString)) return null;
        
        if (Guid.TryParse(userIdString, out var userId))
        {
            return userId;
        }
        
        return null;
    }

    private static bool TryParseInt(string? input, out int value) => int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    private static Dictionary<string, bool>? ParseSelect(string? select)
    {
        if (string.IsNullOrWhiteSpace(select)) return null;

        var fields = select.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (fields.Length == 0) return null;

        var selection = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            if (!selection.ContainsKey(field)) selection[field] = true;
        }

        return selection;
    }

    private static void ParseInclude(IDictionary<string, object> includeObj, string? includeValue)
    {
        if (string.IsNullOrWhiteSpace(includeValue)) return;

        var includes = includeValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var raw in includes)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.EndsWith(".", StringComparison.Ordinal)) continue;

            var clean = raw.Trim('.');
            if (string.IsNullOrWhiteSpace(clean)) continue;

            var parts = clean.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0) continue;

            var current = includeObj;
            for (var index = 0; index < parts.Length; index++)
            {
                var part = parts[index];
                var isLast = index == parts.Length - 1;

                if (!current.TryGetValue(part, out var existing))
                {
                    if (isLast)
                    {
                        current[part] = true;
                    }
                    else
                    {
                        var next = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        current[part] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["include"] = next
                        };
                        current = next;
                    }
                }
                else if (!isLast)
                {
                    if (existing is bool)
                    {
                        var next = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        current[part] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["include"] = next
                        };
                        current = next;
                    }
                    else if (existing is IDictionary<string, object> nested)
                    {
                        if (!nested.TryGetValue("include", out var child) || child is not IDictionary<string, object> childDict)
                        {
                            childDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                            nested["include"] = childDict;
                        }

                        current = (IDictionary<string, object>)childDict;
                    }
                }
            }
        }
    }

    private static void AddFilter(IDictionary<string, object?> whereClause, string key, string value)
    {
        var keys = key.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (keys.Length == 0) return;

        IDictionary<string, object?> current = whereClause;
        for (var index = 0; index < keys.Length; index++)
        {
            var segment = keys[index];
            var isLast = index == keys.Length - 1;

            if (isLast)
            {
                current[segment] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["contains"] = value
                };
            }
            else
            {
                if (!current.TryGetValue(segment, out var next) || next is not IDictionary<string, object?> nextDict)
                {
                    nextDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    current[segment] = nextDict;
                }

                current = nextDict;
            }
        }
    }
}
