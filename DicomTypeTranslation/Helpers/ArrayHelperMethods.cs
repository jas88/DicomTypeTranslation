using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DicomTypeTranslation.Helpers;

/// <summary>
/// Helper methods for <see cref="Array"/> including equality and representation as strings
/// </summary>
public static class ArrayHelperMethods
{
    /// <summary>
    /// Returns true if the two arrays contain the same elements (using <see cref="FlexibleEquality"/>)
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool ArrayEquals(Array a, Array b)
    {
        if (a.Length != b.Length)
            return false;

        for (var i = 0; i < a.Length; i++)
            if (!FlexibleEquality.FlexibleEquals(a.GetValue(i), b.GetValue(i)))
                return false;

        return true;
    }

    /// <summary>
    /// Returns a string representation of the array suitable for human visualisation
    /// </summary>
    /// <param name="a"></param>
    /// <param name="prefix"></param>
    /// <returns></returns>
    public static string AsciiArt(Array a, string prefix = "")
    {
        var estimatedCapacity = a.Length * 50; // Rough estimate for prefix, index, and value formatting
        var sb = new StringBuilder(estimatedCapacity);

        for (var i = 0; i < a.Length; i++)
        {
            sb.Append($"{prefix} [{i}] - ");

            //if run out of values in dictionary 1
            var val = a.GetValue(i) ?? "Null";

            if (DictionaryHelperMethods.IsDictionary(val))
                sb.AppendLine($"\r\n {DictionaryHelperMethods.AsciiArt((IDictionary)val, $"{prefix}\t")}");
            else if (val is Array array)
                sb.AppendLine($"\r\n {AsciiArt(array, $"{prefix}\t")}");
            else
                sb.AppendLine(val.ToString());
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns a string representation of both arrays highlighting differences in array elements
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="prefix"></param>
    /// <returns></returns>
    public static string AsciiArt(Array a, Array b, string prefix = "")
    {
        var estimatedCapacity = Math.Max(a.Length, b.Length) * 80; // Estimate for comparing two arrays with formatting
        var sb = new StringBuilder(estimatedCapacity);

        for (var i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            sb.Append($"{prefix} [{i}] - ");

            //if run out of values in dictionary 1
            if (i > a.Length)
                sb.AppendLine($" \t <NULL> \t {b.GetValue(i)}");
            //if run out of values in dictionary 2
            else if (i > b.Length)
                sb.AppendLine($" \t {a.GetValue(i)} \t <NULL>");
            else
            {
                var val1 = a.GetValue(i);
                var val2 = b.GetValue(i);

                if (DictionaryHelperMethods.IsDictionary(val1) && DictionaryHelperMethods.IsDictionary(val2))
                    sb.Append($"\r\n {DictionaryHelperMethods.AsciiArt((IDictionary)val1,
                        (IDictionary)val2, $"{prefix}\t")}");
                else
                if (val1 is Array array1 && val2 is Array array2)
                    sb.Append($"\r\n {AsciiArt(array1,
                        array2, $"{prefix}\t")}");
                else
                    //if we haven't outrun of either array
                    sb.AppendLine($" \t {val1} \t {val2} {(FlexibleEquality.FlexibleEquals(val1, val2) ? "" : "<DIFF>")}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns true if <paramref name="a"/> contains any elements which are <see cref="Array"/> or <see cref="IDictionary"/>
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    private static bool ContainsSubArraysOrSubtrees(Array a)
    {
        return a.OfType<Array>().Any() || a.OfType<IDictionary>().Any();
    }

    /// <summary>
    /// Separates array elements with backslashes unless the array contains sub arrays or dictionaries in which case it resorts to ASCIIArt
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    public static string GetStringRepresentation(Array a)
    {
        if (ContainsSubArraysOrSubtrees(a))
            return AsciiArt(a);

        var estimatedCapacity = a.Length * 10; // Rough estimate for simple values with backslash separators
        var sb = new StringBuilder(estimatedCapacity);
        sb.AppendJoin('\\', a.Cast<object>());
        return sb.ToString();
    }

    #region Span-based Performance Optimizations

    /// <summary>
    /// Optimized version of GetStringRepresentation using Span&lt;char&gt; for improved performance.
    /// Writes the string representation of the array to the provided destination span.
    /// Use this when you need high-performance array string conversion with minimal allocations.
    /// </summary>
    /// <param name="a">The array to convert to string representation</param>
    /// <param name="destination">Destination span to write the result</param>
    /// <param name="charsWritten">Number of characters written to destination</param>
    /// <returns>True if successful, false if buffer too small or array contains complex structures</returns>
    /// <remarks>
    /// This method uses Span&lt;char&gt; to avoid string allocations during formatting.
    /// For arrays with sub-arrays or dictionaries, this method returns false and the caller
    /// should fall back to GetStringRepresentation.
    /// For backward compatibility with existing code, use GetStringRepresentation instead.
    /// Recommended buffer size: estimate 10-20 chars per element for simple types.
    /// </remarks>
    public static bool TryGetStringRepresentation(Array a, Span<char> destination, out int charsWritten)
    {
        charsWritten = 0;

        if (ContainsSubArraysOrSubtrees(a))
            return false; // Cannot handle complex structures with Span

        if (a.Length == 0)
            return true;

        var pos = 0;
        for (var i = 0; i < a.Length; i++)
        {
            var element = a.GetValue(i);
            var elementStr = element?.ToString() ?? "Null";

            // Check if there's enough space for this element and separator
            var requiredSpace = elementStr.Length + (i < a.Length - 1 ? 1 : 0);
            if (pos + requiredSpace > destination.Length)
                return false;

            // Copy element string
            elementStr.AsSpan().CopyTo(destination[pos..]);
            pos += elementStr.Length;

            // Add backslash separator (except after last element)
            if (i < a.Length - 1)
                destination[pos++] = '\\';
        }

        charsWritten = pos;
        return true;
    }

    /// <summary>
    /// Optimized version of GetStringRepresentation that uses stack allocation for small arrays
    /// and array pooling for larger arrays to minimize heap allocations.
    /// This is the recommended high-performance alternative to GetStringRepresentation for hot paths.
    /// </summary>
    /// <param name="a">The array to convert to string representation</param>
    /// <returns>String representation of the array</returns>
    /// <remarks>
    /// This method uses:
    /// - Stack allocation (stackalloc) for small arrays (up to 1KB buffer)
    /// - ArrayPool for medium-sized arrays
    /// - Falls back to StringBuilder for very large or complex arrays
    /// Use this for performance-critical paths where array-to-string conversion is frequent.
    /// For backward compatibility with existing code, use GetStringRepresentation instead.
    /// </remarks>
    public static string GetStringRepresentationOptimized(Array a)
    {
        if (ContainsSubArraysOrSubtrees(a))
            return AsciiArt(a);

        if (a.Length == 0)
            return string.Empty;

        const int stackAllocThreshold = 1024;

        // Estimate required buffer size (conservative)
        var estimatedSize = a.Length * 20; // 20 chars per element average

        if (estimatedSize <= stackAllocThreshold)
        {
            // Use stack allocation for small arrays
            Span<char> buffer = stackalloc char[stackAllocThreshold];
            if (TryGetStringRepresentation(a, buffer, out var charsWritten))
                return new string(buffer[..charsWritten]);
        }

        // Use array pooling for larger arrays
        var pooledArray = ArrayPool<char>.Shared.Rent(estimatedSize);
        try
        {
            if (TryGetStringRepresentation(a, pooledArray, out var charsWritten))
                return new string(pooledArray, 0, charsWritten);

            // If buffer was too small, try again with larger buffer
            var largerArray = ArrayPool<char>.Shared.Rent(estimatedSize * 2);
            try
            {
                if (TryGetStringRepresentation(a, largerArray, out charsWritten))
                    return new string(largerArray, 0, charsWritten);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(largerArray);
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(pooledArray);
        }

        // Ultimate fallback to original StringBuilder implementation
        return GetStringRepresentation(a);
    }

    /// <summary>
    /// Optimized version of AsciiArt using StringBuilder with pooled initial capacity estimation.
    /// Use this for high-performance formatting of arrays with known approximate sizes.
    /// </summary>
    /// <param name="a">The array to format</param>
    /// <param name="prefix">Prefix for each line</param>
    /// <param name="estimatedCapacity">Estimated capacity for the StringBuilder (if known)</param>
    /// <returns>Formatted string representation</returns>
    /// <remarks>
    /// This optimized version allows the caller to provide a capacity hint to avoid StringBuilder
    /// reallocations. If estimatedCapacity is 0 or not provided, it uses the default estimation.
    /// For backward compatibility with existing code, use AsciiArt instead.
    /// </remarks>
    public static string AsciiArtOptimized(Array a, string prefix = "", int estimatedCapacity = 0)
    {
        if (estimatedCapacity <= 0)
            estimatedCapacity = a.Length * 50; // Default rough estimate

        var sb = new StringBuilder(estimatedCapacity);

        for (var i = 0; i < a.Length; i++)
        {
            sb.Append($"{prefix} [{i}] - ");

            var val = a.GetValue(i) ?? "Null";

            if (DictionaryHelperMethods.IsDictionary(val))
                sb.AppendLine($"\r\n {DictionaryHelperMethods.AsciiArt((IDictionary)val, $"{prefix}\t")}");
            else if (val is Array array)
                sb.AppendLine($"\r\n {AsciiArtOptimized(array, $"{prefix}\t", array.Length * 50)}");
            else
                sb.AppendLine(val.ToString());
        }

        return sb.ToString();
    }

    #endregion
}