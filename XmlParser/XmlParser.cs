using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections.ObjectModel;
using System.IO;

namespace XmlParser
{
    public class XmlParserHelper : IDisposable
    {
        private XmlDocument _xmlDocument;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isSort">节点排序</param>
        /// <param name="parseNode">自定义节点解析，可为null</param>
        /// <returns>XmlParse</returns>
        public XmlParse TryParse(string xmlPath, bool isSort, Func<string, string, XmlParseNode> parseNode)
        {
            return ParseXml(xmlPath, isSort, parseNode, ref _xmlDocument);
        }

        #region Parse

        private XmlParse ParseXml(string xmlPath, bool isSort, Func<string, string, XmlParseNode> parseNode, ref XmlDocument xmlDocument)
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
        /// <param name="isSort">是否针对属性和节点进行排序</param>
        /// <param name="parseNode">自定义构造Attrribute或Node，一般用于决策数据类型或隐藏，参数为null，则所有Value按照字符串处理且均显示</param>
        /// <param name="nodeCollection">存储解析后的子节点集合</param>
        /// <param name="isExistNodeWithMultiAttribute">是否存在带有多个（>=1）属性的节点</param>
        private void ParseXmlNodes(XmlNodeList xmlNodeList, ObservableCollection<XmlParseNode> nodeCollection, bool isSort, Func<string, string, XmlParseNode> parseNode)
        {
            if (xmlNodeList == null || nodeCollection == null)
                return;

            foreach (XmlNode node in xmlNodeList)
            {
                //从有效节点开始
                if (node.NodeType != XmlNodeType.Element)
                    continue;

                XmlParseNode xmlParseNode = null;

                if (node.HasChildNodes && node.ChildNodes.Count == 1 && node.FirstChild.NodeType == XmlNodeType.Text)
                {
                    /* <Node>Text</Node> */
                    xmlParseNode = ParseNode(node.Name, node.FirstChild.Value, parseNode);
                    xmlParseNode.XmlNode = node;
                }
                else
                {
                    xmlParseNode = ParseNode(node.Name, node.Value, parseNode);
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
                        var xpd = ParseNode(attribute.Name, attribute.Value, parseNode);
                        if (xpd != null)
                        {
                            xpd.IsAttribute = true;
                            xpd.XmlNode = node;
                            xmlParseNode.NodeCollection.Add(xpd);
                        }
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
                            orderedEnumerable = orderedEnumerable.ThenBy(n => n.Name);
                        else
                            orderedEnumerable = xmlParseNode.NodeCollection.OrderBy(n => n.Name);
                    }

                    if (orderedEnumerable != null)
                        xmlParseNode.NodeCollection = new ObservableCollection<XmlParseNode>(orderedEnumerable);
                }

                nodeCollection.Add(xmlParseNode);
            }
        }

        private XmlParseNode ParseNode(string name, string value, Func<string, string, XmlParseNode> parseNode)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            return parseNode != null ? parseNode(name, value) : new XmlParseNode { Name = name, Value = value };
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

        #region IDisposable Members

        public void Dispose()
        {
            _xmlDocument = null;
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
