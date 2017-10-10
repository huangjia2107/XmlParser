using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace XmlParser
{ 

    public class XmlParseValueDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;

            if (element != null && item != null && item is XmlParseNode)
            {
                var xpd = item as XmlParseNode;

                if (xpd.ValueType != null)
                {
                    if (xpd.ValueType == typeof(bool))
                        return Application.Current.Resources["XmlParseBoolDataTemplate"] as DataTemplate;
                    else if (xpd.ValueType.BaseType == typeof(Enum))
                        return Application.Current.Resources["XmlParseEnumDataTemplate"] as DataTemplate;
                }

                return Application.Current.Resources["XmlParseStringDataTemplate"] as DataTemplate;
            }

            return null;
        }
    }
}
