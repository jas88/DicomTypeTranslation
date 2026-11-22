# Packages Used

## Risk Assessment common to all

1. Packages on NuGet are virus scanned by the NuGet site.
2. This package is widely used and is actively maintained.
3. Of the packages listed here, every runtime dependency is open source, and so is every build and test tool except Microsoft.Testing.Extensions.CodeCoverage. That package is closed source, licensed under the Microsoft Software License Terms (Microsoft .NET Library). It is used only to collect code coverage when the tests run, and is not shipped with the library. FAnsiSql.Legacy brings in two packages that are not listed here and are not open source: Oracle.ManagedDataAccess.Core (Oracle Free Distribution, Hosting, and Use Terms and Conditions) and Microsoft.Data.SqlClient.SNI.runtime (Microsoft Software License Terms for the Microsoft.Data.SqlClient.SNI Library).

## Runtime Dependencies

| Package | Source Code | License | Purpose | Additional Risk Assessment |
| ------- | ------------ | ------- | ------- | -------------------------- |
| FAnsiSql.Legacy | [GitHub](https://github.com/jas88/FAnsiSql) | [GPL 3.0](https://www.gnu.org/licenses/gpl-3.0.html) | Handles assigning translating database types and DBMS interactions | |
| MongoDB.Bson | [GitHub](https://github.com/mongodb/mongo-csharp-driver) | [Apache 2.0](http://www.apache.org/licenses/LICENSE-2.0) | BSON serialization for writing dicom tags to MongoDb databases | |
| fo-dicom | [GitHub](https://github.com/fo-dicom/fo-dicom) | [MS-PL](https://opensource.org/licenses/MS-PL) | Handles reading/writing dicom tags from dicom datasets | |
| YamlDotNet | [GitHub](https://github.com/aaubry/YamlDotNet) | [MIT](https://opensource.org/licenses/MIT) | Loading configuration files | |
| SQLitePCLRaw.lib.e_sqlite3 | [GitHub](https://github.com/ericsink/SQLitePCL.raw) | [Apache 2.0](http://www.apache.org/licenses/LICENSE-2.0) | Transitive pin of the native SQLite binary (via FAnsiSql.Sqlite/Microsoft.Data.Sqlite) to 3.50.3 to fix CVE-2025-6965 | Pinned to override a vulnerable transitive version |

## Build Tools (PrivateAssets)

These packages are only used during the build process and are not distributed with the library.

| Package | Source Code | License | Purpose | Additional Risk Assessment |
| ------- | ------------ | ------- | ------- | -------------------------- |
| MinVer | [GitHub](https://github.com/adamralph/minver) | [Apache 2.0](http://www.apache.org/licenses/LICENSE-2.0) | Git-based semantic versioning | |
| DotNet.ReproducibleBuilds | [GitHub](https://github.com/dotnet/reproducible-builds) | [MIT](https://opensource.org/licenses/MIT) | Ensures deterministic and reproducible builds | |
| Microsoft.Testing.Extensions.CodeCoverage | Closed source; documentation and samples on [GitHub](https://github.com/microsoft/codecoverage) | [Microsoft Software License Terms (Microsoft .NET Library)](https://www.nuget.org/packages/Microsoft.Testing.Extensions.CodeCoverage/18.12.0/License) | Code coverage collection for the tests on Microsoft Testing Platform; build-standards adds it to test projects (test only) | Not open source; the licence allows its use to develop and test applications |
