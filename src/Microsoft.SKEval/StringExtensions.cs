// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;

namespace Microsoft.SKEval;

public static class StringExtensions
{
    public static bool IsValidJson(this string value)
    {
        try
        {
            using (JsonDocument.Parse(value))
            {
                return true; // Parsing succeeded, so it's valid JSON
            }
        }
        catch (JsonException)
        {
            return false; // Parsing failed, so it's not valid JSON
        }
        
    }
}