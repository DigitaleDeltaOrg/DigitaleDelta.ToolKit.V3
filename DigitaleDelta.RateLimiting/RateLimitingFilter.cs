// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace DigitaleDelta.RateLimiting;

/// <summary>
/// Implements a rate-limiting filter that can be applied to specific actions in the API.
/// </summary>
public class RateLimitingFilter(IOptions<Dictionary<string, RateLimitOptions>> rateLimitOptions) : IAsyncActionFilter
{
    private readonly ConcurrentDictionary<string, int>                      _concurrentRequests = new();
    private readonly ConcurrentDictionary<string, LinkedList<DateTime>>     _requestLog         = new();

    private static TimeSpan GetCutOff(string unit, int limit)
    {
        return unit switch
        {
            "s" => TimeSpan.FromSeconds(limit), 
            "m" => TimeSpan.FromMinutes(limit), 
            "h" => TimeSpan.FromHours(limit), 
            _ => TimeSpan.Zero
        };
    }

    /// <summary>
    /// Executes actions before and after the action method is invoked, implementing rate limiting for specific actions in the API.
    /// </summary>
    /// <param name="context">The context of the action execution.</param>
    /// <param name="next">The delegate representing the next action execution.</param>
    /// <returns>A task that represents the asynchronous action execution.</returns>
public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controllerName = context.Controller.GetType().Name;
        var identity = GetIdentityUsedFromHttpContext(context.HttpContext);
        var incremented = false;

        try
        {
            if (string.IsNullOrEmpty(identity))
            {
                await next().ConfigureAwait(false);
                
                return;
            }

            if (rateLimitOptions.Value.TryGetValue(controllerName, out var value))
            {
                var current = _concurrentRequests.AddOrUpdate(identity, 1, (_, count) => count + 1);
                
                incremented = true;

                var userRequests = _requestLog.GetOrAdd(identity, _ => []);
                var cutoff = DateTime.UtcNow - GetCutOff(value.Unit, value.Limit);

                context.HttpContext.Response.Headers.Append("RateLimit-Limit", $"{value.Limit}");
                context.HttpContext.Response.Headers.Append("RateLimit-Remaining", $"{value.Limit - userRequests.Count}");
                context.HttpContext.Response.Headers.Append("RateLimit-Reset", $"{(int)(DateTime.UtcNow - cutoff).TotalSeconds}");

                if (current > value.NumberOfConcurrentRequests)
                {
                    context.Result = new ContentResult
                    {
                        Content = $"Only {value.NumberOfConcurrentRequests} concurrent request is allowed.",
                        StatusCode = StatusCodes.Status503ServiceUnavailable
                    };
                    return;
                }

                TrimExpiredRequests(userRequests, cutoff);

                if (userRequests.Count >= value.Limit)
                {
                    context.Result = new ContentResult
                    {
                        Content = "Too many requests, please slow down.",
                        StatusCode = StatusCodes.Status429TooManyRequests
                    };
                    return;
                }

                userRequests.AddLast(DateTime.UtcNow);
            }

            if (context.Result == null)
            {
                await next().ConfigureAwait(false);
            }
        }
        finally
        {
            if (incremented)
            {
                _concurrentRequests.AddOrUpdate(identity, 0, (_, value) => Math.Max(value - 1, 0));
            }
        }
    }

    private static void TrimExpiredRequests(LinkedList<DateTime> userRequests, DateTime cutoff)
    {
        userRequests.Where(request => request < cutoff).ToList().ForEach(request => userRequests.Remove(request));
    }
  
    private static string GetIdentityUsedFromHttpContext(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("sub")?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                return $"{userId}/{context.Connection.RemoteIpAddress}";
            }
        
            var userName = context.User.Identity.Name;
            
            if (!string.IsNullOrEmpty(userName))
            {
                return $"{userName}/{context.Connection.RemoteIpAddress}";
            }
        }

        if (context.Request.Headers.TryGetValue("X-API-KEY", out var value))
        {
            var apiKey = value.FirstOrDefault();
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                return $"{apiKey}/{context.Connection.RemoteIpAddress}";
            }
        }

        if (context.Request.Headers.TryGetValue("API-KEY", out var value2))
        {
            var apiKey = value2.FirstOrDefault();
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                return $"{apiKey}/{context.Connection.RemoteIpAddress}";
            }
        }
        
        if (context.Connection.ClientCertificate != null)
        {
            var certSubject = context.Connection.ClientCertificate.Subject;
            
            if (!string.IsNullOrEmpty(certSubject))
            {
                return $"{certSubject}/{context.Connection.RemoteIpAddress}";
            }
        }

        if (context.Request.Headers.ContainsKey("X-Forwarded-For") || (context.Request.Headers.ContainsKey("Forwarded")))
        {
            return $"{context.Session?.Id ?? string.Empty}/{context.Connection.RemoteIpAddress}";
        }
      
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
      
        return !string.IsNullOrEmpty(ipAddress) 
            ? $"{context.Session?.Id ?? string.Empty}/{ipAddress}" 
            : $"{context.Session?.Id ?? string.Empty}";
    }
}