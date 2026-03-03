// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace DigitaleDelta.SkipToken;

/// <summary>
/// Provides utility methods for creating, validating, and processing skip tokens, which are used to maintain state or pagination across requests.
/// </summary>
public class SkipTokenHelper
{
    private const string skipTokenKey = "$skiptoken";
    private readonly byte[] _hmacKey;
    private readonly string _algorithm;

    /// <summary>
    /// Validate the strength of the secret.
    /// </summary>
    /// <param name="secret"></param>
    /// <param name="algorithm"></param>
    /// <exception cref="ArgumentException"></exception>
    public SkipTokenHelper(string secret, string algorithm = "HMACSHA256")
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new ArgumentException("The secret may not be null or empty.", nameof(secret));
        }

        if (secret.Length < 16)
        {
            throw new ArgumentException("The secret must be at least 16 characters long for proper HMAC security.", nameof(secret));
        }

        if (algorithm != "HMACSHA256" && algorithm != "HMACSHA512")
        {
            throw new ArgumentException("The algorithm must be either HMACSHA256 or HMACSHA512.", nameof(algorithm));
        }

        _algorithm = algorithm;
        _hmacKey = Encoding.UTF8.GetBytes(secret);
    }

    /// <summary>
    /// Attempts to construct a new skip token URL based on a given base URL and identifier.
    /// </summary>
    /// <param name="baseUrl">The base URL to modify and include the new skip token.</param>
    /// <param name="id">The identifier to associate with the skip token.</param>
    /// <param name="newUrl">The constructed URL containing the new skip token, or the original URL if construction fails.</param>
    /// <returns>A boolean indicating the success of the skip token URL construction. Returns true if successful, false otherwise.</returns>
    public bool TryConstructFromUrl(string baseUrl, string? id, out string? newUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) || id == null)
        {
            newUrl = baseUrl;

            return false;
        }

        var uri = new Uri(baseUrl, UriKind.RelativeOrAbsolute);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

        query.Remove(skipTokenKey);

        var newQuery = string.Join("&", query.AllKeys.Where(k => k != null).Select(k => $"{k}={Uri.EscapeDataString(query[k]!)}"));
        var skipToken = new SkipToken { RequestUrl = newQuery, LastId = id, CreationDate = DateTimeOffset.Now };
        var json = System.Text.Json.JsonSerializer.Serialize(skipToken);
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var jsonB64 = Convert.ToBase64String(jsonBytes);
        var hmac = ComputeHmac(jsonBytes);
        var hmacB64 = Convert.ToBase64String(hmac);

        newUrl = $"{jsonB64}.{hmacB64}";

        return true;
    }

    /// <summary>
    /// Attempts to extract a valid skip token from a given URL.
    /// </summary>
    /// <param name="url">The URL from which to extract the skip token.</param>
    /// <param name="token">The extracted skip token if successful; otherwise, null.</param>
    /// <param name="error">An error message if extraction fails; otherwise, null.</param>
    /// <param name="validAgeInMinutes">The maximum valid age of the skip token, in minutes. Defaults to 10.</param>
    /// <returns>
    /// Returns true if the skip token is valid and within the specified valid age; otherwise, false.
    /// </returns>
    public bool TryExtractFromUrl(string? url, out SkipToken? token, out string? error, int validAgeInMinutes = 10)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            token = null;
            error = "The url is not valid.";

            return false;
        }

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

        if (!query.AllKeys.Contains(skipTokenKey))
        {
            token = null;
            error = null;

            return true;
        }

        try
        {
            var skipTokenParam = query[skipTokenKey]!;
            var parts = skipTokenParam.Split('.', 2);

            if (parts.Length != 2)
            {
                token = null;
                error = "The skip token is not valid. Signature is missing.";

                return false;
            }

            var jsonBytes = Convert.FromBase64String(parts[0]);
            var hmac = Convert.FromBase64String(parts[1]);
            var expectedHmac = ComputeHmac(jsonBytes);

            if (!CryptographicOperations.FixedTimeEquals(expectedHmac, hmac))
            {
                token = null;
                error = "Signature mismatch. The token may have been tampered with or the secret may be incorrect.";

                return false;
            }

            var json = Encoding.UTF8.GetString(jsonBytes);
            var skipToken = System.Text.Json.JsonSerializer.Deserialize<SkipToken>(json);

            if (skipToken == null)
            {
                token = null;
                error = "The skip token is not valid.";

                return false;
            }

            var valid = skipToken.CreationDate.AddMinutes(validAgeInMinutes) >= DateTimeOffset.Now;

            if (!valid)
            {
                token = skipToken;
                error = "The skip token has expired.";

                return false;
            }

            token = skipToken;
            error = null;

            return true;
        }
        catch(Exception e)
        {
            token = null;
            error = $"The skip token is not valid. {e.Message}";

            return false;
        }
    }

    /// <summary>
    /// Computes the HMAC (Hash-based Message Authentication Code) for the given data using the specified algorithm.
    /// </summary>
    /// <param name="data">The input byte array for which the HMAC should be computed.</param>
    /// <returns>A byte array representing the computed HMAC.</returns>
    private byte[] ComputeHmac(byte[] data)
    {
        return _algorithm == "HMACSHA512"
            ? new HMACSHA512(_hmacKey).ComputeHash(data)
            : new HMACSHA256(_hmacKey).ComputeHash(data);
    }
}
