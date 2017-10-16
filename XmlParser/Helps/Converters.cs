using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using XmlHelps;

namespace XmlParser
{
    public class ObjectToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || value == DependencyProperty.UnsetValue)
                return string.Empty;

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }



    public class BoolToVisibilityConverter : IValueConverter
    {
        public BoolToVisibilityConverter()
        {
            trueVisibility = Visibility.Visible;
            falseVisibility = Visibility.Collapsed;
        }

        public Visibility trueVisibility { get; set; }
        public Visibility falseVisibility { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isTrue = (bool)value;
            return isTrue ? trueVisibility : falseVisibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumTypeToItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Type t = (Type)value;
            return Enum.GetValues(t);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class XmlParseNodeCollectionToToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || value == DependencyProperty.UnsetValue)
                return null;

            ObservableCollection<XmlParseNode> xpdCollection = (ObservableCollection<XmlParseNode>)value;
            var attributeList = xpdCollection.Where(xpd => xpd.IsAttribute && xpd.IsVisible && xpd.IsFilterVisible);
            if (attributeList == null || xpdCollection.Count == 0)
                return null;

            string tooltip = string.Join("\n", attributeList.Select(xpd => string.Format("{0} = \"{1}\"", xpd.DisplayName, xpd.Value)).ToArray<string>());

            if (string.IsNullOrEmpty(tooltip))
                return null;

            return tooltip;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class OnlyOneTreeViewItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var item = (TreeViewItem)value;
            var ic = ItemsControl.ItemsControlFromItemContainer(item);
            return ic.Items.Count == 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }

    public class LastTreeViewItemMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Contains(null) || values.Contains(DependencyProperty.UnsetValue))
                return false;

            var item = (TreeViewItem)values[0];

            if (item.Visibility != Visibility.Visible)
                return false;

            var ic = ItemsControl.ItemsControlFromItemContainer(item);
            if (ic == null)
                return false;

            var curIndex = ic.ItemContainerGenerator.IndexFromContainer(item);

            int i = curIndex + 1;
            for (; i < ic.Items.Count; i++)
            {
                var curItem = (TreeViewItem)ic.ItemContainerGenerator.ContainerFromItem(ic.Items[0]);
                if (curItem.Visibility == Visibility.Visible)
                    return false;
            }

            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolAndBoolToVisibilityMultiConverter : IMultiValueConverter
    {
        public BoolAndBoolToVisibilityMultiConverter()
        {
            boolValue1 = true;
            boolValue2 = true;
        }

        public bool boolValue1 { get; set; }
        public bool boolValue2 { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Contains(null) || values.Contains(DependencyProperty.UnsetValue))
                return Visibility.Visible;

            bool _boolValue1 = (bool)values[0];
            bool _boolValue2 = (bool)values[1];

            Visibility v = (Visibility)(parameter ?? Visibility.Visible);

            if (_boolValue1 == boolValue1 && _boolValue2 == boolValue2)
                return v;
            else
                return v == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || value == DependencyProperty.UnsetValue)
                return Visibility.Collapsed;

            return string.IsNullOrEmpty(value.ToString()) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
