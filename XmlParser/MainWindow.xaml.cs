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
        XmlParserHelper _xmlParserHelper = null; 
        Stack<List<XmlParseNode>> nodeStack = new Stack<List<XmlParseNode>>();

        public MainWindow()
        {
            InitializeComponent();

            _xmlParserHelper = new XmlParserHelper(true);
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            _xmlParserHelper.SaveXml("d:\\ss.xml");
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = "XML|*.xml";
            ofd.Multiselect = false;

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                 treeView.DataContext = _xmlParserHelper.TryParse(ofd.FileName, true, ParseNode); 
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchTextBlock.Text))
                _xmlParserHelper.FilterXmlParseNode(null);
            else
            {
                _xmlParserHelper.FilterXmlParseNode(nodeName =>
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

