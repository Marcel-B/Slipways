using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace com.b_velop.Slipways.Web.Middlewares
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class FacebookMiddleware
    {
        private readonly ILogger<FacebookMiddleware> _logger;
        private readonly RequestDelegate _next;

        public FacebookMiddleware(
            ILogger<FacebookMiddleware> logger,
            RequestDelegate next)
        {
            _logger = logger;
            _next = next;
        }

        public Task Invoke(
            HttpContext httpContext)
        {
            if (httpContext.Request.Host.Value.Contains("facebook"))
            {
                var queryString = new QueryString(httpContext.Request.QueryString.Value.Replace("http", "https"));
                _logger.LogInformation(2233, queryString.Value);
                httpContext.Request.QueryString = queryString;
                httpContext.Request.Scheme = "https";
            }
            return _next(httpContext);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class FacebookMiddlewareExtensions
    {
        public static IApplicationBuilder UseFacebookMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<FacebookMiddleware>();
        }
    }
}
