# FrozenDictionary Optimization

## Summary
Replaced `Dictionary<Type, Action<DicomDataset, DicomTag, object>>` with `FrozenDictionary<Type, Action<DicomDataset, DicomTag, object>>` in `DicomTypeTranslaterWriter.cs` for improved read performance.

## Implementation Details

### Location
- **File**: `DicomTypeTranslation/DicomTypeTranslaterWriter.cs`
- **Line**: 23 (field declaration)
- **Line**: 78 (initialization in static constructor)

### Changes Made

1. Added `using System.Collections.Frozen;` namespace
2. Changed field type from `Dictionary` to `FrozenDictionary`
3. Modified static constructor to build a regular `Dictionary` first, then freeze it using `.ToFrozenDictionary()`

### Code Pattern

```csharp
// Before:
private static readonly Dictionary<Type, Action<DicomDataset, DicomTag, object>> _dicomAddMethodDictionary
    = new Dictionary<Type, Action<DicomDataset, DicomTag, object>>();

static DicomTypeTranslaterWriter()
{
    _dicomAddMethodDictionary.Add(typeof(string), (ds, t, o) => ds.Add(t, (string)o));
    // ... more Add() calls
}

// After:
private static readonly FrozenDictionary<Type, Action<DicomDataset, DicomTag, object>> _dicomAddMethodDictionary;

static DicomTypeTranslaterWriter()
{
    var dictionary = new Dictionary<Type, Action<DicomDataset, DicomTag, object>>
    {
        { typeof(string), (ds, t, o) => ds.Add(t, (string)o) },
        // ... collection initializer syntax
    };

    _dicomAddMethodDictionary = dictionary.ToFrozenDictionary();
}
```

## Performance Benefits

### FrozenDictionary Characteristics
- **Optimized for read-heavy workloads**: Once created, a `FrozenDictionary` cannot be modified
- **Faster lookups**: Up to 2-3x faster than regular `Dictionary` for small to medium collections
- **Better memory layout**: More cache-friendly internal structure
- **Perfect for static dispatch tables**: Ideal for this use case where the dictionary is initialized once and read many times

### Use Case Fit
The `_dicomAddMethodDictionary` is a perfect candidate for `FrozenDictionary` because:
1. **Write-once**: Populated only in the static constructor
2. **Read-many**: Accessed on every call to `SetDicomTag()` method
3. **Fixed size**: Contains 41 type mappings that never change
4. **Hot path**: Used during DICOM dataset creation, which can happen frequently

### Expected Performance Impact
- **Lookup time**: Approximately 2-3x faster for `TryGetValue()` operations
- **Memory**: Slightly reduced memory footprint due to optimized internal structure
- **Initialization**: Minimal one-time cost during static constructor (negligible)

## Compatibility

### .NET Version Requirements
- Requires .NET 8.0 or later
- `System.Collections.Frozen` namespace introduced in .NET 8
- This project targets .NET 9.0, so fully compatible

### API Compatibility
- **No breaking changes**: `FrozenDictionary<TKey, TValue>` implements `IReadOnlyDictionary<TKey, TValue>`
- Same `TryGetValue()` API as `Dictionary`
- Internal implementation detail - no public API changes

## Testing

### Test Results
- All non-database tests pass: 113 passed
- `DicomTypeTranslatorTests` suite: 13/13 tests pass
- No functional changes - this is purely an internal optimization

### Test Coverage
The `SetDicomTag()` method that uses this dictionary is extensively tested across:
- Single value types (string, int, double, DateTime, etc.)
- Array types (string[], int[], DateTime[], etc.)
- Complex types (TimeSpan, DicomSequence, etc.)
- DICOM-specific types (DicomTag, DicomUID, DicomTransferSyntax, etc.)

## References

- [FrozenDictionary<TKey,TValue> Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.collections.frozen.frozendictionary-2)
- [Performance improvements in .NET 8 - Frozen Collections](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-8/#frozen-collections)
- [.NET 8 What's New - Frozen Collections](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8#frozen-collections)

## Maintenance Notes

If new DICOM type mappings need to be added in the future:
1. Add entries to the dictionary initializer in the static constructor
2. The dictionary will be automatically frozen after all entries are added
3. Do not attempt to modify `_dicomAddMethodDictionary` after initialization (compiler will prevent this)
