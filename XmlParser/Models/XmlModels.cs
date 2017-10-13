﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml;

namespace XmlParser
{
    public class XmlParseNode : ViewModelBase
    {
        #region public

        public string OriginalName { get; set; }

        public object Tag { get; set; }

        public string DisplayName { get; set; }

        public ObservableCollection<XmlParseNode> NodeCollection { get; set; }

        private bool _IsAttribute;
        public bool IsAttribute
        {
            get { return _IsAttribute; }
            set
            {
                _IsAttribute = value;
                HasValue = IsHasValue();
            }
        }

        private XmlNode _XmlNode;
        public XmlNode XmlNode
        {
            get { return _XmlNode; }
            set
            {
                _XmlNode = value;
                IsRoot = IsRootNode();
                HasValue = IsHasValue();
            }
        }

        private object _Value;
        public object Value
        {
            get { return _Value; }
            set
            {
                _Value = value;
                ValueType = _Value.GetType();

                SyncToXmlNode();
                InvokePropertyChanged("Value");
            }
        }

        private bool _IsVisible = true;
        public bool IsVisible
        {
            get { return _IsVisible; }
            set { _IsVisible = value; InvokePropertyChanged("IsVisible"); }
        }

        private bool _IsFilterVisible = true;
        public bool IsFilterVisible
        {
            get { return _IsFilterVisible; }
            set { _IsFilterVisible = value; InvokePropertyChanged("IsFilterVisible"); }
        }

        private bool _IsExpanded;
        public bool IsExpanded
        {
            get { return _IsExpanded; }
            set { _IsExpanded = value; InvokePropertyChanged("IsExpanded"); }
        }

        #endregion

        #region protected

        private bool _HasValue;
        public bool HasValue
        {
            get { return _HasValue; }
            protected set
            {
                _HasValue = value;
                InvokePropertyChanged("HasValue");
            }
        }

        private Type _ValueType;
        public Type ValueType
        {
            get { return _ValueType; }
            protected set
            {
                if (_ValueType != value)
                {
                    _ValueType = value;
                    InvokePropertyChanged("ValueType");
                }
            }
        }

        private bool _IsRoot;
        public bool IsRoot
        {
            get { return _IsRoot; }
            protected set { _IsRoot = value; InvokePropertyChanged("IsRoot"); }
        }

        #endregion

        #region func

        private bool IsHasValue()
        {
            if (XmlNode == null)
                return false;

            if (IsAttribute)
                return true;
            else
            {
                /* <Node>Text</Node> */
                return XmlNode.HasChildNodes && XmlNode.ChildNodes.Count == 1 && XmlNode.FirstChild.NodeType == XmlNodeType.Text;
            }
        }

        private bool IsRootNode()
        {
            if (XmlNode == null)
                return false;

            return XmlNode.NodeType == XmlNodeType.Element
                && XmlNode.ParentNode is XmlDocument
                && XmlNode.ParentNode != null
                && XmlNode.ParentNode.NodeType == XmlNodeType.Document
                && XmlNode.ParentNode.ParentNode == null;
        }

        private bool SyncToXmlNode()
        {
            if (XmlNode == null)
                return false;

            if (IsAttribute)
            {
                var attribute = XmlNode.Attributes[OriginalName];
                if (attribute != null && attribute.Name == OriginalName)
                {
                    attribute.Value = (Value ?? string.Empty).ToString();
                    return true;
                }
            }
            else
            {
                /* <Node>Text</Node> */
                if (XmlNode.HasChildNodes && XmlNode.ChildNodes.Count == 1 && XmlNode.FirstChild.NodeType == XmlNodeType.Text)
                {
                    if (OriginalName == XmlNode.Name)
                    {
                        XmlNode.FirstChild.Value = (Value ?? string.Empty).ToString();
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion
    }

    public class XmlParse : ViewModelBase
    {
        private bool _FilterChanged;
        public bool FilterChanged
        {
            get { return _FilterChanged; }
            set
            {
                _FilterChanged = value;
                InvokePropertyChanged("FilterChanged");
            }
        }

        public ObservableCollection<XmlParseNode> NodeCollection { get; set; }
    }
}
