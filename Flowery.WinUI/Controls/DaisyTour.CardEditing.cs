using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation;
using Windows.System;

namespace Flowery.Controls;

public sealed partial class DaisyTour
{
    private readonly DaisyButton _cardMove = new()
    {
        Content = new FontIcon { Glyph = "\uE759", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 16 },
        Variant = DaisyButtonVariant.Ghost, Shape = DaisyButtonShape.Square, Size = DaisySize.Small,
        Visibility = Visibility.Collapsed, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(8, 0, 0, 0)
    };
    private readonly DaisyButton _cardSave = new()
    {
        Content = new SymbolIcon(Symbol.Save), Variant = DaisyButtonVariant.Ghost,
        Shape = DaisyButtonShape.Square, Size = DaisySize.Small, IsEnabled = false,
        Visibility = Visibility.Collapsed, VerticalAlignment = VerticalAlignment.Top
    };
    private (Pointer Pointer, Point PointerOrigin, Point CardOrigin, Point? PreviousPosition)? _cardDrag;
    private Point? _temporaryCardPosition;
    private Rect _configuredCardBounds;
    private Rect _intendedCardBounds;
    internal bool IsCardDragging => _cardDrag is not null;
    private bool CardPositionChanged => _temporaryCardPosition is not null &&
        (Math.Abs(_intendedCardBounds.X - _configuredCardBounds.X) >= 0.5 || Math.Abs(_intendedCardBounds.Y - _configuredCardBounds.Y) >= 0.5);

    private Grid CreateCardHeader()
    {
        FlowerySizeManager.SetIgnoreGlobalSize(_cardMove, true);
        FlowerySizeManager.SetIgnoreGlobalSize(_cardSave, true);
        AutomationProperties.SetAutomationId(_cardMove, "DaisyTourMoveCard");
        AutomationProperties.SetAutomationId(_cardSave, "DaisyTourSaveCard");
        _cardSave.Click += OnCardSave;
        _cardMove.AddHandler(PointerPressedEvent, new PointerEventHandler(OnCardMovePressed), true);
        _cardMove.AddHandler(PointerMovedEvent, new PointerEventHandler(OnCardMoveMoved), true);
        _cardMove.AddHandler(PointerReleasedEvent, new PointerEventHandler(OnCardMoveFinished), true);
        _cardMove.AddHandler(PointerCaptureLostEvent, new PointerEventHandler(OnCardMoveFinished), true);
        _cardMove.AddHandler(PointerCanceledEvent, new PointerEventHandler(OnCardMoveCanceled), true);
        _cardMove.KeyDown += OnCardMoveKeyDown;
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(_title);
        Grid.SetColumn(_cardMove, 1);
        header.Children.Add(_cardMove);
        Grid.SetColumn(_cardSave, 2);
        header.Children.Add(_cardSave);
        return header;
    }

    private void OnCardMovePressed(object sender, PointerRoutedEventArgs args)
    {
        if (_editor is not { } editor || _session is not { Busy: false } ||
            !args.GetCurrentPoint(_surface).Properties.IsLeftButtonPressed) return;
        editor.CancelGesture();
        _cardDrag = (args.Pointer, args.GetCurrentPoint(_surface).Position, new Point(CardBounds.X, CardBounds.Y), _temporaryCardPosition);
        if (!_cardMove.CapturePointer(args.Pointer))
        {
            ReleaseCardDrag();
            return;
        }
        _cardMove.Focus(FocusState.Pointer);
        args.Handled = true;
    }

    private void OnCardMoveMoved(object sender, PointerRoutedEventArgs args)
    {
        if (_cardDrag is not { } drag || drag.Pointer.PointerId != args.Pointer.PointerId) return;
        UpdateCardDrag(args.GetCurrentPoint(_surface).Position);
        args.Handled = true;
    }

    private void OnCardMoveFinished(object sender, PointerRoutedEventArgs args)
    {
        if (_cardDrag is not { } drag || drag.Pointer.PointerId != args.Pointer.PointerId) return;
        // A button can release capture before its PointerReleased handler runs.
        UpdateCardDrag(args.GetCurrentPoint(_surface).Position);
        ReleaseCardDrag();
        args.Handled = true;
    }

    private void OnCardMoveCanceled(object sender, PointerRoutedEventArgs args)
    {
        if (_cardDrag is not { } drag || drag.Pointer.PointerId != args.Pointer.PointerId) return;
        CancelCardDrag();
        args.Handled = true;
    }

    private void UpdateCardDrag(Point point)
    {
        if (_cardDrag is not { } drag) return;
        MoveCardTo(new Point(drag.CardOrigin.X + point.X - drag.PointerOrigin.X,
            drag.CardOrigin.Y + point.Y - drag.PointerOrigin.Y));
    }

    private void MoveCardTo(Point point)
    {
        if (_editor is null || _session is not { Busy: false } session) return;
        var travel = CardTravelArea(new Size(CardBounds.Width, CardBounds.Height));
        var x = Math.Clamp(point.X, travel.Left, travel.Right);
        var y = Math.Clamp(point.Y, travel.Top, travel.Bottom);
        // Moving the card only clears the view. Saving is a separate authored change.
        if (Math.Abs(x - CardBounds.X) < 0.01 && Math.Abs(y - CardBounds.Y) < 0.01) return;
        _temporaryCardPosition = new Point(travel.Width > 0 ? (x - travel.Left) / travel.Width : 0.5,
            travel.Height > 0 ? (y - travel.Top) / travel.Height : 1);
        UpdateGeometry(session);
    }

    private void OnCardMoveKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (_editor is not { } editor || _session is not { Busy: false } ||
            args.Key is not (VirtualKey.Left or VirtualKey.Right or VirtualKey.Up or VirtualKey.Down)) return;
        editor.CancelGesture();
        var shift = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;
        var distance = shift ? 10d : 1d;
        MoveCardTo(new Point(CardBounds.X + (args.Key == VirtualKey.Left ? -distance : args.Key == VirtualKey.Right ? distance : 0),
            CardBounds.Y + (args.Key == VirtualKey.Up ? -distance : args.Key == VirtualKey.Down ? distance : 0)));
        args.Handled = true;
    }

    private void ApplyCardPositionPreview()
    {
        _configuredCardBounds = CardBounds;
        if (_editor is not null && _temporaryCardPosition is { } position)
        {
            var travel = CardTravelArea(new Size(CardBounds.Width, CardBounds.Height));
            CardBounds = new Rect(travel.X + position.X * travel.Width, travel.Y + position.Y * travel.Height,
                CardBounds.Width, CardBounds.Height);
        }
        _intendedCardBounds = CardBounds;
        CardBounds = AvoidEditorInspector(CardBounds);
        _cardSave.IsEnabled = _session is { Editing: true, Busy: false } && CardPositionChanged;
    }

    private void OnCardSave(object sender, RoutedEventArgs args)
    {
        if (_editor is not { } editor || _session is not { Busy: false } ||
            _temporaryCardPosition is not { } position || !CardPositionChanged) return;
        ClearCardPositionPreview();
        editor.SetCardPosition(position.X, position.Y);
    }

    internal void CancelCardDrag()
    {
        if (_cardDrag is not { } drag) return;
        _temporaryCardPosition = drag.PreviousPosition;
        ReleaseCardDrag();
        if (_session is { Busy: false } session) UpdateGeometry(session);
    }

    internal void ClearCardPositionPreview()
    {
        ReleaseCardDrag();
        _temporaryCardPosition = null;
        _cardSave.IsEnabled = false;
    }

    private void ReleaseCardDrag()
    {
        if (_cardDrag is not { } drag) return;
        _cardDrag = null;
        _cardMove.ReleasePointerCapture(drag.Pointer);
    }
}
