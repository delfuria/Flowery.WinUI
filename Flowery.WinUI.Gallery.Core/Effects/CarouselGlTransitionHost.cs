using System;
using System.Collections.Generic;
using Flowery.Effects;
using Microsoft.UI.Xaml;

namespace Flowery.WinUI.Gallery.Effects
{
    public sealed partial class CarouselGlTransitionHost : CarouselGlNativeHost
    {
        private static readonly IReadOnlyDictionary<string, string> Images = new Dictionary<string, string>
        {
            ["from"] = "Assets/wallpaper_blue.jpg",
            ["to"] = "Assets/wallpaper_green.jpg"
        };
        private static readonly Lazy<IReadOnlyDictionary<string, string>> Shaders = new(LoadShaders);
        public static readonly DependencyProperty TransitionNameProperty = DependencyProperty.Register(
            nameof(TransitionName), typeof(string), typeof(CarouselGlTransitionHost), new PropertyMetadata(string.Empty, OnParametersChanged));
        public static readonly DependencyProperty IntervalSecondsProperty = DependencyProperty.Register(
            nameof(IntervalSeconds), typeof(double), typeof(CarouselGlTransitionHost), new PropertyMetadata(0d, OnParametersChanged));
        public static readonly DependencyProperty TransitionSecondsProperty = DependencyProperty.Register(
            nameof(TransitionSeconds), typeof(double), typeof(CarouselGlTransitionHost), new PropertyMetadata(0.6d, OnParametersChanged));

        public string TransitionName { get => (string)GetValue(TransitionNameProperty); set => SetValue(TransitionNameProperty, value); }
        public double IntervalSeconds { get => (double)GetValue(IntervalSecondsProperty); set => SetValue(IntervalSecondsProperty, value); }
        public double TransitionSeconds { get => (double)GetValue(TransitionSecondsProperty); set => SetValue(TransitionSecondsProperty, value); }
        public static IReadOnlyDictionary<string, string> AvailableShaders => Shaders.Value;
        protected override IReadOnlyDictionary<string, string> ShaderSources => Shaders.Value;
        protected override string EngineScript => ReadEmbeddedText(typeof(CarouselGlTransitionHost).Assembly,
            "Flowery.GL.flowery-carousel-gl-transitions.js");
        protected override CarouselGlConfiguration? CreateConfiguration() => string.IsNullOrWhiteSpace(TransitionName)
            ? null
            : new CarouselGlConfiguration("transitions", "transition", TransitionName, null, Images, IntervalSeconds, TransitionSeconds);

        private static void OnParametersChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
            ((CarouselGlTransitionHost)sender).RefreshRenderer();

        private static IReadOnlyDictionary<string, string> LoadShaders()
        {
            var assembly = typeof(CarouselGlTransitionHost).Assembly;
            const string prefix = "Flowery.GL.Shaders.";
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var resource in assembly.GetManifestResourceNames())
            {
                if (!resource.StartsWith(prefix, StringComparison.Ordinal)) continue;
                var name = resource[prefix.Length..];
                if (name.EndsWith(".glsl", StringComparison.Ordinal)) name = name[..^5];
                result.Add(name, ReadEmbeddedText(assembly, resource));
            }
            if (result.Count != 100) throw new InvalidOperationException("The complete GL transition catalog requires 100 shaders.");
            return result;
        }
    }
}
