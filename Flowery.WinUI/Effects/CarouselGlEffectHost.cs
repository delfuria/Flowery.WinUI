using System;
using Microsoft.UI.Xaml;

namespace Flowery.Effects
{
    public sealed partial class CarouselGlEffectHost : CarouselGlNativeHost
    {
        public static readonly DependencyProperty EffectProperty =
            DependencyProperty.Register(
                nameof(Effect),
                typeof(CarouselGlEffectKind),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(CarouselGlEffectKind.None, OnParametersChanged));

        public static readonly DependencyProperty IntervalSecondsProperty =
            DependencyProperty.Register(
                nameof(IntervalSeconds),
                typeof(double),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(0d, OnParametersChanged));

        public static readonly DependencyProperty TransitionSecondsProperty =
            DependencyProperty.Register(
                nameof(TransitionSeconds),
                typeof(double),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(2.4d, OnParametersChanged));

        public static readonly DependencyProperty OverlayEffectProperty =
            DependencyProperty.Register(
                nameof(OverlayEffect),
                typeof(CarouselGlEffectKind),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(CarouselGlEffectKind.None, OnParametersChanged));

        public static readonly DependencyProperty TransitionSliceCountProperty =
            DependencyProperty.Register(
                nameof(TransitionSliceCount),
                typeof(int),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(8, OnParametersChanged));

        public static readonly DependencyProperty TransitionStaggerSlicesProperty =
            DependencyProperty.Register(
                nameof(TransitionStaggerSlices),
                typeof(bool),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(true, OnParametersChanged));

        public static readonly DependencyProperty TransitionSliceStaggerMsProperty =
            DependencyProperty.Register(
                nameof(TransitionSliceStaggerMs),
                typeof(double),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(50.0, OnParametersChanged));

        public static readonly DependencyProperty TransitionPixelateSizeProperty =
            DependencyProperty.Register(
                nameof(TransitionPixelateSize),
                typeof(int),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(20, OnParametersChanged));

        public static readonly DependencyProperty TransitionDissolveDensityProperty =
            DependencyProperty.Register(
                nameof(TransitionDissolveDensity),
                typeof(double),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(0.5, OnParametersChanged));

        public static readonly DependencyProperty TransitionFlipAngleProperty =
            DependencyProperty.Register(
                nameof(TransitionFlipAngle),
                typeof(double),
                typeof(CarouselGlEffectHost),
                new PropertyMetadata(90.0, OnParametersChanged));

        public CarouselGlEffectKind Effect
        {
            get => (CarouselGlEffectKind)GetValue(EffectProperty);
            set => SetValue(EffectProperty, value);
        }

        public double IntervalSeconds
        {
            get => (double)GetValue(IntervalSecondsProperty);
            set => SetValue(IntervalSecondsProperty, value);
        }

        public double TransitionSeconds
        {
            get => (double)GetValue(TransitionSecondsProperty);
            set => SetValue(TransitionSecondsProperty, value);
        }

        public CarouselGlEffectKind OverlayEffect
        {
            get => (CarouselGlEffectKind)GetValue(OverlayEffectProperty);
            set => SetValue(OverlayEffectProperty, value);
        }

        public int TransitionSliceCount
        {
            get => (int)GetValue(TransitionSliceCountProperty);
            set => SetValue(TransitionSliceCountProperty, value);
        }

        public bool TransitionStaggerSlices
        {
            get => (bool)GetValue(TransitionStaggerSlicesProperty);
            set => SetValue(TransitionStaggerSlicesProperty, value);
        }

        public double TransitionSliceStaggerMs
        {
            get => (double)GetValue(TransitionSliceStaggerMsProperty);
            set => SetValue(TransitionSliceStaggerMsProperty, value);
        }

        public int TransitionPixelateSize
        {
            get => (int)GetValue(TransitionPixelateSizeProperty);
            set => SetValue(TransitionPixelateSizeProperty, value);
        }

        public double TransitionDissolveDensity
        {
            get => (double)GetValue(TransitionDissolveDensityProperty);
            set => SetValue(TransitionDissolveDensityProperty, value);
        }

        public double TransitionFlipAngle
        {
            get => (double)GetValue(TransitionFlipAngleProperty);
            set => SetValue(TransitionFlipAngleProperty, value);
        }

        private static void OnParametersChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ((CarouselGlEffectHost)sender).RefreshRenderer();
        }

        protected override string EngineScript => ReadEmbeddedText(
            typeof(CarouselGlEffectHost).Assembly, "Flowery.WebAssets.flowery-carousel-gl.js");

        protected override CarouselGlConfiguration? CreateConfiguration()
        {
            var effect = Effect == CarouselGlEffectKind.None ? OverlayEffect : Effect;
            if (effect == CarouselGlEffectKind.None) return null;
            if (!CarouselGlEffectRegistry.TryGet(effect, out var definition) || definition is null)
                throw new InvalidOperationException("GL effect is not registered: " + effect);
            string? overlayVariant = null;
            if (definition.Mode is not "effect" and not "text"
                && CarouselGlEffectRegistry.TryGet(OverlayEffect, out var overlay)
                && overlay is { Mode: "effect" }) overlayVariant = overlay.Variant;
            return new CarouselGlConfiguration("effects", definition.Mode, definition.Variant, overlayVariant,
                definition.Assets, IntervalSeconds, TransitionSeconds,
                TransitionSliceCount, TransitionStaggerSlices, TransitionSliceStaggerMs,
                TransitionPixelateSize, TransitionDissolveDensity, TransitionFlipAngle);
        }
    }
}
