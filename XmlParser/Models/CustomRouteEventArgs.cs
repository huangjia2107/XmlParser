using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace XmlParser.Models
{
    public class CommonTextChangedEventArgs<T> : RoutedPropertyChangedEventArgs<T>
    {
        public bool IsManual { get; private set; }

        public CommonTextChangedEventArgs(T oldValue, T newValue, bool isManual)
            : base(oldValue, newValue)
        {
            IsManual = isManual;
        }

        public CommonTextChangedEventArgs(T oldValue, T newValue, bool isManual, RoutedEvent routedEvent)
            : base(oldValue, newValue, routedEvent)
        {
            IsManual = isManual;
        }
    }
}
