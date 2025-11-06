
using FellowOakDicom;
using DicomTypeTranslation.Helpers;
using DicomTypeTranslation.Tests.Helpers;
using NLog;
using NUnit.Framework;
using System.IO;
using System.Linq;
using FellowOakDicom.Serialization;

namespace DicomTypeTranslation.Tests;

[TestFixture]
public class JsonDicomConverterTests
{
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    private static readonly string _dcmDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDicomFiles");

    private readonly string _srDcmPath = Path.Combine(_dcmDir, "report01.dcm");
    private readonly string _imDcmPath = Path.Combine(_dcmDir, "image11.dcm");

    #region Fixture Methods

    [OneTimeSetUp]
    public void SetUp()
    {
        TestLogger.Setup();

        // NOTE(rkm 2020-11-02) Disable so we can create DicomDatasets with specific test values
        // Update JS 2021-10-29 AutoValidation removed, needs to be handled differently
        DicomValidationBuilderExtension.SkipValidation(null);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        TestLogger.ShutDown();
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Serializes originalDataset to JSON, deserializes, and re-serializes.
    /// Verifies that both datasets are equal, and both json serializations are equal!
    /// </summary>
    private void VerifyJsonTripleTrip(DicomDataset originalDataset, bool expectFail = false)
    {
        var json = DicomTypeTranslater.SerializeDatasetToJson(originalDataset);
        _logger.Debug($"Initial json:\n{json}");

        var recoDataset = DicomTypeTranslater.DeserializeJsonToDataset(json);

        var json2 = DicomTypeTranslater.SerializeDatasetToJson(recoDataset);
        _logger.Debug($"Final json:\n{json}");

        Assert.That(json2, expectFail ? Is.Not.EqualTo(json) : Is.EqualTo(json));

        // NOTE: Group length elements have been retired from the standard, and have never been included in any JSON conversion.
        // Remove them here to allow comparison between datasets.
        RemoveGroupLengths(originalDataset);

        if (expectFail)
            Assert.That(DicomDatasetHelpers.ValueEquals(originalDataset, recoDataset), Is.False);
        else
            Assert.That(DicomDatasetHelpers.ValueEquals(originalDataset, recoDataset), Is.True);
    }

    /// <summary>
    /// Removes group length elements from the DICOM dataset. These have been retired in the DICOM standard.
    /// </summary>
    /// <remarks><see href="http://dicom.nema.org/medical/dicom/current/output/html/part05.html#sect_7.2"/></remarks>
    /// <param name="dataset">DICOM dataset</param>
    private static void RemoveGroupLengths(DicomDataset dataset)
    {
        if (dataset == null)
            return;

        dataset.Remove(x => x.Tag.Element == 0x0000);

        // Handle sequences
        foreach (var sq in dataset.Where(x => x.ValueRepresentation == DicomVR.SQ).Cast<DicomSequence>())
        foreach (var item in sq.Items)
            RemoveGroupLengths(item);
    }

    private static void ValidatePrivateCreatorsExist(DicomDataset dataset)
    {
        foreach (var item in dataset)
        {
            if ((item.Tag.Element & 0xff00) != 0)
                Assert.That(string.IsNullOrWhiteSpace(item.Tag.PrivateCreator?.Creator), Is.False);

            Assert.That(item.Tag.DictionaryEntry, Is.Not.Null);

            if (item.ValueRepresentation != DicomVR.SQ)
                continue;

            foreach (var subDs in ((DicomSequence)item).Items)
                ValidatePrivateCreatorsExist(subDs);
        }
    }

    #endregion

    #region Tests

    [Test]
    public void TestFile_Image11()
    {
        var ds = DicomFile.Open(_imDcmPath).Dataset;
        ds.Remove(DicomTag.PixelData);

        Assert.Throws<System.Text.Json.JsonException>(() => VerifyJsonTripleTrip(ds), "Expected JsonException parsing 2.500000 as an IntegerString");
    }

    [Test]
    public void TestFile_Report01()
    {
        var ds = DicomFile.Open(_srDcmPath).Dataset;
        VerifyJsonTripleTrip(ds);
    }

    [Ignore("Ignore unless you want to test specific files")]
    [TestCase("test.dcm")]
    public void TestFile_Other(string filePath)
    {
        Assert.That(File.Exists(filePath), Is.True);
        var ds = DicomFile.Open(filePath).Dataset;
        VerifyJsonTripleTrip(ds);
    }

    [Test]
    public void TestJsonConversionAllVrs()
    {
        VerifyJsonTripleTrip(TranslationTestHelpers.BuildVrDataset());
    }

    [Test]
    public void TestZooDataset()
    {
        var ds = TranslationTestHelpers.BuildZooDataset();
        VerifyJsonTripleTrip(ds);
    }

    [Test]
    public void TestNullDataset()
    {
        var ds = TranslationTestHelpers.BuildAllTypesNullDataset();
        VerifyJsonTripleTrip(ds);
    }

    [Test]
    public void TestBlankAndNullSerialization()
    {
        var dataset = new DicomDataset
        {
            new DicomDecimalString(DicomTag.SelectorDSValue, default(string)),
            new DicomIntegerString(DicomTag.SelectorISValue, ""),
            new DicomFloatingPointSingle(DicomTag.SelectorFLValue, default(float)),
            new DicomFloatingPointDouble(DicomTag.SelectorFDValue)
        };

        var json = DicomTypeTranslater.SerializeDatasetToJson(dataset);
        _logger.Debug(json);

        var recoDataset = DicomTypeTranslater.DeserializeJsonToDataset(json);
        Assert.That(DicomDatasetHelpers.ValueEquals(dataset, recoDataset), Is.True);
    }

    [Test]
    public void TestPrivateTagsDeserialization()
    {
        var privateCreator = DicomDictionary.Default.GetPrivateCreator("Testing");
        var privateTag1 = new DicomTag(4013, 0x008, privateCreator);
        var privateTag2 = new DicomTag(4013, 0x009, privateCreator);

        var privateDs = new DicomDataset
        {
            { DicomTag.Modality, "CT" },
            new DicomCodeString(privateTag1, "test1"),
            { DicomVR.SH, privateTag2, "test2" }
        };

        VerifyJsonTripleTrip(privateDs);
    }

    [Test]
    public void Deserialize_PrivateCreators_AreSet()
    {
        var originalDataset = TranslationTestHelpers.BuildPrivateDataset();

        ValidatePrivateCreatorsExist(originalDataset);

        var json = DicomJson.ConvertDicomToJson(originalDataset,false,false,NumberSerializationMode.PreferablyAsNumber);
        var recoDs = DicomJson.ConvertJsonToDicom(json);

        ValidatePrivateCreatorsExist(recoDs);
    }

    [Test]
    public void TestMaskedTagSerialization()
    {
        //Example: OverlayRows element has a masked tag of (60xx,0010)
        _logger.Info(
            $"DicomTag.OverlayRows.DictionaryEntry.MaskTag: {DicomTag.OverlayRows.DictionaryEntry.MaskTag}");

        const string rawJson = "{\"60000010\":{\"vr\":\"US\",\"Value\":[128]},\"60000011\":{\"vr\":\"US\",\"Value\":[614]},\"60000040\":" +
                               "{\"vr\":\"CS\",\"Value\":[\"G\"]},\"60000050\":{\"vr\":\"SS\",\"Value\":[0,0]},\"60000100\":{\"vr\":\"US\"," +
                               "\"Value\":[1]},\"60000102\":{\"vr\":\"US\",\"Value\":[0]},\"60020010\":{\"vr\":\"US\",\"Value\":[512]}," +
                               "\"60020011\":{\"vr\":\"US\",\"Value\":[614]},\"60020040\":{\"vr\":\"CS\",\"Value\":[\"G\"]},\"60020050\":" +
                               "{\"vr\":\"SS\",\"Value\":[0,0]},\"60020100\":{\"vr\":\"US\",\"Value\":[1]},\"60020102\":{\"vr\":\"US\",\"Value\":[0]}}";

        var maskDataset = DicomTypeTranslater.DeserializeJsonToDataset(rawJson);

        foreach (var item in maskDataset.Where(x => x.Tag.DictionaryEntry.Keyword == DicomTag.OverlayRows.DictionaryEntry.Keyword))
            _logger.Debug("{0} {1} - Val: {2}", item.Tag, item.Tag.DictionaryEntry.Keyword, maskDataset.TryGetString(item.Tag,out var s)?s:"(unknown)");

        VerifyJsonTripleTrip(maskDataset);
    }

    [Test]
    public void TestDecimalStringSerialization()
    {
        string[] testValues = { ".123", ".0", "5\0", " 0000012.", "00", "00.0", "-.123" };
        string[] expectedValues = { "0.123", "0.0", "5", "12", "0", "0.0", "-0.123" };

        var ds = new DicomDataset
        {
            new DicomDecimalString(DicomTag.PatientWeight, "")
        };

        var json = DicomTypeTranslater.SerializeDatasetToJson(ds);

        Assert.That(json, Is.EqualTo("{\"00101030\":{\"vr\":\"DS\"}}"));

        for (var i = 0; i < testValues.Length; i++)
        {
            ds.AddOrUpdate(DicomTag.PatientWeight, testValues[i]);

            json = DicomTypeTranslater.SerializeDatasetToJson(ds);
            Assert.That(json, Is.EqualTo($"{{\"00101030\":{{\"vr\":\"DS\",\"Value\":[{expectedValues[i]}]}}}}"));
        }
    }

    [Test]
    public void TestLongIntegerStringSerialization()
    {
        var ds = new DicomDataset();

        string[] testValues =
        {
            "123",              // Normal value
            "-123",             // Normal value
            "00000000",         // Technically allowed
            "+123",             // Strange, but allowed
            "00001234123412"    // A value we can fix and parse
        };

        // Values which will be 'fixed' by the standard JsonDicomConverter
        string[] expectedValues = { "123", "-123", "0", "123", "1234123412" };

        for (var i = 0; i < testValues.Length; i++)
        {
            ds.AddOrUpdate(new DicomIntegerString(DicomTag.SelectorAttributePrivateCreator, testValues[i]));

            var json = DicomTypeTranslater.SerializeDatasetToJson(ds);
            Assert.That(json, Is.EqualTo($@"{{""00720056"":{{""vr"":""IS"",""Value"":[{expectedValues[i]}]}}}}"));
        }

        // Value larger than an Int32 - the standard converter should handle this as a string
        ds.AddOrUpdate(new DicomIntegerString(DicomTag.SelectorAttributePrivateCreator, "10001234123412"));
        Assert.That(DicomTypeTranslater.SerializeDatasetToJson(ds), Is.EqualTo("{\"00720056\":{\"vr\":\"IS\",\"Value\":[\"10001234123412\"]}}"));
    }

    [Test]
    public void TestDicomJsonEncoding()
    {
        // As per http://dicom.nema.org/medical/dicom/current/output/chtml/part18/sect_F.2.html, the default encoding for DICOM JSON should be UTF-8

        var ds = new DicomDataset
        {
            new DicomUnlimitedText(DicomTag.TextValue, "¥£€$¢₡₢₣₤₥₦₧₨₩₪₫₭₮₯₹")
        };

        // Both converters should now correctly handle UTF-8 encoding
        VerifyJsonTripleTrip(ds);
    }

    [Test]
    public void JsonSerialization_SerializeBinaryTrue_ContainsTags()
    {
        var ds = TranslationTestHelpers.BuildVrDataset();

        DicomTypeTranslater.SerializeBinaryData = true;
        VerifyJsonTripleTrip(ds);
    }

    [Test]
    [Ignore("This test was specific to SmiJsonDicomConverter which has been removed")]
    public void JsonSerialization_SerializeBinaryFalse_ContainsEmptyTags()
    {
        // This test was only applicable for the removed SmiJsonDicomConverter
        Assert.Pass("Test not applicable - SmiJsonDicomConverter removed");
    }

    private static void AssertBlacklistedNulls(DicomDataset ds)
    {
        foreach (var item in ds.Where(x =>
                     DicomTypeTranslater.DicomVrBlacklist.Contains(x.ValueRepresentation)
                     || x.ValueRepresentation == DicomVR.SQ))
        {
            if (item.ValueRepresentation == DicomVR.SQ)
                foreach (var subDataset in (DicomSequence)item)
                    AssertBlacklistedNulls(subDataset);
            else
                Assert.That(((DicomElement)item).Buffer.Size, Is.Zero, $"Expected 0 for {item.Tag.DictionaryEntry.Keyword}");
        }
    }

    #endregion
}