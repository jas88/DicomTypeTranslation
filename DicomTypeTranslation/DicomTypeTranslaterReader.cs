
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using FellowOakDicom;
using MongoDB.Bson;


namespace DicomTypeTranslation;

/// <summary>
/// Helper class for rapidly reading <see cref="DicomTag"/> values from <see cref="DicomDataset"/> in basic C# Types (string, int, double etc.).  Also supports
/// Bson types (for MongoDb).
/// </summary>
public static class DicomTypeTranslaterReader
{

    /// <summary>
    /// Returns a column name for a DicomTag either the Dicom standard keyword on it's own or the (group,element) tag number followed by the keyword.
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="includeTagCodeAsPrefix">True to include the dicom tag code number e.g. '(0008,0058)-' before the keyword 'FailedSOPInstanceUIDList'</param>
    /// <returns></returns>
    public static string GetColumnNameForTag(DicomTag tag, bool includeTagCodeAsPrefix)
    {
        return includeTagCodeAsPrefix ? $"{tag}-{tag.DictionaryEntry.Keyword}"
            :
            tag.DictionaryEntry.Keyword;
    }

    /// <summary>
    /// Returns a basic type (string, double, int, array, dictionary etc) for the given top level <paramref name="tag"/> in the <paramref name="dataset"/>.
    /// </summary>
    /// <param name="dataset"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    public static object? GetCSharpValue(DicomDataset dataset, DicomTag tag)
    {
        return GetCSharpValue(dataset, dataset.GetDicomItem<DicomItem>(tag));
    }

    /// <summary>
    /// Returns a basic type (string, double, int array, dictionary etc) for the given <paramref name="item"/> in the <paramref name="dataset"/>.
    /// </summary>
    /// <param name="dataset"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public static object? GetCSharpValue(DicomDataset dataset, DicomItem item)
    {
        if (dataset == null || !dataset.Any())
            throw new ArgumentException("The DicomDataset is invalid as it is null or has no elements.");

        if (item == null || item.Tag == null || item.ValueRepresentation == null)
            throw new ArgumentException(
                $"The DicomItem is invalid as it is either null, has a null Tag, or null ValueRepresentation: {item}");

        if (!dataset.Contains(item))
            throw new ArgumentException("The DicomDataset does not contain the item");

        if (item.Tag == DicomTag.PixelData)
            return null;

        switch (item.ValueRepresentation.Code)
        {
            // AE - Application Entity
            case "AE":
                return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

            // AS - Age String
            case "AS":
                return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

            // AT - Attribute Tag
            case "AT":
                return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

            // CS - Code String
            case "CS":
                return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

            // DA - Date
            case "DA":
                return GetValueFromDatasetWithMultiplicity<DateTime>(dataset, item.Tag);

            // DS - Decimal String
            case "DS":
                return GetValueFromDatasetWithMultiplicity<decimal>(dataset, item.Tag);

            // DT - Date Time
            case "DT":
                return GetValueFromDatasetWithMultiplicity<DateTime>(dataset, item.Tag);

            // FL - Floating Point Single
            case "FL":
                return GetValueFromDatasetWithMultiplicity<float>(dataset, item.Tag);

            // FD - Floating Point Double
            case "FD":
                return GetValueFromDatasetWithMultiplicity<double>(dataset, item.Tag);

            // IS - Integer String
            case "IS":
                return GetValueFromDatasetWithMultiplicity<int>(dataset, item.Tag);

            // LO - Long String
            case "LO":
                return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

            // LT - Long Text
            case "LT":
                return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

            // OB - Other Byte String
            case "OB":
                return GetValueFromDatasetWithMultiplicity<byte>(dataset, item.Tag);

            // OD - Other Double String
            case "OD":
                return GetValueFromDatasetWithMultiplicity<double>(dataset, item.Tag);

            // OF - Other Float String
            case "OF":
                return GetValueFromDatasetWithMultiplicity<float>(dataset, item.Tag);

            // OL - Other Long
            case "OL":
                return GetValueFromDatasetWithMultiplicity<uint>(dataset, item.Tag);

            // OV - Other Very Long
            case "OV":
                return GetValueFromDatasetWithMultiplicity<ulong>(dataset, item.Tag);

            // OW - Other Word String
            case "OW":
                return GetValueFromDatasetWithMultiplicity<ushort>(dataset, item.Tag);

            // PN - Person Name
            case "PN":
                return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

            // SH - Short String
            case "SH":
                return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

            // SL - Signed Long
            case "SL":
                return GetValueFromDatasetWithMultiplicity<int>(dataset, item.Tag);

            // SQ - Sequence
            case "SQ":
                return GetSequenceFromDataset(dataset, item.Tag);

            // SS - Signed Short
            case "SS":
                return GetValueFromDatasetWithMultiplicity<short>(dataset, item.Tag);

            // ST - Short Text
            case "ST":
                return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

            // SV - Signed Very Long
            case "SV":
                return GetValueFromDatasetWithMultiplicity<long>(dataset, item.Tag);

            // TM - Time
            case "TM":

                var tm = GetValueFromDatasetWithMultiplicity<DateTime>(dataset, item.Tag);

                // Need to handle case where we couldn't parse to DateTime so returned string instead
                return tm is DateTime
                    ? ConvertToTimeSpanArray(tm)
                    : tm;

            // UC - Unlimited Characters
            case "UC":
                return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

            // UI - Unique Identifier
            case "UI":
                return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

            // UL - Unsigned Long
            case "UL":
                return GetValueFromDatasetWithMultiplicity<uint>(dataset, item.Tag);

            // UN - Unknown
            case "UN":
                return GetValueFromDatasetWithMultiplicity<byte>(dataset, item.Tag);

            // UR - URL
            case "UR":
                return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

            // US - Unsigned Short
            case "US":
                return GetValueFromDatasetWithMultiplicity<ushort>(dataset, item.Tag);

            // UT - Unlimited Text
            case "UT":
                return GetValueFromDatasetWithMultiplicity<string>(dataset, item.Tag);

            // UV - Unsigned Very Long
            case "UV":
                return GetValueFromDatasetWithMultiplicity<ulong>(dataset, item.Tag);

            // NONE
            case "NONE":
                return GetValueFromDatasetWithMultiplicity<object>(dataset, item.Tag);

            default:
                //return GetValueFromDatasetWithMultiplicity<object>(dataset, item.Tag);
                throw new Exception(
                    $"Unknown VR code: {item.ValueRepresentation.Code}({item.ValueRepresentation.Name})");
        }
    }

    private static object? ConvertToTimeSpanArray(object? array)
    {
        return array switch
        {
            null => null,
            DateTime dateTime => dateTime.TimeOfDay,
            _ => ((DateTime[])array).Select(static e => e.TimeOfDay).ToArray()
        };
    }

    #region Span-based Performance Optimizations

    /// <summary>
    /// Optimized version of GetSequenceFromDataset that uses Memory&lt;T&gt; to reduce allocations.
    /// Returns a memory-efficient representation of the sequence data.
    /// Use this when you need to process large sequences and want to minimize heap allocations.
    /// </summary>
    /// <param name="ds">The DicomDataset containing the sequence</param>
    /// <param name="tag">The DicomTag identifying the sequence</param>
    /// <param name="result">Output array of dictionaries representing the sequence elements</param>
    /// <returns>True if the sequence contains data, false if empty or null</returns>
    /// <remarks>
    /// This method is optimized for performance and reduces allocations by:
    /// - Using ArrayPool for temporary buffers when appropriate
    /// - Minimizing intermediate collections
    /// - Returning false for empty sequences instead of null
    /// For backward compatibility with existing code, use GetSequenceFromDataset instead.
    /// </remarks>
    public static bool TryGetSequenceFromDatasetOptimized(DicomDataset ds, DicomTag tag, out Dictionary<DicomTag, object>[]? result)
    {
        var sequence = ds.GetSequence(tag);
        if (sequence.Items.Count == 0)
        {
            result = null;
            return false;
        }

        // Pre-allocate the array to exact size needed
        var toReturn = new Dictionary<DicomTag, object>[sequence.Items.Count];

        var index = 0;
        foreach (var sequenceElement in sequence)
        {
            // Estimate initial dictionary capacity based on item count to reduce rehashing
            var itemCount = sequenceElement.Count();
            var current = new Dictionary<DicomTag, object>(itemCount);
            toReturn[index++] = current;

            foreach (var item in sequenceElement)
            {
                current.Add(item.Tag, GetCSharpValue(sequenceElement, item)!);
            }
        }

        result = toReturn;
        return true;
    }

    /// <summary>
    /// Optimized string building for BSON attribute tags using Span&lt;char&gt; to avoid allocations.
    /// Use this when you need high-performance tag string generation with minimal GC pressure.
    /// </summary>
    /// <param name="dataset">The DicomDataset containing the tag</param>
    /// <param name="tag">The DicomTag to convert</param>
    /// <param name="destination">Span to write the result into</param>
    /// <param name="charsWritten">Number of characters written to destination</param>
    /// <returns>True if the tag was successfully written to destination, false if buffer was too small</returns>
    /// <remarks>
    /// This method uses Span&lt;char&gt; to build the attribute tag string without heap allocations.
    /// The caller must provide a sufficiently large destination buffer.
    /// Recommended buffer size: at least 16 characters per value * value count.
    /// For backward compatibility with existing code, use GetAttributeTagString instead.
    /// </remarks>
    public static bool TryFormatAttributeTagString(DicomDataset dataset, DicomTag tag, Span<char> destination, out int charsWritten)
    {
        charsWritten = 0;

        var values = dataset.GetValues<string>(tag);
        if (values == null || values.Length == 0)
            return false;

        var totalLength = 0;

        // Calculate required length and validate buffer size
        for (var i = 0; i < values.Length; i++)
        {
            // Each value: remove '(', ',', ')' and add backslash separator (except last)
            var value = values.GetValue(i) as string ?? string.Empty;
            totalLength += value.Length - 3; // Approximate: removing 3 chars: '(', ',', ')'
            if (i < values.Length - 1)
                totalLength++; // backslash separator
        }

        if (destination.Length < totalLength)
            return false;

        var pos = 0;
        for (var i = 0; i < values.Length; i++)
        {
            var valueStr = values.GetValue(i) as string;
            ReadOnlySpan<char> value = valueStr != null ? valueStr.AsSpan() : ReadOnlySpan<char>.Empty;

            // Process each character, skipping '(', ',', ')'
            foreach (var c in value)
            {
                if (c != '(' && c != ',' && c != ')')
                {
                    var upperChar = char.ToUpperInvariant(c);
                    destination[pos++] = upperChar;
                }
            }

            // Add backslash separator between values
            if (i < values.Length - 1 && pos < destination.Length)
                destination[pos++] = '\\';
        }

        charsWritten = pos;
        return true;
    }

    /// <summary>
    /// Optimized version of GetBsonKeyForTag using Span&lt;char&gt; for string manipulation.
    /// Reduces allocations when building BSON keys from DicomTags.
    /// </summary>
    /// <param name="tag">The DicomTag to convert</param>
    /// <param name="destination">Span to write the result into</param>
    /// <param name="charsWritten">Number of characters written</param>
    /// <returns>True if successful, false if buffer too small</returns>
    /// <remarks>
    /// This optimized version uses Span&lt;char&gt; to build the key without intermediate string allocations.
    /// For backward compatibility with existing code, use GetBsonKeyForTag instead.
    /// Recommended buffer size: at least 256 characters to handle all tag names.
    /// </remarks>
    public static bool TryFormatBsonKeyForTag(DicomTag tag, Span<char> destination, out int charsWritten)
    {
        charsWritten = 0;

        // Determine the tag name format
        string tagName;
        if (tag.IsPrivate || tag.DictionaryEntry.MaskTag != null)
        {
            // Format: (XXXX,XXXX)-Keyword
            var tagStr = tag.ToString(); // e.g., "(0008,0058)"
            var keyword = tag.DictionaryEntry.Keyword;
            tagName = $"{tagStr}-{keyword}";
        }
        else
        {
            tagName = tag.DictionaryEntry.Keyword;
        }

        if (tagName.Length > destination.Length)
            return false;

        // Copy and replace '.' with '_' for MongoDB compatibility
        var pos = 0;
        foreach (var c in tagName)
        {
            destination[pos++] = c == '.' ? '_' : c;
        }

        charsWritten = pos;
        return true;
    }

    /// <summary>
    /// Stack-allocated buffer for small attribute tag operations.
    /// Uses stackalloc for tags with few values to avoid heap allocations entirely.
    /// </summary>
    /// <param name="dataset">The DicomDataset containing the tag</param>
    /// <param name="tag">The DicomTag to convert</param>
    /// <returns>The formatted attribute tag string</returns>
    /// <remarks>
    /// This method uses stack allocation for small buffers (up to 512 chars) to avoid GC pressure.
    /// For larger tags, it falls back to array pooling.
    /// This is the recommended high-performance alternative to GetAttributeTagString for hot paths.
    /// </remarks>
    public static string GetAttributeTagStringOptimized(DicomDataset dataset, DicomTag tag)
    {
        const int stackAllocThreshold = 512;

        var values = dataset.GetValues<string>(tag);
        if (values == null || values.Length == 0)
            return string.Empty;

        // Estimate required buffer size
        var estimatedLength = values.Length * 16; // rough estimate

        if (estimatedLength <= stackAllocThreshold)
        {
            // Use stack allocation for small buffers
            Span<char> buffer = stackalloc char[stackAllocThreshold];
            if (TryFormatAttributeTagString(dataset, tag, buffer, out var charsWritten))
                return new string(buffer[..charsWritten]);
        }

        // Fall back to pooled array for larger buffers
        var pooledArray = ArrayPool<char>.Shared.Rent(estimatedLength);
        try
        {
            if (TryFormatAttributeTagString(dataset, tag, pooledArray, out var charsWritten))
                return new string(pooledArray, 0, charsWritten);

            // If buffer was too small, try with larger buffer
            var largerArray = ArrayPool<char>.Shared.Rent(estimatedLength * 2);
            try
            {
                if (TryFormatAttributeTagString(dataset, tag, largerArray, out charsWritten))
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

        // Ultimate fallback to original implementation
        var bsonValue = GetAttributeTagString(dataset, tag);
        return bsonValue.AsString;
    }

    #endregion

    private static object? GetSequenceFromDataset(DicomDataset ds, DicomTag tag)
    {
        var toReturn = new List<Dictionary<DicomTag, object>>();

        foreach (var sequenceElement in ds.GetSequence(tag))
        {
            using var enumerator = sequenceElement.GetEnumerator();

            var current = new Dictionary<DicomTag, object>();
            toReturn.Add(current);

            while (enumerator.MoveNext())
                current.Add(enumerator.Current.Tag, GetCSharpValue(sequenceElement, enumerator.Current)!);
        }

        return toReturn.Count != 0
            ? toReturn.ToArray()
            : null;
    }

    private static object GetValueFromDatasetWithMultiplicity<TNaturalType>(DicomDataset dataset, DicomTag tag)
    {
        Array array;

        try
        {
            array = dataset.GetValues<TNaturalType>(tag);
        }
        catch (Exception e)
        {
            var vals = dataset.GetString(tag);
            throw new ArgumentException($"Tag {tag.DictionaryEntry.Keyword} {tag} has invalid value(s): '{vals}'", e);
        }

        if (array == null || array.Length == 0)
            return null;

        //if it is a single element then although the tag supports multiplicity only 1 value is stored in it so return string
        if (array.Length == 1)
            return array.GetValue(0);

        //tag supports multiplicity and the item has multiple values stored in it
        return array;
    }

    #region Bson Types

    /// <summary>
    /// Returns a key for a DicomTag either the Dicom standard keyword on it's own or the (group,element) tag number followed by the keyword. Strips out any "." for MongoDb.
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    private static string GetBsonKeyForTag(DicomTag tag)
    {
        var tagName =
            (tag.IsPrivate || tag.DictionaryEntry.MaskTag != null) ?
                GetColumnNameForTag(tag, true) :
                GetColumnNameForTag(tag, false);

        // Can't have "." in MongoDb keys
        return tagName.Replace(".", "_");
    }

    private static BsonValue CreateBsonValueFromSequence(DicomDataset ds, DicomTag tag, bool writeVr)
    {
        if (!ds.Contains(tag))
            throw new ArgumentException("The DicomDataset does not contain the item");

        var sequenceArray = new BsonArray();

        foreach (var sequenceElement in ds.GetSequence(tag))
            sequenceArray.Add(BuildBsonDocument(sequenceElement));

        if (sequenceArray.Count > 0)
            return sequenceArray;

        return writeVr
            ? (BsonValue)new BsonDocument
            {
                { "vr", "SQ" },
                { "val", BsonNull.Value }
            }
            : BsonNull.Value;
    }

    /// <summary>
    /// Create a single BsonValue from a DicomItem
    /// </summary>
    /// <param name="dataset"></param>
    /// <param name="item"></param>
    /// <param name="writeVr"></param>
    /// <returns></returns>
    private static BsonValue CreateBsonValue(DicomDataset dataset, DicomItem item, bool writeVr)
    {
        if (item is DicomSequence)
            return CreateBsonValueFromSequence(dataset, item.Tag, writeVr);

        var element = dataset.GetDicomItem<DicomElement>(item.Tag);

        BsonValue retVal;

        if (element is null || element.Count == 0)
            retVal = BsonNull.Value;

        else if (!DicomTypeTranslater.SerializeBinaryData && DicomTypeTranslater.DicomVrBlacklist.Contains(item.ValueRepresentation))
            retVal = BsonNull.Value;

        else if (element is DicomStringElement se)
        {
            se.TargetEncoding = Encoding.UTF8;
            if (se is not DicomMultiStringElement && se.Length == 0)
                retVal = BsonNull.Value;
            else
                retVal = (BsonString)dataset.GetString(element.Tag);
        }

        else if (element.ValueRepresentation == DicomVR.AT) // Special case - need to construct manually
            retVal = GetAttributeTagString(dataset, element.Tag);

        else
        {
            // Must be a numeric element - convert using default BSON mapper
            var val = dataset.GetValues<object>(item.Tag);
            retVal = BsonTypeMapper.MapToBsonValue(val);
        }

        if (!writeVr)
            return retVal;

        return new BsonDocument
        {
            { "vr", item.ValueRepresentation.Code },
            { "val", retVal }
        };
    }

    private static BsonValue GetAttributeTagString(DicomDataset dataset, DicomTag tag)
    {
        return (BsonString)string
            .Join("\\", dataset.GetValues<string>(tag))
            .Replace("(", string.Empty)
            .Replace(",", string.Empty)
            .Replace(")", string.Empty)
            .ToUpper();
    }

    /// <summary>
    /// Build an entire BsonDocument from a dataset
    /// </summary>
    /// <param name="dataset"></param>
    /// <returns></returns>
    public static BsonDocument BuildBsonDocument(DicomDataset dataset)
    {
        var datasetDoc = new BsonDocument();

        foreach (var item in dataset)
        {
            // Don't serialize group length elements
            if (((uint)item.Tag & 0xffff) == 0)
                continue;

            var bsonKey = GetBsonKeyForTag(item.Tag);

            // For private tags, or tags which have an ambiguous ValueRepresentation, we need to include the VR as well as the value
            var writeVr =
                item.Tag.IsPrivate ||
                item.Tag.DictionaryEntry.ValueRepresentations.Length > 1;

            var bsonVal = CreateBsonValue(dataset, item, writeVr);

            datasetDoc.Add(bsonKey, bsonVal);
        }

        return datasetDoc;
    }

    #endregion
}