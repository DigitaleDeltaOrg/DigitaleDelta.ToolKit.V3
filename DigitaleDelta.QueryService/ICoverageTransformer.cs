using System.Text.Json;
using DigitaleDelta.CoverageJSON.Models;

namespace DigitaleDelta.QueryService;

public interface ICoverageTransformer
{
    Coverage? Transform(JsonElement? value);
}
