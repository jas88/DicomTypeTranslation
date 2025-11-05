using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;

namespace DicomTypeTranslation.Tests;

class ChangeLogIsCorrectTests
{
    [TestCase("../../../../CHANGELOG.md")]
    public void TestChangeLogContents(string? changeLogPath)
    {
        Assert.That(changeLogPath, Is.Not.Null, "changeLogPath must be provided");

        if (!Path.IsPathRooted(changeLogPath))
            changeLogPath = Path.Combine(TestContext.CurrentContext.TestDirectory, changeLogPath);

        if (!File.Exists(changeLogPath))
            Assert.Fail($"Could not find file {changeLogPath}");

        var fi = new FileInfo(changeLogPath);

        if (fi.Directory == null)
            Assert.Fail($"Could not determine directory of {changeLogPath}");

        var assemblyInfo = Path.Combine(fi.Directory!.FullName,"SharedAssemblyInfo.cs");

        if(!File.Exists(assemblyInfo))
            Assert.Fail($"Could not find file {assemblyInfo}");

        var match = Regex.Match(File.ReadAllText(assemblyInfo),@"AssemblyInformationalVersion\(""(.*)""\)");
        Assert.That(match.Success, Is.True, $"Could not find AssemblyInformationalVersion tag in {assemblyInfo}");

        var currentVersion = match.Groups[1].Value;

        var changeLog = File.ReadAllText(changeLogPath);

        Assert.That(changeLog, Does.Contain($"## [{currentVersion}]"), $"{changeLogPath} did not contain a header for the current version '{currentVersion}'");

    }
}