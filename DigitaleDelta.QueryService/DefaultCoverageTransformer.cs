using System.Text.Json;
using DigitaleDelta.CoverageJSON.Models;

namespace DigitaleDelta.QueryService;

/// <summary>
/// Represents the default implementation of the <see cref="ICoverageTransformer"/>
/// interface, responsible for transforming a <see cref="JsonElement"/> object
/// into a <see cref="Coverage"/> object.
/// </summary>
public class DefaultCoverageTransformer : ICoverageTransformer
{
    /// <summary>
    /// Transforms a given <see cref="JsonElement"/> into a <see cref="Coverage"/> object.
    ///
    /// Implements the <see cref="ICoverageTransformer"/> interface.
    /// </summary>
    /// <param name="value">
    /// A <see cref="JsonElement"/> representing the source object to be deserialized into a <see cref="Coverage"/> object.
    /// This parameter can be null, in which case the method returns null. It assumes that the input is a valid CoverageJSON.
    /// </param>
    /// <returns>
    /// Returns a <see cref="Coverage"/> object if the deserialization is successful and the input is not null; otherwise, null.
    /// </returns>
    public Coverage? Transform(JsonElement? value) => value?.Deserialize<Coverage>();
}
