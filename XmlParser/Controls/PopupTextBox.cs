using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using XmlParser.Helps;
using System.Windows.Controls.Primitives;
using XmlParser.Models;

namespace XmlParser.Controls
{
    [TemplatePart(Name = PopupTemplateName, Type = typeof(Popup))]
    [TemplatePart(Name = TextBoxTemplateName, Type = typeof(TextBox))]
    public class PopupTextBox : Control
    {
        private const string PopupTemplateName = "PART_Popup";
        private const string TextBoxTemplateName = "PART_TextBox";

        private Popup _popup;
        private TextBox _textBox;

        private bool _IsManual = false;

        static PopupTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PopupTextBox), new FrameworkPropertyMetadata(typeof(PopupTextBox)));
        }

        public static readonly RoutedEvent TextChangedEvent = EventManager.RegisterRoutedEvent("TextChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<string>), typeof(PopupTextBox));
        public event RoutedPropertyChangedEventHandler<string> TextChanged
        {
            add { AddHandler(TextChangedEvent, value); }
            remove { RemoveHandler(TextChangedEvent, value); }
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(PopupTextBox), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, TextPropertyChanged));
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        static void TextPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var popupTextBox = sender as PopupTextBox;
            popupTextBox.OnTextChanged((string)e.OldValue, (string)e.NewValue);
        }

        public static readonly DependencyProperty TextAlignmentProperty = TextBlock.TextAlignmentProperty.AddOwner(typeof(PopupTextBox));
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        public static readonly DependencyProperty TextTrimmingProperty = TextBlock.TextTrimmingProperty.AddOwner(typeof(PopupTextBox), new PropertyMetadata(TextTrimming.CharacterEllipsis));
        public TextTrimming TextTrimming
        {
            get { return (TextTrimming)GetValue(TextTrimmingProperty); }
            set { SetValue(TextTrimmingProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _popup = base.GetTemplateChild(PopupTemplateName) as Popup;
            _textBox = base.GetTemplateChild(TextBoxTemplateName) as TextBox;

            _popup.Closed -= _popup_Closed;
            _popup.Closed += _popup_Closed;

            _popup.Opened -= _popup_Opened;
            _popup.Opened += _popup_Opened;

            _textBox.LostFocus -= _textBox_LostFocus;
            _textBox.LostFocus += _textBox_LostFocus;

            _textBox.PreviewKeyDown -= _textBox_PreviewKeyDown;
            _textBox.PreviewKeyDown += _textBox_PreviewKeyDown;

            OnTextChanged(Text, Text);
        }

        void _popup_Opened(object sender, EventArgs e)
        {
            var fromVisual = (HwndSource)PresentationSource.FromVisual(_textBox);

            if (fromVisual != null)
            {
                Win32.RECT rect;

                if (Win32.GetWindowRect(fromVisual.Handle, out rect))
                {
                    var topLeftToTargetPoint = this.PointFromScreen(new Point(rect.Left, rect.Top));

                    _textBox.Width = Math.Max(0, this.ActualWidth - Math.Abs(topLeftToTargetPoint.X));

                    if (topLeftToTargetPoint.Y < 0)
                        _popup.VerticalOffset += this.ActualHeight;
                    else
                        _popup.VerticalOffset -= this.ActualHeight;
                }
            }
        }

        void _popup_Closed(object sender, EventArgs e)
        {
            _textBox.Width = this.ActualWidth;
            _popup.VerticalOffset = 0;
        }

        private void _textBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_textBox != null && Text != _textBox.Text)
                {
                    _IsManual = true;
                    Text = _textBox.Text;
                }

                if (_popup != null)
                    _popup.IsOpen = false;
            }
        }

        private void _textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_textBox != null && Text != _textBox.Text)
            {
                _IsManual = true;
                Text = _textBox.Text;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (_popup == null)
                return;

            if (!_popup.IsOpen)
                _popup.IsOpen = true;

            if (_textBox != null && !_textBox.IsKeyboardFocusWithin)
            {
                _textBox.Focus();
                _textBox.SelectAll();

                e.Handled = true;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            e.Handled = true;
        }

        private void OnTextChanged(string oldString, string newString)
        {
            if (_textBox != null)
                _textBox.Text = newString;

            if (oldString != newString)
                RaiseEvent(new CommonTextChangedEventArgs<string>(oldString, newString, _IsManual, TextChangedEvent));

            _IsManual = false;
        }
    }
}
