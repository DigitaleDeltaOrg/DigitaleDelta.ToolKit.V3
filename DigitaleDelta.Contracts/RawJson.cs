// Copyright (c) 2025, 2026 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace DigitaleDelta.Contracts;

/// <summary>
/// Marker wrapper for a string that already contains valid JSON.
/// Carried through the materialised row dictionary so the JSON serialiser can emit the value
/// verbatim (via <c>Utf8JsonWriter.WriteRawValue</c>) rather than escaping it as a string literal.
/// </summary>
/// <param name="Json">The raw JSON text. Must be a syntactically valid JSON document.</param>
public readonly record struct RawJson(string Json);
