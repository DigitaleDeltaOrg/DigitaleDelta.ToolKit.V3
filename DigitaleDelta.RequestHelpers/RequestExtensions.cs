// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using DigitaleDelta.Contracts;
using DigitaleDelta.ErrorHandling;
using DigitaleDelta.ODataTranslator;
using DigitaleDelta.ODataTranslator.Helpers;
using DigitaleDelta.Contracts.Configuration;
using Microsoft.AspNetCore.Http;

namespace DigitaleDelta.RequestHelpers;

public static class RequestExtensions
{
    private const string omitValuesConstant = "omit-values=nulls";
    private const string referenceAppliedConstant = "Preference-Applied";
    private const string preferHeaderConstant = "Prefer";

    /// <param name="request">The HTTP request.</param>
    extension(HttpRequest request)
    {
        /// <summary>
        /// Determines whether the request should omit null values in the response.
        /// </summary>
        /// <returns>True if the request should omit null values, otherwise false.</returns>
        private bool ShouldOmitNullValues()
        {
            var preferHeader = request.Headers.TryGetValue(preferHeaderConstant, out var values)
                ? values.FirstOrDefault()
                : null;

            return preferHeader == null || preferHeader.Contains(omitValuesConstant, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Sets the Preference-Applied response header based on the provided HttpRequest.
        /// </summary>
        public void SetPreferenceAppliedResponseHeader()
        {
            var response = request.HttpContext.Response;

            if (response.Headers.TryGetValue(referenceAppliedConstant, out var values))
            {
                var preferApplied = values.FirstOrDefault();

                if (preferApplied?.Contains(omitValuesConstant) == false)
                {
                    response.Headers[referenceAppliedConstant] += $",{omitValuesConstant}";
                }
            }
            else
            {
                response.Headers[referenceAppliedConstant] = omitValuesConstant;
            }
        }

        public string GetUrl()
        {
            return $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
        }

        public string GetBaseUrl()
        {
            return $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}";
        }

        /// <summary>
        /// Converts the HTTP request query parameters into a structured <see cref="ODataQueryOptions"/> object.
        /// </summary>
        /// <param name="propertyMaps"></param>
        /// <param name="functionMaps">A collection of OData function mappings for validation purposes.</param>
        /// <param name="defaultTop">The default maximum number of entities to return if $top is not specified.</param>
        /// <param name="maxTop">The maximum allowable value for $top to ensure query safety.</param>
        /// <returns>A populated <see cref="ODataQueryOptions"/> object based on the HTTP request query parameters.</returns>
        /// <exception cref="ODataValidationException">Thrown when query validation fails or contains unsupported parameters.</exception>
        public ODataQueryOptions ToODataQueryOptions(Dictionary<string, ODataToSqlMap> propertyMaps, Dictionary<string, ODataFunctionMap> functionMaps, int defaultTop, int maxTop)
        {
            var options = new ODataQueryOptions
            {
                Url = request.GetUrl()
            };

            request.Query.TryGetValue("$filter", out var filterValue);

            var filterString = filterValue.FirstOrDefault() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(filterString))
            {
                var wrapped = $"$filter={filterString}";

                if (!ODataFilter.TryParse(wrapped, out var filter, out var error) || filter == null)
                {
                    throw new ODataValidationException("Filter error", new Exception(error), "Invalid filter.");
                }

                var validator = new ODataFilterValidator(propertyMaps, functionMaps);

                if (!validator.TryValidate(filter.Context, out var errors))
                {
                    throw new ODataValidationException("Filter error", new Exception(error), string.Join("; ", errors));
                }

                options.Filter = filter.Context;
            }

            if (request.Query.TryGetValue("$orderby", out var _))
            {
                throw new ODataValidationException("Filter error", new Exception("$orderby is not supported."), "$orderby is not supported.");
            }

            request.Query.TryGetValue("$top", out var topValue);

            if (int.TryParse(topValue.FirstOrDefault(), out var top))
            {
                options.RequestedTop = top;

                if (top < 1 || top > maxTop)
                {
                    throw new ODataValidationException("Parameter error", new Exception(ErrorMessages.topOutOfRange), $"1 - {maxTop}");
                }

                options.Top = top;
            }
            else
            {
                options.Top = defaultTop;
            }

            request.Query.TryGetValue("$select", out var selectValue);
            options.Select = selectValue.ToArray();
            request.Query.TryGetValue("$count", out var countValue);

            if (bool.TryParse(countValue.FirstOrDefault(), out var count))
            {
                options.Count = count;
            }

            request.Query.TryGetValue("$skiptoken", out var skipTokenValue);

            options.SkipToken = skipTokenValue;
            options.OmitNulls = request.ShouldOmitNullValues();

            var crs = HandleCrsRequest(request);

            options.CrsId = crs.crsId;
            HandleCrsResponse(request, request.HttpContext.Response);
            request.SetPreferenceAppliedResponseHeader();
            options.ClaimsPrincipal = request.HttpContext.User;

            return options;
        }
    }

    /// <summary>
    /// Handles the CRS request by extracting the CRS name and ID from the request headers.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>A tuple containing the CRS name and ID.</returns>
    /// <exception cref="HttpRequestException">Thrown if the request does not contain the "Content-Crs" header or the CRS is invalid.</exception>
    private static (string? crsName, int? crsId) HandleCrsRequest(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Accept-Crs", out var value))
        {
            return (null, 4258);
        }

        var crs = CrsHelper.ValidateContentCrs(value);

        return crs == null
            ? throw new HttpRequestException("Accept-Crs", null, HttpStatusCode.PreconditionFailed)
            : (value, crs);
    }

    /// <summary>
    /// Handles the CRS response by appending the "Accept-Crs" header to the response.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <param name="response">The HTTP response.</param>
    private static void HandleCrsResponse(HttpRequest request, HttpResponse response)
    {
        if (!request.Headers.TryGetValue("Accept-Crs", out var value))
        {
            return;
        }

        response.Headers.Append("Content-Crs", value);
    }

    public class QueryToHeaderMiddleware(RequestDelegate next)
    {
        /// <summary>
        /// Add 'prefer' and 'content-crs' to the headers if they are provided via the query.
        /// </summary>
        /// <param name="httpContext">Current context</param>
        public Task Invoke(HttpContext httpContext)
        {
            TranslateQueryIntoHeader(httpContext, "prefer", "prefer");
            TranslateQueryIntoHeader(httpContext, "content-crs", "Content-Crs");

            return next(httpContext);
        }

        /// <summary>
        /// Translate query into header.
        /// </summary>
        /// <param name="httpContext">Current context</param>
        /// <param name="query">Query parameter name</param>
        /// <param name="header">Header parameter name</param>
        private static void TranslateQueryIntoHeader(HttpContext httpContext, string query, string header)
        {
            var queryParameter = httpContext.Request.Query[query].ToString();

            if (!string.IsNullOrEmpty(queryParameter))
            {
                httpContext.Request.Headers.Append(header, queryParameter);
            }
        }
    }
}
