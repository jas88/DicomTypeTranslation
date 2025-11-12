
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [5.0.0] - 2025-11-12

### Added
- Pixel data format fields to all templates for better image encoding metadata tracking (#19):
  - TransferSyntaxUID (0002,0010) - Compression/encoding identification
  - PhotometricInterpretation (0028,0004) - Color space information (already present in some templates, now in all)
  - BitsAllocated (0028,0100) - Bits per pixel sample
  - BitsStored (0028,0101) - Actual bits used
  - HighBit (0028,0102) - High bit position
  - PixelRepresentation (0028,0103) - Signed vs unsigned representation

### Changed
- **BREAKING**: Template file extension changed from `.it` to `.yaml` (#17)
  - All template files now use standard YAML extension
  - Update any code that hardcodes `.it` extension
- Harmonized template field sets for consistency across all modalities (#16)
- Template inclusion in .csproj now uses wildcard pattern (`*.yaml`) for cleaner configuration

### Migration Guide
If you reference template files by extension:
- Old: `CT.it`, `MR.it`, etc.
- New: `CT.yaml`, `MR.yaml`, etc.

[5.0.0]: https://github.com/jas88/DicomTypeTranslation/compare/v4.3.0...v5.0.0

## [4.3.0] - 2025-11-05

### Added
- 9 new Span/ReadOnlySpan/Memory-optimized methods for performance-critical paths
  - TryGetSequenceFromDatasetOptimized, TryFormatAttributeTagString, and 7 other high-performance variants
  - 60-100% reduction in memory allocations for hot paths
  - 2-3× faster array-to-string operations
- Comprehensive Span optimization documentation (docs/SPAN-OPTIMIZATIONS.md)
- FrozenDictionary optimization documentation (docs/FROZENDICTIONARY-OPTIMIZATION.md)
- Record types analysis and migration guide (docs/RECORD-TYPES-ANALYSIS.md)

### Changed
- Replaced MongoDB.Driver with lighter MongoDB.Bson package (reduced dependencies)
- Converted ImageTableTemplate to record type (64% code reduction)
- Converted ImageColumnTemplate to record type (59% code reduction)
- Optimized type dispatch using FrozenDictionary (2-3× faster lookups)

### Fixed
- All 43 nullable reference type warnings (complete null safety)
- Updated JSON serialization to use standard DICOM format exclusively

### Removed
- SmiJsonDicomConverter.cs (dead code, custom JSON converter)
- Newtonsoft.Json dependency (replaced by fo-dicom's built-in serialization)
- useOwn parameter from SerializeDatasetToJson/DeserializeJsonToDataset (marked obsolete)

### Performance
- 60-100% fewer memory allocations in hot paths
- 2-3× faster type dispatch lookups
- 1.5-2× faster attribute tag formatting
- 60-80% reduction in GC pressure

## [4.2.1] - 2025-10-22

### Performance
- Additional performance work and optimizations

## [4.2.0] - 2025-10-22

### Changed
- Migrate from HIC.FAnsiSql to FAnsiSql.Legacy 3.3.1
- Upgrade to .NET 9.0
- Centralize build configuration using Directory.Build.props, Directory.Packages.props, and global.json
- Enable nullable reference types
- Change package name from HIC.DicomTypeTranslation to DicomTypeTranslation
- Update README badges and URLs to reference jas88/DicomTypeTranslation fork
- Replace buildstats.info NuGet badge with shields.io badge
- Consolidate GitHub Actions workflows: merge CodeQL into dotnet-core.yml
- Update dotnet-core.yml to use global.json for SDK version
- Change NuGet secret from NUGET_KEY to NUGET_API_KEY

### Added
- Add a DICOM template for the Mammography (MG) modality

### Performance
- Replace String.Format with string interpolation (9 locations)
- Optimize dictionary lookups using TryGetValue pattern
- Eliminate unnecessary collection allocations in SetSequenceFromObject
- Use Array.Sort instead of LINQ for dictionary key sorting
- Implement regex source generators for compile-time optimization
- Add StringBuilder capacity hints to reduce reallocations
- Use Span<char> for zero-allocation tag parsing
- Change generic Exception to ArgumentException for better diagnostics

### Fixed
- Fix NUnit analyzer warnings (NUnit1034, NUnit1033)
- Fix PackageListIsCorrectTests for centralized package management

## [4.1.5] - 2024-10-28

### Changed
- Migrate to fo-dicom 5.2.0
- Upgrade FAnsiSql to 3.2.7

## [4.1.4] - 2024-10-02

### Changed
- Migrate to fo-dicom 5.1.4
- Upgrade FAnsiSql to 3.2.3
- Upgrade TypeGuesser to 1.5.1
- Migrate to NUnit 4.2.2

## [4.1.3] - 2024-04-30

### Changed
- Migrate to fo-dicom 5.1.3
- Upgrade FAnsiSql to 3.1.4

## [4.1.2] - 2024-04-02

### Changed
- Upgrade FAnsiSql to 3.1.3
- Upgrade TypeGuesser to 1.5.0

## [4.1.1] - 2024-03-08

### Changed
- Upgrade TypeGuesser to 1.4.3
- Update FAnsiSql to 3.1.2

### Fixed
- Fix AggregateException unwrapping in dicom tag reader

## [4.1.0] - 2023-12-14

### Changed
- Upgrade to .NET 8
- Use fo-dicom 5.1.2 and FAnsiSql 3.1.0

## [4.0.0] - 2023-11-02

### Changed
- Migrate to TypeGuesser 1.4.2
- Upgrade FAnsiSql to 3.0.1
- Drop .NET Core 3.1 and .NET 6.0 support
- Update test packages

## [3.0.0] - 2023-04-20

### Changed
- Migrate to fo-dicom 5.1.0
- Drop netcoreapp3.1 and net5.0-windows support
- Target .NET 6.0 and .NET 7.0

## [2.1.2] - 2023-03-23

### Changed
- Bump fo-dicom from 5.0.3 to 5.0.4

## [2.1.1] - 2023-03-01

### Changed
- Bump FAnsiSql from 2.0.7 to 2.0.9
- Bump TypeGuesser from 1.3.3 to 1.3.4

## [2.1.0] - 2022-11-11

### Added
- Add .NET 7.0 support

### Changed
- Update fo-dicom to 5.0.3
- Update FAnsiSql to 2.0.3
- Update TypeGuesser to 1.2.2
- Update build packages

## [2.0.8] - 2022-07-26

### Changed
- Update NuGet packages

## [2.0.7] - 2022-06-22

### Changed
- Update fo-dicom from 5.0.1 to 5.0.2

## [2.0.6] - 2022-06-08

### Changed
- Update FAnsi to 2.0.1

## [2.0.5] - 2022-05-17

### Changed
- Migrate to fo-dicom 5.0.1

## [2.0.4] - 2022-04-14

### Changed
- Update packages

## [2.0.3] - 2022-04-06

### Changed
- Update packages

## [2.0.2] - 2022-01-27

### Changed
- Update FAnsi

## [2.0.1] - 2021-12-20

### Changed
- Update FAnsi

## [2.0.0] - 2021-11-03

### Added
- Package project for NuGet
- Add SourceLink support

### Changed
- Update to .NET 6.0
- Update to fo-dicom 5
- Update FAnsi
- Update YamlDotNet

### Removed
- Remove .NET Framework 4.7.2 support

## [1.0.0.0] - 2020-07-03

Initial Release

[Unreleased]: https://github.com/jas88/DicomTypeTranslation/compare/v4.3.0...HEAD
[4.3.0]: https://github.com/jas88/DicomTypeTranslation/compare/v4.2.1...v4.3.0
[4.2.1]: https://github.com/jas88/DicomTypeTranslation/compare/v4.2.0...v4.2.1
[4.2.0]: https://github.com/jas88/DicomTypeTranslation/compare/v4.1.5...v4.2.0
[4.1.5]: https://github.com/jas88/DicomTypeTranslation/compare/v4.1.4...v4.1.5
[4.1.4]: https://github.com/jas88/DicomTypeTranslation/compare/v4.1.3...v4.1.4
[4.1.3]: https://github.com/jas88/DicomTypeTranslation/compare/v4.1.2...v4.1.3
[4.1.2]: https://github.com/jas88/DicomTypeTranslation/compare/v4.1.1...v4.1.2
[4.1.1]: https://github.com/jas88/DicomTypeTranslation/compare/v4.1.0...v4.1.1
[4.1.0]: https://github.com/jas88/DicomTypeTranslation/compare/v4.0.0...v4.1.0
[4.0.0]: https://github.com/jas88/DicomTypeTranslation/compare/v3.0.0...v4.0.0
[3.0.0]: https://github.com/jas88/DicomTypeTranslation/compare/v2.1.2...v3.0.0
[2.1.2]: https://github.com/jas88/DicomTypeTranslation/compare/v2.1.1...v2.1.2
[2.1.1]: https://github.com/jas88/DicomTypeTranslation/compare/v2.1.0...v2.1.1
[2.1.0]: https://github.com/jas88/DicomTypeTranslation/compare/v2.0.8...v2.1.0
[2.0.8]: https://github.com/jas88/DicomTypeTranslation/compare/v2.0.7...v2.0.8
[2.0.7]: https://github.com/jas88/DicomTypeTranslation/compare/v2.0.6...v2.0.7
[2.0.6]: https://github.com/jas88/DicomTypeTranslation/compare/v2.0.5...v2.0.6
[2.0.5]: https://github.com/jas88/DicomTypeTranslation/compare/v2.0.4...v2.0.5
[2.0.4]: https://github.com/jas88/DicomTypeTranslation/compare/v2.0.3...v2.0.4
[2.0.3]: https://github.com/jas88/DicomTypeTranslation/compare/v2.0.2...v2.0.3
[2.0.2]: https://github.com/jas88/DicomTypeTranslation/compare/v2.0.1...v2.0.2
[2.0.1]: https://github.com/jas88/DicomTypeTranslation/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/jas88/DicomTypeTranslation/compare/v1.0.0.0...v2.0.0
[1.0.0.0]: https://github.com/jas88/DicomTypeTranslation/releases/tag/v1.0.0.0
