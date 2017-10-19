using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections.ObjectModel;
using System.IO;

namespace XmlHelps
{
    public class XmlParser : IDisposable
    {
        private XmlDocument _xmlDocument;
        private XmlParse _xmlParse;
        private Stack<List<XmlParseNode>> FilterStack;

        public bool IsSupportFilter { get; private set; }

        public string BaseURI { get { return _xmlDocument == null ? null : _xmlDocument.BaseURI.Replace("file:///", ""); } }

        public XmlParser() { }

        public XmlParser(bool isSupportFilter)
        {
            IsSupportFilter = isSupportFilter;

            if (isSupportFilter)
                FilterStack = new Stack<List<XmlParseNode>>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isSort">节点排序</param>
        /// <param name="parseNode">自定义节点解析，可为null</param>
        /// <returns>XmlParse</returns>
        public XmlParse TryParse(string xmlPath, bool isSort, Action<XmlParseNode> parseNode)
        {
            _xmlParse = ParseXml(xmlPath, isSort, parseNode, ref _xmlDocument);

            if (IsSupportFilter && _xmlParse != null)
            {
                FilterStack.Clear();
                FilterStack.TrimExcess();

                ConstructFilterStack(_xmlParse.NodeCollection, new List<XmlParseNode>());
            }

            return _xmlParse;
        }

        #region Parse

        private XmlParse ParseXml(string xmlPath, bool isSort, Action<XmlParseNode> parseNode, ref XmlDocument xmlDocument)
        {
            if (!File.Exists(xmlPath))
                return null;

            try
            {
                xmlDocument = new XmlDocument();
                xmlDocument.Load(xmlPath);

                if (xmlDocument.HasChildNodes)
                {
                    var nodeList = new ObservableCollection<XmlParseNode>();

                    ParseXmlNodes(xmlDocument.ChildNodes, nodeList, isSort, parseNode);

                    if (nodeList.Count > 0)
                        return new XmlParse { NodeCollection = nodeList };
                }
            }
            catch
            {
                xmlDocument = null;
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlNodeList">Xml中子节点集合</param>
        /// /// <param name="nodeCollection">存储解析后的子节点集合</param>
        /// <param name="isSort">是否针对属性和节点进行排序</param>
        /// <param name="parseNode">自定义构造Attrribute或Node，一般用于决策数据类型或隐藏，参数为null，则所有Value按照字符串处理且均显示</param>
        private void ParseXmlNodes(XmlNodeList xmlNodeList, ObservableCollection<XmlParseNode> nodeCollection, bool isSort, Action<XmlParseNode> parseNode)
        {
            if (xmlNodeList == null || nodeCollection == null)
                return;

            foreach (XmlNode node in xmlNodeList)
            {
                //从有效节点开始
                if (node.NodeType != XmlNodeType.Element)
                    continue;

                var xmlParseNode = new XmlParseNode();

                if (node.HasChildNodes && node.ChildNodes.Count == 1 && node.FirstChild.NodeType == XmlNodeType.Text)
                {
                    /* <Node>Text</Node> */
                    ParseNode(node.Name, node.FirstChild.Value, parseNode, ref xmlParseNode);
                    xmlParseNode.XmlNode = node;
                }
                else
                {
                    ParseNode(node.Name, node.Value, parseNode, ref xmlParseNode);
                    xmlParseNode.XmlNode = node;

                    if (node.HasChildNodes && node.ChildNodes.Count > 0)
                    {
                        xmlParseNode.NodeCollection = new ObservableCollection<XmlParseNode>();
                        ParseXmlNodes(node.ChildNodes, xmlParseNode.NodeCollection, isSort, parseNode);
                    }
                }

                //解析 Attribute
                if (node.Attributes != null && node.Attributes.Count > 0)
                {
                    if (xmlParseNode.NodeCollection == null)
                        xmlParseNode.NodeCollection = new ObservableCollection<XmlParseNode>();

                    foreach (XmlAttribute attribute in node.Attributes)
                    {
                        var xmlParseAttributeNode = new XmlParseNode
                        {
                            IsAttribute = true,
                            XmlNode = attribute
                        };
                        ParseNode(attribute.Name, attribute.Value, parseNode, ref xmlParseAttributeNode);
                        xmlParseNode.NodeCollection.Add(xmlParseAttributeNode);
                    }
                }

                //排序
                if (xmlParseNode.NodeCollection != null)
                {
                    IOrderedEnumerable<XmlParseNode> orderedEnumerable = null;

                    if (!xmlParseNode.NodeCollection.All(n => n.IsAttribute))
                        orderedEnumerable = xmlParseNode.NodeCollection.OrderByDescending(n => n.IsAttribute);

                    if (isSort)
                    {
                        if (orderedEnumerable != null)
                            orderedEnumerable = orderedEnumerable.ThenBy(n => n.DisplayName);
                        else
                            orderedEnumerable = xmlParseNode.NodeCollection.OrderBy(n => n.DisplayName);
                    }

                    if (orderedEnumerable != null)
                        xmlParseNode.NodeCollection = new ObservableCollection<XmlParseNode>(orderedEnumerable);
                }

                nodeCollection.Add(xmlParseNode);
            }
        }

        private void ParseNode(string name, string value, Action<XmlParseNode> parseNode, ref XmlParseNode xmlParseNode)
        {
            if (xmlParseNode == null)
                xmlParseNode = new XmlParseNode();

            xmlParseNode.OriginalName = name;
            xmlParseNode.DisplayName = name;
            xmlParseNode.Value = string.IsNullOrEmpty(value) ? string.Empty : value;

            if (parseNode != null)
                parseNode(xmlParseNode);
        }

        #endregion

        #region Save

        public bool SaveXml(string savePath)
        {
            if (string.IsNullOrEmpty(savePath))
                throw new ArgumentNullException("savePath");

            if (_xmlDocument != null)
            {
                _xmlDocument.Save(savePath);
                return true;
            }

            return false;
        }

        #endregion

        #region Filter

        private void ConstructFilterStack(ObservableCollection<XmlParseNode> nodeCollection, List<XmlParseNode> nodeList)
        {
            if (nodeCollection != null && nodeCollection.Count > 0 && nodeList != null)
            {
                FilterStack.Push(nodeList);

                var childList = new List<XmlParseNode>();
                foreach (XmlParseNode node in nodeCollection)
                {
                    nodeList.Add(node);
                    ConstructFilterStack(node.NodeCollection, childList);
                }
            }
        }

        public void FilterXmlParseNode(Predicate<string> filter)
        {
            if (!IsSupportFilter || FilterStack == null || FilterStack.Count == 0)
                return;

            foreach (List<XmlParseNode> nodeList in FilterStack)
            {
                nodeList.ForEach(node =>
                {
                    if (filter == null)
                        node.IsFilterVisible = true;
                    else
                    {
                        node.IsFilterVisible = filter(node.DisplayName);

                        //父节点满足条件，则所有子节点都显示
                        //父节点不满足条件，则根据是否存在显示的子节点来决定
                        if (node.NodeCollection != null && node.NodeCollection.Count > 0)
                        {
                            if (node.IsFilterVisible)
                                UpdateChildNodeVisible(node.NodeCollection, true);
                            else
                                node.IsFilterVisible = node.NodeCollection.FirstOrDefault(n => n.IsFilterVisible) != null;
                        }
                    }
                });
            }

            RefreshFilterFlag();
        }

        private void RefreshFilterFlag()
        {
            if (_xmlParse != null)
                _xmlParse.FilterChanged = !_xmlParse.FilterChanged;
        }

        private void UpdateChildNodeVisible(ObservableCollection<XmlParseNode> nodeCollection, bool isVisible)
        {
            if (nodeCollection == null || nodeCollection.Count == 0)
                return;

            foreach (var n in nodeCollection)
            {
                n.IsFilterVisible = isVisible;
                UpdateChildNodeVisible(n.NodeCollection, isVisible);
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            _xmlDocument = null;
            _xmlParse = null;

            if (FilterStack != null)
            {
                FilterStack.Clear();
                FilterStack.TrimExcess();
                FilterStack = null;
            }

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
