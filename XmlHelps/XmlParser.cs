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
        private Stack<List<XmlParseNode>> _childToParentNodeStack;

        public bool IsSupportFilter { get; private set; }

        public string BaseURI { get { return _xmlDocument == null ? null : _xmlDocument.BaseURI.Replace("file:///", ""); } }

        public XmlParser()
            : this(false)
        { }

        public XmlParser(bool isSupportFilter)
        {
            IsSupportFilter = isSupportFilter;
            _childToParentNodeStack = new Stack<List<XmlParseNode>>();
        }

        /// <param name="isSort">节点排序</param>
        /// <param name="parseNode">自定义节点解析，可为null</param>
        /// <returns>XmlParse</returns>
        public XmlParse TryParse(Action<XmlDocument> loadXml, bool isSort, Action<XmlParseNode> parseNode)
        {
            if (loadXml == null)
                return null;

            _xmlParse = ParseXml(loadXml, isSort, parseNode, ref _xmlDocument);

            if (_xmlParse != null)
            {
                _childToParentNodeStack.Clear();
                _childToParentNodeStack.TrimExcess();

                ConstructC2PStack(_xmlParse.NodeCollection, new List<XmlParseNode>());
                UpdateXmlParseNodeVisible();
            }

            return _xmlParse;
        }

        #region Parse

        private XmlParse ParseXml(Action<XmlDocument> loadXml, bool isSort, Action<XmlParseNode> parseNode, ref XmlDocument xmlDocument)
        {
            if (loadXml == null)
                return null;

            try
            {
                xmlDocument = new XmlDocument();
                loadXml(xmlDocument);

                if (xmlDocument.HasChildNodes)
                {
                    var xmlparseNode = new XmlParseNode();
                    ParseXmlNodes(xmlparseNode, xmlDocument.ChildNodes, isSort, parseNode);

                    if (xmlparseNode.NodeCollection != null && xmlparseNode.NodeCollection.Count > 0)
                        return new XmlParse { NodeCollection = xmlparseNode.NodeCollection };
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
        /// <param name="parentNode">父节点</param>
        /// <param name="xmlNodeList">Xml子节点集合</param>
        /// <param name="isSort">是否针对属性和节点进行排序</param>
        /// <param name="parseNode">自定义构造Attrribute或Node，一般用于决策数据类型或隐藏，参数为null，则所有Value按照字符串处理且均显示</param>
        private void ParseXmlNodes(XmlParseNode parentNode, XmlNodeList xmlNodeList, bool isSort, Action<XmlParseNode> parseNode)
        {
            if (xmlNodeList == null || parentNode == null)
                return;

            if (parentNode.NodeCollection == null)
                parentNode.NodeCollection = new ObservableCollection<XmlParseNode>();

            foreach (XmlNode node in xmlNodeList)
            {
                //从有效节点开始
                if (node.NodeType != XmlNodeType.Element)
                    continue;

                var xmlParseNode = new XmlParseNode { XmlNode = node, Parent = parentNode };

                if (node.HasChildNodes && node.ChildNodes.Count == 1 && node.FirstChild.NodeType == XmlNodeType.Text)
                {
                    /* <Node>Text</Node> */
                    ParseNode(node.Name, node.FirstChild.Value, parseNode, ref xmlParseNode);
                }
                else
                {
                    ParseNode(node.Name, node.Value, parseNode, ref xmlParseNode);

                    if (node.HasChildNodes && node.ChildNodes.Count > 0)
                        ParseXmlNodes(xmlParseNode, node.ChildNodes, isSort, parseNode);
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
                            XmlNode = attribute,
                            Parent = xmlParseNode
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

                parentNode.NodeCollection.Add(xmlParseNode);
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

        /// <summary>
        /// 便于自下而上遍历节点
        /// </summary>
        /// <param name="nodeCollection"></param>
        /// <param name="nodeList"></param>
        private void ConstructC2PStack(ObservableCollection<XmlParseNode> nodeCollection, List<XmlParseNode> nodeList)
        {
            if (nodeCollection != null && nodeCollection.Count > 0 && nodeList != null)
            {
                _childToParentNodeStack.Push(nodeList);

                var childList = new List<XmlParseNode>();
                foreach (XmlParseNode node in nodeCollection)
                {
                    nodeList.Add(node);
                    ConstructC2PStack(node.NodeCollection, childList);
                }
            }
        }

        /// <summary>
        /// 若当前节点可见：
        /// 1.该节点以上的父节点均得可见
        /// 2.若该节点所有子节点均不可见，则该节点以下的子节点均得可见
        /// </summary>
        private void UpdateXmlParseNodeVisible()
        {
            if (_childToParentNodeStack == null || _childToParentNodeStack.Count == 0)
                return;

            foreach (List<XmlParseNode> nodeList in _childToParentNodeStack)
            {
                nodeList.ForEach(node =>
                {
                    if (node.IsVisible)
                    {
                        node.UpdateParentVisible(true);

                        if (node.NodeCollection != null && node.NodeCollection.Count > 0)
                        {
                            if (node.NodeCollection.FirstOrDefault(n => n.IsVisible) == null)
                                UpdateAllChildNodeAction(node.NodeCollection, n => n.IsVisible = true);
                        }
                    }
                });
            }
        }

        private void UpdateAllChildNodeAction(ObservableCollection<XmlParseNode> nodeCollection, Action<XmlParseNode> action)
        {
            if (nodeCollection == null || nodeCollection.Count == 0)
                return;

            foreach (var n in nodeCollection)
            {
                if (action != null)
                    action(n);

                UpdateAllChildNodeAction(n.NodeCollection, action);
            }
        }

        #endregion

        #region Save

        public bool SaveXml(Action<XmlDocument> saveXml)
        {
            if (saveXml == null)
                throw new ArgumentNullException("saveXml");

            if (_xmlDocument != null)
            {
                saveXml(_xmlDocument);
                return true;
            }

            return false;
        }

        #endregion

        #region Filter

        public void FilterXmlParseNode(Predicate<string> filter)
        {
            if (!IsSupportFilter || _childToParentNodeStack == null || _childToParentNodeStack.Count == 0)
                return;

            foreach (List<XmlParseNode> nodeList in _childToParentNodeStack)
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
                                UpdateAllChildNodeAction(node.NodeCollection, n => n.IsFilterVisible = true);
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

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            _xmlDocument = null;
            _xmlParse = null;

            if (_childToParentNodeStack != null)
            {
                _childToParentNodeStack.Clear();
                _childToParentNodeStack.TrimExcess();
                _childToParentNodeStack = null;
            }

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
