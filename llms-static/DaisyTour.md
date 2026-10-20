# DaisyTour

`DaisyTour` explains existing controls with a dimmed backdrop, live target highlights, annotations, and a navigation card.
It uses native WinUI controls and the shared vector presentation support.
It does not change application data or move the target into the overlay.

## Add a tour

Add one host as the last child of the page's root `Grid`.
Keep the host outside scrollable content.
The host presents its overlay across the current `XamlRoot`.

```xml
<Grid xmlns:daisy="using:Flowery.Controls">
    <StackPanel>
        <daisy:DaisyButton x:Name="CreateButton" Content="Create" />
        <daisy:DaisyButton Content="Show tour" Click="ShowTour_Click" />
    </StackPanel>
    <daisy:DaisyTour x:Name="Tour">
        <daisy:DaisyTour.Steps>
            <daisy:DaisyTourStep Id="create" Target="{x:Bind CreateButton}"
                Title="Create an item" Description="Use this button to create an item." />
        </daisy:DaisyTour.Steps>
    </daisy:DaisyTour>
</Grid>
```

```csharp
private async void ShowTour_Click(object sender, RoutedEventArgs e)
{
    await Tour.StartAsync();
}
```

`Steps` is an `ObservableCollection<DaisyTourStep>`.
Each step needs a unique, nonempty `Id` and a target in the host's `XamlRoot`.
An empty collection does not open a tour.

## Prepare a target

Use `PrepareAsync` to select a tab, expand a section, or create a target.
The host calls it before each visit, including Back.
The callback receives the session cancellation token.
After preparation, the host brings the target into view and measures its current layout.

```csharp
Tour.Steps.Add(new DaisyTourStep
{
    Id = "details",
    Title = "Details",
    Description = "Edit the selected item's details here.",
    PrepareAsync = (step, cancellationToken) =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        Tabs.SelectedItem = DetailsTab;
        step.Target = DetailsEditor;
        return Task.CompletedTask;
    }
});
```

For asynchronous application work, await that work with the supplied token.
Do not use fixed delays to wait for the layout.
Keep step definitions in the application; the library does not discover targets from names or screen coordinates.

## State and outcomes

| Member | Meaning |
| --- | --- |
| `StartAsync(CancellationToken)` | Start a session at its first step. |
| `NextAsync()` | Prepare the next step, or complete the final step. |
| `BackAsync()` | Prepare the previous step again. |
| `Skip()` | Close with `Skipped`. Escape has the same effect. |
| `Cancel()` | Close with `Cancelled`. |
| `IsOpen`, `CurrentIndex`, `CurrentStep` | Read the current session state. |
| `Closed` | Receive one outcome for the session. |

The `Closed` event supplies `DaisyTourClosedEventArgs`.
Its `Reason` is `Completed`, `Skipped`, `Cancelled`, or `TargetUnavailable`.
`StepId`, `AnnotationId`, `Target`, and `Error` identify the affected definition when applicable.
Unavailable targets do not count as completed steps.

```csharp
Tour.Closed += (_, result) =>
{
    if (result.Reason is DaisyTourCloseReason.Completed or DaisyTourCloseReason.Skipped)
        SaveTourSeen();
};
```

The application owns any first-run flag and replay command.
Keep replay available after completion or Skip.
Do not mark cancellation or a target error as successful completion.

## Annotations

Each step has an `Annotations` collection.
Give each annotation an `Id` to identify it in target-error outcomes.
An annotation uses the step target unless its `Target` supplies another control.
Set `Source` to connect two distinct controls.
Both controls must belong to the same `XamlRoot` and be visible in the layout.
When both cannot fit in a scrolled area, the step target is scrolled into view first.
A connector then still reaches the other control's real position, even outside the window.
A collapsed, transparent, detached, or foreign-window endpoint closes the tour with `TargetUnavailable`.

```csharp
var step = new DaisyTourStep
{
    Id = "relationship",
    Target = InputControl,
    Title = "Input and output",
    Description = "The output shows the result of this input."
};
step.Annotations.Add(new DaisyTourAnnotation
{
    Id = "input-output",
    Preset = DaisyTourAnnotationPreset.TwoEndedArrow,
    Source = InputControl,
    Target = OutputControl,
    SourceAnchor = DaisyTourAnchor.Right,
    TargetAnchor = DaisyTourAnchor.Left
});
Tour.Steps.Add(step);
```

| Preset | Appearance |
| --- | --- |
| `ThinArrow`, `ThickArrow`, `BlockArrow` | Thin stroke, thick stroke, or filled arrow. |
| `CurvedArrow`, `TwoEndedArrow` | Curved path or arrowheads at both ends. |
| `DottedLine`, `DashedLine`, `DashDotLine` | Connectors with separate stroke patterns. |
| `Cloud`, `SpeechBubble` | Shapes with optional text and tails. |

`SourceAnchor` and `TargetAnchor` accept `Auto`, `Top`, `Bottom`, `Left`, `Right`, and `Center`.
Automatic anchors use the current bounds of both controls.
Resize, reflow, and scrolling update the geometry during the active step.

## Style inheritance

`DaisyTour.AnnotationDefaults` supplies tour defaults.
`DaisyTourStep.AnnotationDefaults` overrides those defaults for one step.
Each `DaisyTourAnnotation` can override individual properties again.
Unset brushes, `Default` enum values, and `NaN` measurements inherit their values.

| Properties | Use |
| --- | --- |
| `Kind`, `Path` | Choose Arrow, Line, Cloud, SpeechBubble, or Custom; choose Straight, Curved, Elbow, SCurve, Bezier, or Custom paths. |
| `Stroke`, `Fill`, `Foreground`, `Opacity` | Set stroke, shape fill, text color, and opacity. |
| `StrokeThickness`, `StrokeStyle`, `DashArray`, `DashOffset` | Set stroke width and pattern. Dash values are multiples of stroke thickness. |
| `StartHead`, `EndHead`, `HeadLength`, `HeadWidth`, `ArrowStyle`, `BodyWidth` | Set each arrowhead and the arrow body independently. |
| `Width`, `Height`, `MaxWidth`, `MaxHeight`, `FontSize`, `CornerRadius` | Set the shape and text dimensions in DIP. |
| `Curvature`, anchor offsets, `OffsetX`, `OffsetY` | Adjust the path and its position relative to current target bounds. |
| `ControlPoint1X`, `ControlPoint1Y`, `ControlPoint2X`, `ControlPoint2Y` | Place the control points of a `Bezier` path. |
| `Text`, `Content`, `Tail`, `TailWidth`, `TailLength` | Supply optional annotation content and a separate Speech or Thought tail. |
| `Geometry`, `Template` | Supply custom vector geometry or a custom presentation template. |

```csharp
step.Annotations.Add(new DaisyTourAnnotation
{
    Kind = DaisyTourAnnotationKind.Line,
    Source = InputControl,
    Target = OutputControl,
    Path = DaisyTourAnnotationPath.Elbow,
    StrokeStyle = DaisyTourStrokeStyle.Custom,
    DashArray = new DaisyTourDashCollection { 6, 2, 1, 2 },
    StrokeThickness = 4,
    StartHead = DaisyTourArrowHead.None,
    EndHead = DaisyTourArrowHead.Triangle
});
```

## Connector paths

Connectors start and end at the outer edge of each highlight frame.
For `Curved`, `Elbow`, and `Bezier` paths, automatic anchors face the path's first and last direction, so a bend does not cross its own control.
Arrowheads and block bodies are filled with `Fill`, or with the stroke color when `Fill` is not set.
`Curved` bends by `Curvature`, a fraction of the connector length that defaults to 0.25.
`Elbow` uses right angles; `CornerRadius` rounds both corners.
`SCurve` leaves each anchor perpendicular to its edge; `Curvature` sets the bend and defaults to 0.5.
`Bezier` uses two control points in connector coordinates.
(0,0) is the start, (1,0) is the end, and positive Y bends to the same side as a positive `Curvature`.
A control point without both coordinates uses the `SCurve` control point.
`Custom` fits the annotation's `Geometry` between both anchors by rotation and uniform scaling.
The first and last point of its figure meet the anchors, and the stroke keeps its thickness.
Path markup such as `Geometry="M0,0 C20,-30 30,-30 50,0"` works the same way.
A geometry without a path figure, such as an `EllipseGeometry`, meets the anchors at the middle of its left and right bounds edges.
`ArrowStyle="Block"` is not applied to `Custom` paths.

```xml
<daisy:DaisyTourAnnotation Source="{x:Bind ThemeSelector}" Target="{x:Bind SaveButton}"
                           Path="Bezier" ControlPoint1X="0.2" ControlPoint1Y="-0.6"
                           ControlPoint2X="0.8" ControlPoint2Y="0.6" />
```

## Surface annotations

Clouds and speech bubbles accept text with or without a tail.
They also accept a tail without text.
Without `Width` and `Height`, a shape grows to fit its text up to `MaxWidth` and `MaxHeight`.
Text that exceeds the available height ends with an ellipsis.
`Tail="None"` removes the tail independently of the content.
Custom geometry and templates retain the host's target anchoring and lifecycle.
A `Custom` shape is centered on its `Source`, or on the step target without a `Source`.
Without `Width` and `Height`, its box is sized so that an inscribed ellipse encloses the highlighted control.

## Localization and accessibility

The host uses the existing Daisy theme, motion policy, and five `DaisySize` values.
`HighlightPadding` defaults to 6 DIP, and `HighlightCornerRadius` defaults to 8 DIP.
`NextText`, `BackText`, `SkipText`, and `FinishText` provide optional application overrides.
Without overrides, the host uses the 12 built-in translations.
`Tour_StepCount` supplies the parameterized step announcement.

Bind application titles, descriptions, and annotation text through the existing localization service.
For nonvisual step definitions, give the binding an explicit source.
The WinUI XAML compiler does not accept indexers inside `x:Bind` paths.

```xml
<daisy:DaisyTourStep Id="create" Target="{x:Bind CreateButton}" />
```

After `InitializeComponent`, attach the localized text bindings:

```csharp
var step = Tour.Steps[0];
BindingOperations.SetBinding(step, DaisyTourStep.TitleProperty, new Binding
{
    Source = Localization,
    Path = new PropertyPath("[Tour_CreateTitle]"),
    Mode = BindingMode.OneWay
});
BindingOperations.SetBinding(step, DaisyTourStep.DescriptionProperty, new Binding
{
    Source = Localization,
    Path = new PropertyPath("[Tour_CreateDescription]"),
    Mode = BindingMode.OneWay
});
```

Set `Binding.Source` before attachment.
WinUI rejects changes to `Source` after the binding attaches to a property.
An `x:Bind` on `Binding.Source` runs too late for this pattern.

The overlay blocks input to the application, including input through visible target highlights.
Tab and Shift+Tab stay inside the navigation card.
Escape skips the tour.
Closure restores previous focus when the element can still accept focus.
The navigation card remains responsible for the main accessible explanation.

## Gallery and acceptance

Open **Guided Tour** in the Gallery.
The main example combines typed XAML definitions with a target prepared in C#.
It includes nested scrolling and a target created in a different tab.
The other buttons show arrow variants, stroke patterns, cloud and bubble combinations, and a custom vector shape.
The outcome text shows the closure reason and the affected step and annotation.

Use the Gallery to check these behaviors before release:

- Complete, skip, and replay each sequence.
- Use Back after the prepared tab step.
- Resize the window through wrapping boundaries during a connector step.
- Switch through all five sizes, themes, and languages during an open tour.
- Move the window between monitors with different DPI settings.
- Check keyboard navigation, Narrator, contrast themes, and disabled Windows animations.

Runtime tests establish state and geometry behavior.
They do not replace visual checks of alignment, clipping, focus indicators, or Narrator output.
Pixelery and other application integrations own their targets, translations, persistence, and first-run decisions.

---
