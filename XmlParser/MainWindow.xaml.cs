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
using XmlHelps;
using System.IO;

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
        XmlHelps.XmlParser _xmlParserHelper = null;
        Stack<List<XmlParseNode>> nodeStack = new Stack<List<XmlParseNode>>();

        public MainWindow(string xmlPath)
        {
            InitializeComponent();

            _xmlParserHelper = new XmlHelps.XmlParser(true);

            if (!string.IsNullOrEmpty(xmlPath) && File.Exists(xmlPath) && System.IO.Path.GetExtension(xmlPath).ToLower() == ".xml")
                treeView.DataContext = _xmlParserHelper.TryParse(xmlPath, false, ParseNode);
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
                treeView.DataContext = _xmlParserHelper.TryParse(ofd.FileName, false, ParseNode);
            }
        }

        public List<Type> arrayList = new List<Type> { typeof(PrinterType), typeof(NPType), typeof(VisibilityType), typeof(SuppportType), typeof(Align), typeof(Unit), typeof(MBVersion) };

        private void ParseNode(XmlParseNode xmlParseNode)
        {
            if (xmlParseNode == null)
                return;

            bool isTrue = false;
            string value = xmlParseNode.Value.ToString();

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

            //Visibility
            if (xmlParseNode.OriginalName == "JobShowMode" || xmlParseNode.OriginalName == "AutoDetectZPrintPosition")
                xmlParseNode.IsVisible = false;

            //DisplayName
            if (xmlParseNode.OriginalName == "AfterPrintAdditionalHeat")
                xmlParseNode.DisplayName = "打印后额外加热";

            if (xmlParseNode.OriginalName == "PrinterID")
            {
                xmlParseNode.DisplayName = "打印机ID";
                xmlParseNode.Tag = "mm";
            }

            if (xmlParseNode.OriginalName == "VenderID")
            {
                xmlParseNode.DisplayName = "厂商ID";
                xmlParseNode.Tag = "mm";
            }
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

        private void ScrollViewer_Drop(object sender, DragEventArgs e)
        {

        }

        private void ScrollViewer_PreviewDrop(object sender, DragEventArgs e)
        {
            if (((int)(e.Effects) & (int)(DragDropEffects.Copy)) != 0)
            {
                string[] fileUrls = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (fileUrls != null)
                {
                    treeView.DataContext = _xmlParserHelper.TryParse(fileUrls[0], false, ParseNode);
                }

                e.Handled = true;
            }
        }
    }
}

