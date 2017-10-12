using System;
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

        public string Name { get; set; }
        public bool IsAttribute { get; set; }
        public ObservableCollection<XmlParseNode> NodeCollection { get; set; }

        private XmlNode _XmlNode;
        public XmlNode XmlNode
        {
            get { return _XmlNode; }
            set
            {
                _XmlNode = value;
                IsRoot = IsRootNode(value);
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
                HasValue = _Value != null;

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

        private bool IsRootNode(XmlNode node)
        {
            if (node == null)
                return false;

            return node.NodeType == XmlNodeType.Element
                && node.ParentNode is XmlDocument
                && node.ParentNode != null
                && node.ParentNode.NodeType == XmlNodeType.Document
                && node.ParentNode.ParentNode == null;
        }

        private bool SyncToXmlNode()
        {
            if (XmlNode == null)
                return false;

            if (IsAttribute)
            {
                var attribute = XmlNode.Attributes[Name];
                if (attribute != null && attribute.Name == Name)
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
                    if (Name == XmlNode.Name)
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

    public class XmlParse
    {
        public ObservableCollection<XmlParseNode> NodeCollection { get; set; }
    }
}
