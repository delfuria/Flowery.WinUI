// WinUI adaptation of Xtreme.Components Scheduler (MIT).
// See ThirdPartyNotices/Xtreme.Components.txt for attribution and license.
using Flowery.Controls.Scheduler.Models;
using Flowery.Controls.Scheduler.Rendering;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace Flowery.Controls;

public partial class DaisyScheduler
{
    // Lightweight styling keys from the Avalonia theme. Without a host override they follow the palette.
    private Brush TodayBrush => Resource("DaisySchedulerTodayBrush") as Brush ??
        new SolidColorBrush((Brush("DaisyPrimaryBrush") as SolidColorBrush)?.Color ?? Microsoft.UI.Colors.Transparent) { Opacity = 0.08 };

    // Cleared with the design tokens at the start of each render.
    private readonly Dictionary<Type, Style> _surfaceStyles = new();

    private Brush EventBackgroundBrush => Resource("DaisySchedulerEventBackgroundBrush") as Brush ?? Brush("DaisyBase200Brush");

    private Border CreateAppointment(AppointmentModel appointment, AppointmentSurface surface, Lane? lane = null,
        AppointmentModel? displayed = null, object? resourceId = null)
    {
        var shown = displayed ?? appointment;
        if (_occurrenceSources.TryGetValue(appointment, out var source))
            appointment = source;
        var background = shown.BackColor is { } back ? new SolidColorBrush(back) : EventBackgroundBrush;
        var bar = shown.BarColor is { } accent ? new SolidColorBrush(accent) : Brush("DaisyPrimaryBrush");
        var foreground = shown.BackColor is { } color ? Contrast(color) : Brush("DaisyBaseContentBrush");
        var context = new AppointmentContext(shown, surface, background, bar, foreground,
            Brush("DaisyBase300Brush"), EventCornerRadius, ShowTiles, Culture, TimePattern);
        var template = surface switch
        {
            AppointmentSurface.Chip => AppointmentChipTemplate,
            AppointmentSurface.Bar => AppointmentBarTemplate,
            _ => AppointmentTemplate
        };
        var content = AppointmentRenderer?.Invoke(context) ?? template?.LoadContent() as FrameworkElement ?? BuildAppointmentContent(context);
        content.DataContext = context;
        var frame = new Border
        {
            Background = background,
            BorderBrush = context.BorderBrush,
            BorderThickness = new Thickness(LineThickness),
            CornerRadius = new CornerRadius(EventCornerRadius),
            Child = content,
            MinHeight = ChipHeight,
            Tag = new AppointmentTarget(appointment, lane, shown, lane?.Resource?.Id ?? resourceId),
            DataContext = context,
            IsTabStop = true,
            UseSystemFocusVisuals = true
        };
        AutomationProperties.SetName(frame, appointment.Subject + ", " + context.TimeRange);
        frame.PointerPressed += OnAppointmentPressed;
        frame.KeyDown += OnAppointmentKeyDown;
        var tooltip = AppointmentTooltipRenderer?.Invoke(context) ?? AppointmentTooltipTemplate?.LoadContent() as FrameworkElement ?? BuildTooltip(context);
        tooltip.DataContext = context;
        SetSchedulerTooltip(frame, tooltip);
        _frames.Add((frame, appointment));
        return frame;
    }

    private FrameworkElement BuildAppointmentContent(AppointmentContext context)
    {
        var multiline = context.Surface == AppointmentSurface.Timed;
        // Single-line content is centered; vertical insets would clip it in the ExtraSmall badge height.
        var inset = multiline ? SpacingXS / 2 : 0;
        var main = new Grid { Margin = new Thickness(SpacingXS + LineThickness, inset, SpacingXS + LineThickness, inset) }.Define("Auto,Auto,*,Auto", "Auto,*,Auto");
        // Segoe UI lines are taller than in Flowery.NET. Single-line chips and bars use the secondary
        // size, like their tiles, so the text fits the badge-height bars. Timed blocks use compact
        // subject and time lines, so a one-hour block keeps both at ExtraSmall.
        var text = new AppointmentTextPanel(SpacingXS / 2) { VerticalAlignment = multiline ? VerticalAlignment.Stretch : VerticalAlignment.Center };
        var title = Label(context.Subject, !multiline);
        if (multiline)
            CompactLine(title);
        title.Foreground = context.Foreground;
        title.FontWeight = FontWeights.SemiBold;
        if (CurrentView == SchedulerView.Calendar)
        {
            title.TextWrapping = TextWrapping.Wrap;
            title.TextTrimming = TextTrimming.None;
        }
        text.Add(title, 0);
        if (multiline)
        {
            var time = CompactLine(Label(context.TimeRange, true));
            time.Foreground = context.Foreground;
            text.Add(time, 1);
            if (!string.IsNullOrWhiteSpace(context.Note))
            {
                var note = Label(context.Note, true);
                note.Foreground = context.Foreground;
                if (CurrentView == SchedulerView.Calendar)
                {
                    note.TextWrapping = TextWrapping.Wrap;
                    note.TextTrimming = TextTrimming.None;
                }
                text.Add(note, 4);
            }
        }
        else if (!context.IsAllDay)
            title.Text = context.StartTime + "  " + context.Subject;
        Grid.SetColumn(text, 2);
        Grid.SetRow(text, 1);
        main.Children.Add(text);

        var status = new StackPanel { Spacing = SpacingXS / 2, Margin = new Thickness(0, 0, SpacingXS, 0), Orientation = multiline ? Orientation.Vertical : Orientation.Horizontal };
        var flags = AppointmentFlags(context.Appointment);
        if (flags.Length > 0)
        {
            var label = Label(flags, true);
            label.Foreground = context.Foreground;
            status.Children.Add(label);
        }
        foreach (var indicator in context.Indicators)
        {
            var glyph = Label(indicator.Glyph, true);
            if (indicator.FontFamily is { } family)
                glyph.FontFamily = family;
            glyph.Foreground = indicator.Color is { } color ? new SolidColorBrush(color) : context.Foreground;
            SetSchedulerTooltip(glyph, indicator.Tooltip);
            status.Children.Add(glyph);
        }
        Grid.SetRow(status, 1);
        main.Children.Add(status);

        if (ShowTiles)
        {
            foreach (var side in Enum.GetValues<TileSide>())
            {
                var strip = new StackPanel { Spacing = 3, Orientation = TileLayout.IsVertical(side, multiline) ? Orientation.Vertical : Orientation.Horizontal };
                foreach (var (alignment, tiles) in TileLayout.Groups(context.Tiles, side, multiline))
                {
                    var group = new StackPanel { Spacing = 2, Orientation = strip.Orientation };
                    group.HorizontalAlignment = alignment switch { DecorationAlignment.Middle => HorizontalAlignment.Center, DecorationAlignment.Far => HorizontalAlignment.Right, _ => HorizontalAlignment.Left };
                    group.VerticalAlignment = alignment switch { DecorationAlignment.Middle => VerticalAlignment.Center, DecorationAlignment.Far => VerticalAlignment.Bottom, _ => VerticalAlignment.Top };
                    foreach (var tile in tiles)
                        group.Children.Add(CreateDecoration(tile, context, true));
                    strip.Children.Add(group);
                }
                if (multiline && side is TileSide.Top or TileSide.Bottom)
                {
                    text.Add(strip, side == TileSide.Top ? 2 : 3, first: side == TileSide.Top);
                    if (side == TileSide.Bottom)
                        text.Bottom = strip;
                }
                else
                {
                    Grid.SetColumn(strip, side switch { TileSide.Left => 1, TileSide.Right => 3, _ => 2 });
                    Grid.SetRow(strip, side switch { TileSide.Top => 0, TileSide.Bottom => 2, _ => 1 });
                    main.Children.Add(strip);
                }
            }
        }
        var root = new Grid();
        root.Children.Add(new Border { BorderBrush = context.BarBrush, BorderThickness = new Thickness(SpacingXS, 0, 0, 0), Child = main });
        foreach (var adornment in context.Adornments)
        {
            var decoration = CreateDecoration(adornment, context, false);
            decoration.HorizontalAlignment = adornment.Position switch
            {
                DecorationPosition.Left or DecorationPosition.FloatLeft => HorizontalAlignment.Left,
                DecorationPosition.Right or DecorationPosition.FloatRight => HorizontalAlignment.Right,
                _ => adornment.Alignment switch { DecorationAlignment.Near => HorizontalAlignment.Left, DecorationAlignment.Far => HorizontalAlignment.Right, _ => HorizontalAlignment.Center }
            };
            decoration.VerticalAlignment = adornment.Position switch
            {
                DecorationPosition.Bottom => VerticalAlignment.Bottom,
                DecorationPosition.Top or DecorationPosition.FloatLeft or DecorationPosition.FloatRight => VerticalAlignment.Top,
                _ => adornment.Alignment switch { DecorationAlignment.Near => VerticalAlignment.Top, DecorationAlignment.Far => VerticalAlignment.Bottom, _ => VerticalAlignment.Center }
            };
            root.Children.Add(decoration);
        }
        return root;
    }

    private static TextBlock CompactLine(TextBlock label)
    {
        label.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
        label.LineHeight = label.FontSize * 1.2;
        return label;
    }

    private static string AppointmentFlags(AppointmentModel appointment)
    {
        var flags = (appointment.IsLocked ? "🔒" : "") + (appointment.IsComplete ? "✓" : "") +
            (appointment.IsCancelled ? "×" : "") + (appointment.IsTentative ? "?" : "") +
            (appointment.IsConfirmed ? "✓" : "") + (appointment.IsRecurring ? "↻" : "");
        foreach (var indicator in appointment.Indicators)
            if (!string.IsNullOrEmpty(indicator.Glyph))
                flags = flags.Replace(indicator.Glyph, string.Empty, StringComparison.Ordinal);
        return flags;
    }

    private Border CreateDecoration(AppointmentDecoration decoration, AppointmentContext context, bool tile)
    {
        var background = decoration.BackColor is { } back ? new SolidColorBrush(back) : tile ? Brush("DaisyBase300Brush") : Transparent;
        var foreground = context.Foreground;
        if (background is SolidColorBrush { Color.A: > 0 } fill)
        {
            var color = fill.Color;
            if (color.A < 255 && context.BackBrush is SolidColorBrush backdrop)
            {
                var opacity = color.A / 255d;
                color = Color.FromArgb(255,
                    (byte)Math.Round(color.R * opacity + backdrop.Color.R * (1 - opacity)),
                    (byte)Math.Round(color.G * opacity + backdrop.Color.G * (1 - opacity)),
                    (byte)Math.Round(color.B * opacity + backdrop.Color.B * (1 - opacity)));
            }
            foreground = Contrast(color);
        }
        FrameworkElement content;
        if (decoration.Image is { Length: > 0 } bytes)
            content = OwnedImage(bytes, ChipHeight - 6);
        else
        {
            var label = Label(string.Join(" ", new[] { decoration.Glyph, decoration.Text }).Trim(), true);
            if (decoration.FontSize is { } fontSize)
                label.FontSize = fontSize;
            label.Foreground = decoration.ForeColor is { } color ? new SolidColorBrush(color) : foreground;
            label.FontWeight = decoration.Bold ? FontWeights.Bold : FontWeights.Normal;
            if (decoration.GlyphFontFamily is { } family)
                label.FontFamily = family;
            content = label;
        }
        var border = new Border
        {
            Child = content,
            Padding = new Thickness(SpacingXS - LineThickness, LineThickness, SpacingXS - LineThickness, LineThickness),
            CornerRadius = new CornerRadius(EventCornerRadius),
            Background = background,
            BorderBrush = decoration.BorderColor is { } outline ? new SolidColorBrush(outline) : Transparent,
            BorderThickness = new Thickness(decoration.BorderColor is null ? 0 : LineThickness),
            RenderTransform = new TranslateTransform { X = decoration.OffsetX, Y = decoration.OffsetY }
        };
        SetSchedulerTooltip(border, decoration.Tooltip);
        return border;
    }

    private FrameworkElement BuildTooltip(AppointmentContext context)
    {
        var stack = new StackPanel { Spacing = SpacingXS + SpacingXS / 2, MaxWidth = Token<double>("DaisySchedulerTooltipMaximumWidth") };
        var title = TooltipLabel(context.Subject);
        title.FontWeight = FontWeights.SemiBold;
        stack.Children.Add(title);
        stack.Children.Add(TooltipLabel(context.Start.ToString("d", Culture) + "  " + context.TimeRange, true));
        foreach (var detail in context.Details)
            stack.Children.Add(TooltipLabel(detail.Label + ": " + detail.Value, true));
        if (context.Note is { Length: > 0 } note)
        {
            var label = TooltipLabel(note);
            label.TextWrapping = TextWrapping.Wrap;
            stack.Children.Add(label);
        }
        if (context.Memo is { Length: > 0 } memo)
            stack.Children.Add(TooltipLabel(memo, true));
        return stack;
    }

    private TextBlock TooltipLabel(string text, bool secondary = false)
    {
        var label = Label(text, secondary);
        // Inherit the scheduler tooltip palette, including for custom appointment fills.
        label.ClearValue(TextBlock.ForegroundProperty);
        return label;
    }

    private void SetSchedulerTooltip(FrameworkElement target, object? content)
    {
        if (content is null)
            return;
        var tooltip = content as ToolTip ?? new ToolTip { Content = content };
        // A host-supplied ToolTip keeps its own style.
        if (tooltip.ReadLocalValue(FrameworkElement.StyleProperty) == DependencyProperty.UnsetValue)
            tooltip.Style = SchedulerSurfaceStyle(typeof(ToolTip));
        tooltip.FontFamily = FontFamily;
        tooltip.FontSize = Token<double>($"DaisySize{_size}SecondaryFontSize");
        tooltip.MaxWidth = Token<double>("DaisySchedulerTooltipMaximumWidth");
        ToolTipService.SetToolTip(target, tooltip);
    }

    /// <summary>The scheduler tooltip and flyout surface: Base200 fill, a visible muted border and token padding.</summary>
    private Style SchedulerSurfaceStyle(Type targetType)
    {
        if (_surfaceStyles.TryGetValue(targetType, out var cached))
            return cached;
        var style = new Style(targetType);
        _surfaceStyles[targetType] = style;
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brush("DaisyBase200Brush")));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Brush("DaisyBaseContentBrush")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Brush("DaisyBaseContentMutedBrush")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, Token<Thickness>("DaisyBorderThicknessThin")));
        style.Setters.Add(new Setter(Control.CornerRadiusProperty, Token<CornerRadius>("DaisySizeSmallCornerRadius")));
        style.Setters.Add(new Setter(Control.PaddingProperty, Token<Thickness>("DaisySchedulerTooltipPadding")));
        return style;
    }

    private static Brush Contrast(Color color)
    {
        static double Linear(byte value)
        {
            var channel = value / 255d;
            return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }
        var luminance = 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
        return new SolidColorBrush(luminance > 0.179 ? Microsoft.UI.Colors.Black : Microsoft.UI.Colors.White);
    }

    /// <summary>An image that decodes its bytes while loaded and releases the bitmap when unloaded.</summary>
    private static Image OwnedImage(byte[] bytes, double size)
    {
        var image = new Image { Width = size, Height = size, Tag = bytes };
        image.Loaded += OnOwnedImageLoaded;
        image.Unloaded += OnOwnedImageUnloaded;
        return image;
    }

    private static void OnOwnedImageLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Image { Tag: byte[] bytes } image)
            return;
        var bitmap = new BitmapImage();
        bitmap.SetSource(new MemoryStream(bytes, writable: false).AsRandomAccessStream());
        image.Source = bitmap;
    }

    private static void OnOwnedImageUnloaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Image image)
            image.Source = null;
    }
}
