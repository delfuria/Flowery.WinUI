# Native Carousel and Shader Effects

Flowery.WinUI hosts the existing shader effects inside native WinUI 3 WebView2 controls.
The implementation retains 50 registered effect presets and the Gallery's 100 external transition shaders.
The repository's `PORTING.md` records validated configurations and runtime verification scope.

## Two Shader Hosts

| Host | Assembly and namespace | Purpose |
| --- | --- | --- |
| CarouselGlEffectHost | Flowery.WinUI, Flowery.Effects | Registered masks, flips, transitions, text fill, and continuous effects |
| CarouselGlTransitionHost | Flowery.WinUI.Gallery.Core, Flowery.WinUI.Gallery.Effects | Gallery integration for the external transition shader catalog |

Both derive from CarouselGlNativeHost.
Each host owns its WinUI 3 WebView2 control and renders the existing WebGL shader source.
The native host embeds its renderer document and scripts.
A separate browser application or local web server is not required.
The shader views require Microsoft Edge WebView2 Runtime on the Windows machine.

## Use a Registered Effect

```xml
xmlns:fx="using:Flowery.Effects"

<fx:CarouselGlEffectHost Effect="Checkerboard"
                         Height="280"
                         TransitionSeconds="0.8"
                         TransitionSliceCount="8" />
```

The registry's built-in definitions have no images by default.
Register application assets for the selected effect before constructing its host:

```csharp
CarouselGlEffectRegistry.Register(
    new CarouselGlEffectDefinition(
        CarouselGlEffectKind.Checkerboard,
        "mask",
        new Dictionary<string, string>
        {
            ["maskA"] = "Assets/slide1.jpg",
            ["maskB"] = "Assets/slide2.jpg"
        },
        "checkerboard"));
```

The Gallery registers its own presets and assets through CarouselGlGalleryPresets.

## Preserved Effect Families

- Mask patterns, including blinds, slices, checkerboard, spiral, matrix rain, and wormhole.
- Pixel transitions, including dissolve and pixelate.
- Flip and cube transitions.
- Fade, slide, push, zoom, cover, and reveal transitions.
- Pan, zoom, drift, pulse, breath, and throw effects.
- Text-fill effects and the external shader collection used by the Gallery.

## Effect Host Properties

| Property | Default | Purpose |
| --- | --- | --- |
| Effect | None | Selects the registered preset |
| OverlayEffect | None | Adds a continuous effect over a compatible transition |
| IntervalSeconds | 0 | Controls the interval supplied to the renderer |
| TransitionSeconds | 2.4 | Transition duration |
| TransitionSliceCount | 8 | Number of tiles or slices |
| TransitionStaggerSlices | true | Enables staggered slice animation |
| TransitionSliceStaggerMs | 50 | Delay between slices |
| TransitionPixelateSize | 20 | Pixel block size |
| TransitionDissolveDensity | 0.5 | Dissolve density |
| TransitionFlipAngle | 90 | Flip angle in degrees |

Property changes refresh the renderer configuration.
Effect names use CarouselGlEffectKind.
CarouselGlEffectRegistry.TryGet reads a definition, and Register replaces or adds that definition.

## Gallery Transition Host

CarouselGlTransitionHost exposes TransitionName, IntervalSeconds, and TransitionSeconds.
Its AvailableShaders property provides the complete embedded catalog.
The shader loader requires exactly 100 entries and reports an error if that catalog is incomplete.

The Gallery owns this integration.
Applications that need it can use the same native host pattern with their own shader resources and image configuration.

## Assets

Include local images as Content and copy them to the native application's output directory.
The host accepts relative application paths, `ms-appx` URIs, HTTP(S) URLs, and data URIs.
Relative paths resolve below `AppContext.BaseDirectory`.
For `ms-appx:///` paths, the host removes that prefix and decodes URI characters before it resolves the output path.
It reads local files with `File.ReadAllBytesAsync` and sends their complete contents to the renderer as data URIs.

Preset asset keys remain unchanged:

| Mode | Keys |
| --- | --- |
| Mask | maskA, maskB |
| Flip | flipA, flipB |
| Transition | transitionA, transitionB |
| Continuous effect | effect |
| Text fill | text |

## Lifetime and Diagnostics

The host initializes its WebView2 session after Loaded.
It updates renderer activity when the effective viewport changes.
Unload closes the session, cancels pending requests, removes event handlers, and closes the WebView2 control.

CarouselGlNativeHost exposes these diagnostics:

| Member | Purpose |
| --- | --- |
| IsRendererInitialized | Indicates that the renderer session completed initialization |
| LastRenderError | Retains the most recent initialization or render error |
| RenderFailed | Reports renderer failures |
| ReadFrameAsync(elapsedSeconds) | Reads renderer identity, frame count, pixel sample, and dimensions from a loaded host |
| CompileShadersAsync() | Compiles the host's shader catalog and returns its shader names |

Renderer failures remain visible and cause diagnostic requests to fail.
FLOWERY_STATE_ROOT or FLOWERY_TEST_RESULTS_ROOT selects an isolated WebView2 profile for controlled runs.
Frame and shader diagnostics require a loaded host.

## References

- [DaisyCarousel](DaisyCarousel.md)
- [Carousel Tutorial](carousel-tutorial.md)
- [Graphics](Graphics.md)
- [Animation Helpers](FloweryAnimationHelpers.md)
