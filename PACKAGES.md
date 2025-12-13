# Packages Used

### Risk Assessment common to all:
1. Packages on NuGet are virus scanned by the NuGet site.
2. This package is widely used and is actively maintained.
3. It is open source.

## Runtime Dependencies

| Package | Source Code | License | Purpose | Additional Risk Assessment |
| ------- | ------------| ------- | ------- | -------------------------- |
| FAnsiSql.Legacy |[GitHub](https://github.com/jas88/FAnsiSql) | [GPL 3.0](https://www.gnu.org/licenses/gpl-3.0.html) | Handles assigning translating database types and DBMS interactions|
| MongoDB.Bson | [GitHub](https://github.com/mongodb/mongo-csharp-driver) | [Apache 2.0](http://www.apache.org/licenses/LICENSE-2.0) | BSON serialization for writing dicom tags to MongoDb databases|
| fo-dicom | [GitHub](https://github.com/fo-dicom/fo-dicom) | [MS-PL](https://opensource.org/licenses/MS-PL) | Handles reading/writing dicom tags from dicom datasets | |
| YamlDotNet | [GitHub](https://github.com/aaubry/YamlDotNet) | [MIT](https://opensource.org/licenses/MIT) | Loading configuration files|

## Build Tools (PrivateAssets)

These packages are only used during the build process and are not distributed with the library.

| Package | Source Code | License | Purpose | Additional Risk Assessment |
| ------- | ------------| ------- | ------- | -------------------------- |
| MinVer | [GitHub](https://github.com/adamralph/minver) | [Apache 2.0](http://www.apache.org/licenses/LICENSE-2.0) | Git-based semantic versioning | |
| DotNet.ReproducibleBuilds | [GitHub](https://github.com/dotnet/reproducible-builds) | [MIT](https://opensource.org/licenses/MIT) | Ensures deterministic and reproducible builds | |
| coverlet.collector | [GitHub](https://github.com/coverlet-coverage/coverlet) | [MIT](https://opensource.org/licenses/MIT) | Code coverage collection for tests (dev/test only) | |
