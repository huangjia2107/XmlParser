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
using System.Collections.ObjectModel;

namespace XmlParser
{
    enum PrinterType
    {
        Scanning,
        Onepass
    }

    enum NPType
    {
        NP_Degas,
        NP
    }

    enum VisibilityType
    {
        Hide,
        Show
    }

    enum SuppportType
    {
        Nonsupport,
        support
    }

    enum Align
    {
        Bottom,
        Top,
        Left,
        Right
    }

    enum Unit
    {
        UNITMODE_CM,
        UNITMODE_MM,
        UNITMODE_INCH,
        UNITMODE_FOOT,
    }

    enum MBVersion
    {
        Version4,
        Version5,
        Version6,
        Version7
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        XmlParserHelper _xmlParse = null;
        Stack<List<XmlParseNode>> nodeStack = new Stack<List<XmlParseNode>>();

        public MainWindow()
        {
            InitializeComponent();

            _xmlParse = new XmlParserHelper();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            _xmlParse.SaveXml("d:\\ss.xml");
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = "XML|*.xml";
            ofd.Multiselect = false;

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var parser = _xmlParse.TryParse(ofd.FileName, true, ParseNode);

                treeView.DataContext = parser;

                nodeStack.Clear();
                nodeStack.TrimExcess();
                ConstructTreeStack(parser.NodeCollection, new List<XmlParseNode>());
            }
        }

        public List<Type> arrayList = new List<Type> { typeof(PrinterType), typeof(NPType), typeof(VisibilityType), typeof(SuppportType), typeof(Align), typeof(Unit), typeof(MBVersion) };

        private XmlParseNode ParseNode(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            bool isTrue = false;

            var xmlParseNode = new XmlParseNode { Name = name };

            //检测null值
            if (string.IsNullOrEmpty(value))
                xmlParseNode.Value = "";
            else
            {
                //检测bool值
                if (bool.TryParse(value.ToLower(), out isTrue))
                    xmlParseNode.Value = isTrue;
                else
                {
                    //检测枚举值
                    object enumValue = null;
                    foreach (Type t in arrayList)
                    {
                        if (Enum.IsDefined(t, value))
                        {
                            object obj = Enum.Parse(t, value, true);
                            if (obj != null)
                            {
                                enumValue = obj;
                                break;
                            }
                        }
                    }

                    if (enumValue != null)
                        xmlParseNode.Value = enumValue;
                    else
                        xmlParseNode.Value = value;
                }
            }

            if (xmlParseNode.Name == "JobShowMode" || xmlParseNode.Name == "AutoDetectZPrintPosition")
                xmlParseNode.IsVisible = false;

            return xmlParseNode;
        }

        private void ConstructTreeStack(ObservableCollection<XmlParseNode> nodeCollection, List<XmlParseNode> nodeList)
        {
            if (nodeCollection != null && nodeCollection.Count > 0 && nodeList != null)
            {
                nodeStack.Push(nodeList);

                var childList = new List<XmlParseNode>();
                foreach (XmlParseNode node in nodeCollection)
                {
                    nodeList.Add(node);
                    ConstructTreeStack(node.NodeCollection, childList);
                }
            }
        } 

        /// <summary>  
        /// </summary>
        /// <param name="filter"></param>
        private void FilterNode(Predicate<string> filter)
        {
            foreach (List<XmlParseNode> nodeList in nodeStack)
            {
                nodeList.ForEach(node =>
                {
                    if (filter == null)
                        node.IsFilterVisible = true;
                    else
                    {
                        node.IsFilterVisible = filter(node.Name);

                        //父节点满足条件，则所有子节点都显示
                        //父节点不满足条件，则根据是否存在显示的子节点来决定
                        if (node.NodeCollection != null && node.NodeCollection.Count > 0)
                        {
                            if (node.IsFilterVisible)
                                UpdateChileNodeVisible(node.NodeCollection, true);
                            else
                                node.IsFilterVisible = node.NodeCollection.FirstOrDefault(n => n.IsFilterVisible) != null;
                        } 
                    }
                });
            }
        }

        private void UpdateChileNodeVisible(ObservableCollection<XmlParseNode> nodeCollection, bool isVisible)
        {
            if (nodeCollection == null || nodeCollection.Count == 0)
                return;

            foreach (var n in nodeCollection)
            {
                n.IsFilterVisible = isVisible;
                UpdateChileNodeVisible(n.NodeCollection, isVisible);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchTextBlock.Text))
                FilterNode(null);
            else
            {
                FilterNode(nodeName =>
                {
                    if (MatchCaseCheckBox.IsChecked == true && MatchWholeWodCheckBox.IsChecked == true)
                        return nodeName == SearchTextBlock.Text;

                    if (MatchCaseCheckBox.IsChecked == true)
                        return nodeName.Contains(SearchTextBlock.Text);

                    if (MatchWholeWodCheckBox.IsChecked == true)
                        return nodeName.ToLower() == SearchTextBlock.Text.ToLower();

                    return nodeName.ToLower().Contains(SearchTextBlock.Text.ToLower());
                });
            }
        }
    }
}

