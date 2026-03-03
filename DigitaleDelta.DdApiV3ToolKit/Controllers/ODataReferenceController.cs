// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using DigitaleDelta.Contracts;
using DigitaleDelta.DdApiV3ToolKit.Configuration;
using DigitaleDelta.ODataTranslator;
using DigitaleDelta.QueryService;
using DigitaleDelta.RequestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace DigitaleDelta.DdApiV3ToolKit.Controllers;

/// <summary>
/// Handles OData reference-related requests, providing data access and processing
/// capabilities through OData query options and backend services. Supports rate limiting
/// and integrates with various backend configurations and dependencies.
/// </summary>
/// <remarks>
/// This controller is exposed at the route "v3/odata/references" and is responsible for
/// processing OData requests using query services and a CSDL model. It also implements
/// caching, logging, and authorisation mechanisms.
/// </remarks>
[Route("v3/odata/references")]
[ApiController]
[EnableRateLimiting("ReferenceController")]
public class ODataReferenceController : ControllerBase
{
    private readonly ODataQueryServiceParameters _oDataQueryServiceParameters;
    private readonly ILogger<ODataReferenceController> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IRequestLogger _requestLogger;
    private readonly IAuthorization _authorizationService;
    private readonly BaseParameters _baseParameters;

    /// <summary>
    /// Represents an API controller that handles OData references for the v3 endpoint.
    /// Provides functionality to process OData reference queries using a configured set of parameters
    /// and services.
    /// API Route: "v3/odata/references"
    /// Rate Limiting: Enabled via "ReferenceController" policy
    /// </summary>
    public ODataReferenceController(BaseParameters baseParameters, [FromKeyedServices("ReferenceQueryServiceParameters")] ODataQueryServiceParameters oDataQueryServiceParameters, ILogger<ODataReferenceController> logger, IMemoryCache memoryCache, IRequestLogger requestLogger, IAuthorization authorizationService)
    {
        ArgumentNullException.ThrowIfNull(oDataQueryServiceParameters);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(memoryCache);
        ArgumentNullException.ThrowIfNull(requestLogger);
        ArgumentNullException.ThrowIfNull(authorizationService);

        _oDataQueryServiceParameters = oDataQueryServiceParameters;
        _logger = logger;
        _memoryCache = memoryCache;
        _requestLogger = requestLogger;
        _authorizationService = authorizationService;
        _baseParameters = baseParameters;
    }

    /// <summary>
    /// Processes an OData GET query request and retrieves the appropriate results based on the provided query parameters,
    /// service configurations, and authorisation.
    /// Constructs OData query options, executes the query using the QueryService, and returns either the result
    /// as an IActionResult or a BadRequest response if the processing fails.
    /// </summary>
    /// <returns>An IActionResult containing the OData query result or a BadRequest response on failure.</returns>
    public async Task<IActionResult> Get()
    {
        var oDataQueryOptions = Request.ToODataQueryOptions(_oDataQueryServiceParameters.PropertyMap, _oDataQueryServiceParameters.FunctionMap, _oDataQueryServiceParameters.DefaultPageSize, _oDataQueryServiceParameters.MaxPageSize);
        var queryService = new DigitaleDelta.QueryService.QueryService(_oDataQueryServiceParameters, _logger, _memoryCache, _requestLogger, _authorizationService);
        var response = await queryService.ProcessRequestAsync(Request.HttpContext, oDataQueryOptions, _baseParameters.ParameterPrefix).ConfigureAwait(false);

        return response != null ? response : BadRequest();
    }
}
