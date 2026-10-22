using System;
using System.Linq;
using System.Text.Json;
using Flowery.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.WinUI.RuntimeTests.Controls;

[TestClass]
public class Given_DaisyTourStyleProfile
{
    [TestMethod]
    public void When_ProfileRoundTrips_SparseOverridesAndInheritanceRemainDistinct()
    {
        var profile = new DaisyTourStyleProfile { TourId = "tour" };
        profile.AnnotationDefaults = new DaisyTourStyleOverride();
        DaisyTourProfileResolver.SetValue(profile.AnnotationDefaults, "strokeThickness", 4d);
        var step = new DaisyTourStepStyleProfile { Id = "step" };
        var annotation = new DaisyTourAnnotationStyleProfile { Id = "arrow", Style = new DaisyTourStyleOverride() };
        DaisyTourProfileResolver.SetValue(annotation.Style, "path", DaisyTourAnnotationPath.Bezier);
        DaisyTourProfileResolver.SetValue(annotation.Style, "stroke", "#FF12AB34");
        DaisyTourProfileResolver.SetValue(annotation.Style, "dashArray", new[] { 1d, 3d });
        DaisyTourProfileResolver.InheritValue(annotation.Style, "headWidth");
        step.Annotations.Add(annotation);
        profile.Steps.Add(step);

        var json = DaisyTourStyleProfileSerializer.Serialize(profile);
        var copy = DaisyTourStyleProfileSerializer.Deserialize(json);
        var copiedStyle = copy.Steps[0].Annotations[0].Style!;
        Assert.AreEqual(DaisyTourAnnotationPath.Bezier, copiedStyle.Values.Path);
        Assert.AreEqual("#FF12AB34", copiedStyle.Values.Stroke);
        Assert.AreEqual(4d, copy.AnnotationDefaults!.Values.StrokeThickness);
        CollectionAssert.AreEqual(new[] { 1d, 3d }, copiedStyle.Values.DashArray);
        CollectionAssert.AreEqual(new[] { "headWidth" }, copiedStyle.Inherit);
        Assert.IsNull(copiedStyle.Values.HeadWidth);
        Assert.IsNull(copiedStyle.Values.Fill);
        StringAssert.Contains(json, "\"path\": \"Bezier\"");
        Assert.IsFalse(json.Contains("\"fill\"", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("NaN", StringComparison.Ordinal));
        Assert.AreEqual(json, DaisyTourStyleProfileSerializer.Serialize(copy));
    }

    [TestMethod]
    public void When_ProfileResolves_ColorsAndDashPatternsUseNativeTypes()
    {
        var style = new DaisyTourStyleOverride();
        DaisyTourProfileResolver.SetValue(style, "stroke", "#7F12AB34");
        DaisyTourProfileResolver.SetValue(style, "dashArray", new[] { 2d, 5d });
        Assert.IsTrue(DaisyTourProfileResolver.TryGetValue(style, DaisyTourAnnotationStyle.StrokeProperty, out var value, out var inherit));
        Assert.IsFalse(inherit);
        Assert.IsInstanceOfType<SolidColorBrush>(value);
        var brush = (SolidColorBrush)value!;
        Assert.AreEqual((byte)0x7F, brush.Color.A);
        Assert.AreEqual((byte)0x12, brush.Color.R);
        Assert.AreEqual((byte)0xAB, brush.Color.G);
        Assert.AreEqual((byte)0x34, brush.Color.B);
        Assert.IsTrue(DaisyTourProfileResolver.TryGetValue(style, DaisyTourAnnotationStyle.DashArrayProperty, out value, out inherit));
        CollectionAssert.AreEqual(new[] { 2d, 5d }, ((DaisyTourDashCollection)value!).ToArray());
    }

    [TestMethod]
    public void When_OverrideIsClearedOrInherited_ApplicationValueIsNotChanged()
    {
        var annotation = new DaisyTourAnnotation { StrokeThickness = 9 };
        var style = new DaisyTourStyleOverride();
        Assert.IsFalse(DaisyTourProfileResolver.TryGetValue(style, DaisyTourAnnotationStyle.StrokeThicknessProperty, out _, out _));
        DaisyTourProfileResolver.SetValue(style, "strokeThickness", 4d);
        Assert.IsTrue(DaisyTourProfileResolver.TryGetValue(style, DaisyTourAnnotationStyle.StrokeThicknessProperty, out var value, out var inherit));
        Assert.AreEqual(4d, value);
        Assert.IsFalse(inherit);
        DaisyTourProfileResolver.InheritValue(style, "strokeThickness");
        Assert.IsTrue(DaisyTourProfileResolver.TryGetValue(style, DaisyTourAnnotationStyle.StrokeThicknessProperty, out value, out inherit));
        Assert.IsTrue(inherit);
        Assert.IsNull(value);
        DaisyTourProfileResolver.ClearValue(style, "strokeThickness");
        Assert.IsFalse(DaisyTourProfileResolver.TryGetValue(style, DaisyTourAnnotationStyle.StrokeThicknessProperty, out _, out _));
        Assert.AreEqual(9d, annotation.StrokeThickness);
    }

    [TestMethod]
    public void When_ValueMutationFails_PreviousOverrideRemains()
    {
        var style = new DaisyTourStyleOverride();
        DaisyTourProfileResolver.SetValue(style, "opacity", 0.5d);
        Assert.ThrowsExactly<ArgumentException>(() => DaisyTourProfileResolver.SetValue(style, "opacity", 2d));
        Assert.AreEqual(0.5d, style.Values.Opacity);
        DaisyTourProfileResolver.InheritValue(style, "opacity");
        Assert.ThrowsExactly<ArgumentException>(() => DaisyTourProfileResolver.SetValue(style, "opacity", double.NaN));
        CollectionAssert.AreEqual(new[] { "opacity" }, style.Inherit);
    }

    [TestMethod]
    public void When_CloneChanges_OriginalCollectionsAreIndependent()
    {
        var original = new DaisyTourStyleProfile { TourId = "tour", AnnotationDefaults = new DaisyTourStyleOverride() };
        DaisyTourProfileResolver.SetValue(original.AnnotationDefaults, "dashArray", new[] { 2d, 3d });
        var copy = DaisyTourStyleProfileSerializer.Clone(original);
        copy.AnnotationDefaults!.Values.DashArray![0] = 7;
        copy.AnnotationDefaults.Inherit.Add("stroke");
        Assert.AreEqual(2d, original.AnnotationDefaults.Values.DashArray![0]);
        Assert.HasCount(0, original.AnnotationDefaults.Inherit);
    }

    [TestMethod]
    public void When_JsonIsInvalid_WholeProfileIsRejected()
    {
        string[] cases =
        [
            "{\"schemaVersion\":2,\"tourId\":\"tour\"}",
            "{\"tourId\":\"tour\"}",
            "{\"schemaVersion\":1,\"tourId\":\"tour\",\"extra\":true}",
            "{\"schemaVersion\":1,\"schemaVersion\":1,\"tourId\":\"tour\"}",
            "{\"schemaVersion\":1,\"tourId\":\"tour\",\"steps\":null}",
            "{\"schemaVersion\":1,\"tourId\":\"bad id\"}",
            "{\"schemaVersion\":1,\"tourId\":\"tour\",\"steps\":[{\"id\":\"step\"},{\"id\":\"step\"}]}",
            "{\"schemaVersion\":1,\"tourId\":\"tour\",\"annotationDefaults\":{\"values\":{\"unknown\":1}}}",
            "{\"schemaVersion\":1,\"tourId\":\"tour\",\"annotationDefaults\":{\"values\":{\"opacity\":2}}}",
            "{\"schemaVersion\":1,\"tourId\":\"tour\",\"annotationDefaults\":{\"values\":{\"width\":0}}}",
            "{\"schemaVersion\":1,\"tourId\":\"tour\",\"annotationDefaults\":{\"values\":{\"stroke\":\"red\"}}}",
            "{\"schemaVersion\":1,\"tourId\":\"tour\",\"annotationDefaults\":{\"values\":{\"dashArray\":[0,1]}}}",
            "{\"schemaVersion\":1,\"tourId\":\"tour\",\"annotationDefaults\":{\"values\":{\"path\":1}}}",
            "{\"schemaVersion\":1,\"tourId\":\"tour\",\"annotationDefaults\":{\"values\":{\"path\":\"curved\"}}}",
            "{\"schemaVersion\":1,\"tourId\":\"tour\",\"annotationDefaults\":{\"values\":{\"path\":\"Default\"}}}",
            "{\"schemaVersion\":1,\"tourId\":\"tour\",\"annotationDefaults\":{\"values\":{\"strokeThickness\":3},\"inherit\":[\"strokeThickness\"]}}",
            "{\"schemaVersion\":1,\"tourId\":\"tour\",\"annotationDefaults\":{\"inherit\":[\"stroke\",\"stroke\"]}}",
            "{\"schemaVersion\":1,\"tourId\":\"tour\",\"annotationDefaults\":{\"values\":{\"highlightPadding\":3}}}",
            "{\"schemaVersion\":1,\"tourId\":\"tour\",\"annotationDefaults\":{\"values\":{\"sourceAnchor\":\"Top\"}}}",
        ];
        foreach (var json in cases)
            Assert.ThrowsExactly<JsonException>(() => DaisyTourStyleProfileSerializer.Deserialize(json), json);
    }

    [TestMethod]
    public void When_ProfileTargetsAreUnknownOrDuplicated_ValidationFails()
    {
        var tour = new DaisyTour { Id = "tour" };
        var step = new DaisyTourStep { Id = "step" };
        step.Annotations.Add(new DaisyTourAnnotation { Id = "arrow" });
        tour.Steps.Add(step);
        var profile = new DaisyTourStyleProfile { TourId = "tour" };
        var stepProfile = new DaisyTourStepStyleProfile { Id = "step" };
        stepProfile.Annotations.Add(new DaisyTourAnnotationStyleProfile { Id = "arrow" });
        profile.Steps.Add(stepProfile);
        DaisyTourStyleProfileSerializer.Validate(profile, tour);

        stepProfile.Annotations[0].Id = "missing";
        Assert.ThrowsExactly<ArgumentException>(() => DaisyTourStyleProfileSerializer.Validate(profile, tour));
        stepProfile.Annotations[0].Id = "arrow";
        step.Annotations.Add(new DaisyTourAnnotation { Id = "arrow" });
        Assert.ThrowsExactly<ArgumentException>(() => DaisyTourStyleProfileSerializer.Validate(profile, tour));
        step.Annotations.RemoveAt(1);
        profile.TourId = "other";
        Assert.ThrowsExactly<ArgumentException>(() => DaisyTourStyleProfileSerializer.Validate(profile, tour));
    }

    [TestMethod]
    public void When_DefaultPointerIsProfiled_ItsSyntheticIdIsRecognized()
    {
        var tour = new DaisyTour { Id = "tour" };
        tour.Steps.Add(new DaisyTourStep { Id = "step" });
        var profile = new DaisyTourStyleProfile { TourId = "tour" };
        var step = new DaisyTourStepStyleProfile { Id = "step" };
        step.Annotations.Add(new DaisyTourAnnotationStyleProfile { Id = "default-pointer" });
        profile.Steps.Add(step);
        DaisyTourStyleProfileSerializer.Validate(profile, tour);
    }
}

