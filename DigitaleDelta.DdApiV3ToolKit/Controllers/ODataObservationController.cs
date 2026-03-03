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
/// Controller for handling OData queries related to observations.
/// </summary>
/// <remarks>
/// This controller serves as an endpoint for processing OData-based queries for observations.
/// It supports features such as rate limiting, logging, caching, and authorisation checks.
/// </remarks>
[Route("v3/odata/observations")]
[ApiController]
[EnableRateLimiting("ObservationController")]
public class ODataObservationController : ControllerBase
{
    private readonly ODataQueryServiceParameters _oDataQueryServiceParameters;
    private readonly ILogger<ODataObservationController> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IRequestLogger _requestLogger;
    private readonly IAuthorization _authorizationService;
    private readonly BaseParameters _baseParameters;

    /// <summary>
    /// Controller responsible for handling OData queries related to observations.
    /// </summary>
    /// <remarks>
    /// This controller is configured to handle requests routed to "v3/odata/observations".
    /// It integrates functionalities for rate limiting, logging, caching, and authorisation.
    /// </remarks>
    public ODataObservationController(BaseParameters baseParameters, [FromKeyedServices("ObservationQueryServiceParameters")] ODataQueryServiceParameters oDataQueryServiceParameters, ILogger<ODataObservationController> logger, IMemoryCache memoryCache, IRequestLogger requestLogger, IAuthorization authorizationService)
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
    /// Processes an OData query request for observations and returns the query results.
    /// </summary>
    /// <remarks>
    /// The method uses OData query options to handle and execute the query against observations data.
    /// It employs mechanisms for request processing, logging, caching, and authorisation to ensure proper execution and security.
    /// </remarks>
    /// <returns>
    /// An <c>IActionResult</c> representing the result of the request. The response can include data or an HTTP error code in case of a failure.
    /// </returns>
    public async Task<IActionResult> Get()
    {
        var oDataQueryOptions = Request.ToODataQueryOptions(_oDataQueryServiceParameters.PropertyMap, _oDataQueryServiceParameters.FunctionMap, _oDataQueryServiceParameters.DefaultPageSize, _oDataQueryServiceParameters.MaxPageSize);
        var queryService = new DigitaleDelta.QueryService.QueryService(_oDataQueryServiceParameters, _logger, _memoryCache, _requestLogger, _authorizationService);
        var response = await queryService.ProcessRequestAsync(Request.HttpContext, oDataQueryOptions, parameterPrefix: _baseParameters.ParameterPrefix).ConfigureAwait(false);

        return response != null ? response : BadRequest();
    }
}
