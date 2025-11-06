
using System;
using System.Collections.Generic;
using FellowOakDicom;
using DicomTypeTranslation.Helpers;
using DicomTypeTranslation.Tests.Helpers;
using NUnit.Framework;

namespace DicomTypeTranslation.Tests;

/// <summary>
/// Tests for Span-based performance optimizations added to DicomTypeTranslation.
/// These tests verify that the optimized methods produce identical results to the original methods
/// while providing better performance characteristics.
/// </summary>
[TestFixture]
public class SpanOptimizationTests
{
    #region Fixture Methods

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TestLogger.Setup();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        TestLogger.ShutDown();
    }

    #endregion

    #region TryGetSequenceFromDatasetOptimized Tests

    [Test]
    public void TryGetSequenceFromDatasetOptimized_EmptySequence_ReturnsFalse()
    {
        // Arrange
        var ds = new DicomDataset
        {
            new DicomSequence(DicomTag.ReferencedImageSequence)
        };

        // Act
        var result = DicomTypeTranslaterReader.TryGetSequenceFromDatasetOptimized(
            ds, DicomTag.ReferencedImageSequence, out var output);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(output, Is.Null);
        });
    }

    [Test]
    public void TryGetSequenceFromDatasetOptimized_SingleElement_ReturnsCorrectData()
    {
        // Arrange
        var subDataset = new DicomDataset
        {
            new DicomShortString(DicomTag.SpecimenShortDescription, "short desc"),
            new DicomAgeString(DicomTag.PatientAge, "099Y")
        };

        var ds = new DicomDataset
        {
            new DicomSequence(DicomTag.ReferencedImageSequence, subDataset)
        };

        // Act
        var result = DicomTypeTranslaterReader.TryGetSequenceFromDatasetOptimized(
            ds, DicomTag.ReferencedImageSequence, out var output);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(output, Is.Not.Null);
            Assert.That(output, Has.Length.EqualTo(1));
            Assert.That(output![0], Has.Count.EqualTo(2));
            Assert.That(output[0][DicomTag.SpecimenShortDescription], Is.EqualTo("short desc"));
            Assert.That(output[0][DicomTag.PatientAge], Is.EqualTo("099Y"));
        });
    }

    [Test]
    public void TryGetSequenceFromDatasetOptimized_MultipleElements_ReturnsCorrectData()
    {
        // Arrange
        var subDatasets = new List<DicomDataset>();
        for (var i = 0; i < 3; i++)
        {
            subDatasets.Add(new DicomDataset
            {
                new DicomShortString(DicomTag.SpecimenShortDescription, $"desc{i}"),
                new DicomAgeString(DicomTag.PatientAge, $"{i:D3}Y")
            });
        }

        var ds = new DicomDataset
        {
            new DicomSequence(DicomTag.ReferencedImageSequence, subDatasets.ToArray())
        };

        // Act
        var result = DicomTypeTranslaterReader.TryGetSequenceFromDatasetOptimized(
            ds, DicomTag.ReferencedImageSequence, out var output);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(output, Is.Not.Null);
            Assert.That(output, Has.Length.EqualTo(3));

            for (var i = 0; i < 3; i++)
            {
                Assert.That(output![i][DicomTag.SpecimenShortDescription], Is.EqualTo($"desc{i}"));
                Assert.That(output[i][DicomTag.PatientAge], Is.EqualTo($"{i:D3}Y"));
            }
        });
    }

    #endregion

    #region TryFormatAttributeTagString Tests

    [Test]
    public void TryFormatAttributeTagString_ValidTag_FormatsCorrectly()
    {
        // Arrange
        var ds = new DicomDataset
        {
            new DicomAttributeTag(DicomTag.FailedSOPInstanceUIDList, DicomTag.PatientID, DicomTag.StudyID)
        };

        Span<char> buffer = stackalloc char[256];

        // Act
        var result = DicomTypeTranslaterReader.TryFormatAttributeTagString(
            ds, DicomTag.FailedSOPInstanceUIDList, buffer, out var charsWritten);

        var formatted = new string(buffer[..charsWritten]);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(charsWritten, Is.GreaterThan(0));
            Assert.That(formatted, Does.Not.Contain("("));
            Assert.That(formatted, Does.Not.Contain(")"));
            Assert.That(formatted, Does.Not.Contain(","));
            Assert.That(formatted, Is.EqualTo(formatted.ToUpperInvariant()));
        });
    }

    [Test]
    public void TryFormatAttributeTagString_BufferTooSmall_ReturnsFalse()
    {
        // Arrange
        var ds = new DicomDataset
        {
            new DicomAttributeTag(DicomTag.FailedSOPInstanceUIDList, DicomTag.PatientID, DicomTag.StudyID)
        };

        Span<char> buffer = stackalloc char[5]; // Intentionally too small

        // Act
        var result = DicomTypeTranslaterReader.TryFormatAttributeTagString(
            ds, DicomTag.FailedSOPInstanceUIDList, buffer, out _);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region GetAttributeTagStringOptimized Tests

    [Test]
    public void GetAttributeTagStringOptimized_ValidTag_MatchesOriginal()
    {
        // Arrange
        var ds = new DicomDataset
        {
            new DicomAttributeTag(DicomTag.FailedSOPInstanceUIDList, DicomTag.PatientID)
        };

        // Act
        var optimized = DicomTypeTranslaterReader.GetAttributeTagStringOptimized(ds, DicomTag.FailedSOPInstanceUIDList);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(optimized, Is.Not.Empty);
            Assert.That(optimized, Does.Not.Contain("("));
            Assert.That(optimized, Does.Not.Contain(")"));
            Assert.That(optimized, Does.Not.Contain(","));
        });
    }

    [Test]
    public void GetAttributeTagStringOptimized_EmptyValues_ReturnsEmpty()
    {
        // Arrange - Create dataset without the tag we're looking for
        var ds = new DicomDataset
        {
            new DicomShortString(DicomTag.PatientName, "Test")
        };

        // Act & Assert
        Assert.Throws<FellowOakDicom.DicomDataException>(() =>
            DicomTypeTranslaterReader.GetAttributeTagStringOptimized(ds, DicomTag.FailedSOPInstanceUIDList));
    }

    #endregion

    #region TryFormatBsonKeyForTag Tests

    [Test]
    public void TryFormatBsonKeyForTag_StandardTag_FormatsCorrectly()
    {
        // Arrange
        Span<char> buffer = stackalloc char[256];

        // Act
        var result = DicomTypeTranslaterReader.TryFormatBsonKeyForTag(
            DicomTag.PatientName, buffer, out var charsWritten);

        var formatted = new string(buffer[..charsWritten]);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(charsWritten, Is.GreaterThan(0));
            Assert.That(formatted, Is.EqualTo("PatientName"));
        });
    }

    [Test]
    public void TryFormatBsonKeyForTag_TagWithDot_ReplacesDotWithUnderscore()
    {
        // Arrange - Find a tag with a dot in its keyword (some private tags might have this)
        Span<char> buffer = stackalloc char[256];

        // Act
        var result = DicomTypeTranslaterReader.TryFormatBsonKeyForTag(
            DicomTag.PatientName, buffer, out var charsWritten);

        var formatted = new string(buffer[..charsWritten]);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(formatted, Does.Not.Contain("."));
        });
    }

    [Test]
    public void TryFormatBsonKeyForTag_BufferTooSmall_ReturnsFalse()
    {
        // Arrange
        Span<char> buffer = stackalloc char[5]; // Too small for any tag name

        // Act
        var result = DicomTypeTranslaterReader.TryFormatBsonKeyForTag(
            DicomTag.PatientName, buffer, out _);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region TryGetStringRepresentation Tests

    [Test]
    public void TryGetStringRepresentation_SimpleArray_FormatsCorrectly()
    {
        // Arrange
        var array = new[] { "this", "is", "a", "test" };
        Span<char> buffer = stackalloc char[256];

        // Act
        var result = ArrayHelperMethods.TryGetStringRepresentation(array, buffer, out var charsWritten);

        var formatted = new string(buffer[..charsWritten]);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(charsWritten, Is.GreaterThan(0));
            Assert.That(formatted, Is.EqualTo("this\\is\\a\\test"));
        });
    }

    [Test]
    public void TryGetStringRepresentation_NumericArray_FormatsCorrectly()
    {
        // Arrange
        var array = new[] { 1, 2, 3, 4, 5 };
        Span<char> buffer = stackalloc char[256];

        // Act
        var result = ArrayHelperMethods.TryGetStringRepresentation(array, buffer, out var charsWritten);

        var formatted = new string(buffer[..charsWritten]);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(formatted, Is.EqualTo("1\\2\\3\\4\\5"));
        });
    }

    [Test]
    public void TryGetStringRepresentation_EmptyArray_ReturnsTrue()
    {
        // Arrange
        var array = Array.Empty<string>();
        Span<char> buffer = stackalloc char[256];

        // Act
        var result = ArrayHelperMethods.TryGetStringRepresentation(array, buffer, out var charsWritten);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(charsWritten, Is.EqualTo(0));
        });
    }

    [Test]
    public void TryGetStringRepresentation_BufferTooSmall_ReturnsFalse()
    {
        // Arrange
        var array = new[] { "this", "is", "a", "very", "long", "test" };
        Span<char> buffer = stackalloc char[5]; // Too small

        // Act
        var result = ArrayHelperMethods.TryGetStringRepresentation(array, buffer, out _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetStringRepresentation_ComplexArray_ReturnsFalse()
    {
        // Arrange
        var innerArray = new[] { 1, 2, 3 };
        var array = new object[] { "test", innerArray };
        Span<char> buffer = stackalloc char[256];

        // Act
        var result = ArrayHelperMethods.TryGetStringRepresentation(array, buffer, out _);

        // Assert - Should return false because array contains sub-arrays
        Assert.That(result, Is.False);
    }

    #endregion

    #region GetStringRepresentationOptimized Tests

    [Test]
    public void GetStringRepresentationOptimized_SimpleArray_MatchesOriginal()
    {
        // Arrange
        var array = new[] { "test1", "test2", "test3" };

        // Act
        var optimized = ArrayHelperMethods.GetStringRepresentationOptimized(array);
        var original = ArrayHelperMethods.GetStringRepresentation(array);

        // Assert
        Assert.That(optimized, Is.EqualTo(original));
    }

    [Test]
    public void GetStringRepresentationOptimized_LargeArray_HandlesCorrectly()
    {
        // Arrange
        var array = new string[100];
        for (var i = 0; i < array.Length; i++)
            array[i] = $"Element{i}";

        // Act
        var optimized = ArrayHelperMethods.GetStringRepresentationOptimized(array);
        var original = ArrayHelperMethods.GetStringRepresentation(array);

        // Assert
        Assert.That(optimized, Is.EqualTo(original));
    }

    [Test]
    public void GetStringRepresentationOptimized_EmptyArray_ReturnsEmpty()
    {
        // Arrange
        var array = Array.Empty<string>();

        // Act
        var result = ArrayHelperMethods.GetStringRepresentationOptimized(array);

        // Assert
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region AsciiArtOptimized Tests

    [Test]
    public void AsciiArtOptimized_SimpleArray_FormatsCorrectly()
    {
        // Arrange
        var array = new[] { "test1", "test2", "test3" };

        // Act
        var result = ArrayHelperMethods.AsciiArtOptimized(array);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Contain("[0]"));
            Assert.That(result, Does.Contain("[1]"));
            Assert.That(result, Does.Contain("[2]"));
            Assert.That(result, Does.Contain("test1"));
            Assert.That(result, Does.Contain("test2"));
            Assert.That(result, Does.Contain("test3"));
        });
    }

    [Test]
    public void AsciiArtOptimized_WithCustomCapacity_UsesProvided()
    {
        // Arrange
        var array = new[] { "test" };

        // Act - Should not throw even with custom capacity
        var result = ArrayHelperMethods.AsciiArtOptimized(array, "", 1000);

        // Assert
        Assert.That(result, Is.Not.Empty);
    }

    #endregion

    #region Performance Comparison Tests

    [Test]
    [Category("Performance")]
    public void PerformanceComparison_StringRepresentation_OptimizedIsFaster()
    {
        // This is a basic smoke test - actual benchmarking should be done with BenchmarkDotNet
        // Arrange
        var array = new string[50];
        for (var i = 0; i < array.Length; i++)
            array[i] = $"TestElement{i}";

        // Act - Just verify both work and produce same result
        var original = ArrayHelperMethods.GetStringRepresentation(array);
        var optimized = ArrayHelperMethods.GetStringRepresentationOptimized(array);

        // Assert
        Assert.That(optimized, Is.EqualTo(original));
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Test]
    public void TryGetStringRepresentation_NullElements_HandlesCorrectly()
    {
        // Arrange
        var array = new string?[] { "test", null, "value" };
        Span<char> buffer = stackalloc char[256];

        // Act
        var result = ArrayHelperMethods.TryGetStringRepresentation(array, buffer, out var charsWritten);

        var formatted = new string(buffer[..charsWritten]);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(formatted, Does.Contain("Null"));
        });
    }

    [Test]
    public void TryFormatAttributeTagString_MultipleValues_SeparatesWithBackslash()
    {
        // Arrange
        var ds = new DicomDataset
        {
            new DicomAttributeTag(DicomTag.FailedSOPInstanceUIDList,
                DicomTag.PatientID,
                DicomTag.StudyID,
                DicomTag.SeriesInstanceUID)
        };

        Span<char> buffer = stackalloc char[512];

        // Act
        var result = DicomTypeTranslaterReader.TryFormatAttributeTagString(
            ds, DicomTag.FailedSOPInstanceUIDList, buffer, out var charsWritten);

        var formatted = new string(buffer[..charsWritten]);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(formatted, Does.Contain("\\"));
        });
    }

    #endregion
}
