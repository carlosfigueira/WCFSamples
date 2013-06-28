using System;
using System.Windows.Forms;
using System.Xml;
using BinaryFormatParser;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

namespace WcfBinaryViewer
{
    public partial class XmlBinaryViewer : Form
    {
        private IXmlDictionary staticDictionary = WcfDictionary.GetStaticDictionary();
        private XmlBinaryReaderSession readerSession = null;
        public XmlBinaryViewer()
        {
            InitializeComponent();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void useWCFStaticDictionaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.staticDictionary = WcfDictionary.GetStaticDictionary();
        }

        private void useCustomStaticDictionaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string[] allLines = this.ReadAllLinesFromDialog();
            if (allLines != null)
            {
                XmlDictionary temp = new XmlDictionary();
                this.staticDictionary = temp;
                foreach (string line in allLines)
                {
                    temp.Add(line);
                }
            }
        }

        private string[] ReadAllLinesFromDialog()
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                List<string> result = new List<string>();
                using (Stream fs = this.openFileDialog1.OpenFile())
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            result.Add(sr.ReadLine());
                        }
                    }
                }

                return result.ToArray();
            }
            else
            {
                return null;
            }
        }

        private void loadDynamicDictionaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string[] allLines = this.ReadAllLinesFromDialog();
            if (allLines != null)
            {
                this.readerSession = new XmlBinaryReaderSession();
                for (int i = 0; i < allLines.Length; i++)
                {
                    this.readerSession.Add(i, allLines[i]);
                }
            }
        }

        private void unloadStaticDictionaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.staticDictionary = null;
        }

        private void unloadDynamicDictionaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.readerSession = null;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                byte[] xmlDoc = null;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (Stream fileStream = this.openFileDialog1.OpenFile())
                    {
                        fileStream.CopyTo(ms);
                        xmlDoc = ms.ToArray();
                    }
                }

                XmlBinaryParser parser = new XmlBinaryParser(xmlDoc);
                XmlBinaryNode binaryNode = parser.RootNode;
                this.PopulateTreeView(binaryNode);
            }
        }

        private void PopulateTreeView(XmlBinaryNode binaryNode)
        {
            TreeNode rootNode = this.tvBinaryFile.Nodes.Add(binaryNode.ToString(this.staticDictionary, this.readerSession, false));
            foreach (XmlBinaryNode child in binaryNode.Children)
            {
                AddTreeNode(rootNode, child);
            }
        }

        private void AddTreeNode(TreeNode rootNode, XmlBinaryNode node)
        {
            TreeNode childNode = rootNode.Nodes.Add(node.ToString(this.staticDictionary, this.readerSession, false));
            foreach (XmlBinaryNode child in node.Children)
            {
                this.AddTreeNode(childNode, child);
            }

            if (node.NodeType == XmlBinaryNodeType.Array)
            {
                Array elements = null;
                BinaryArrayElements arrayElements = node.ArrayElements;
                byte nodeType = arrayElements.NodeType;
                string nodeTypeName = XmlBinaryNodeType.GetNodeName((byte)(nodeType & 0xFE));
                switch (arrayElements.NodeType)
                {
                    case XmlBinaryNodeType.Int16Text:
                    case XmlBinaryNodeType.Int16TextWithEndElement:
                        elements = arrayElements.ShortArray;
                        break;
                    case XmlBinaryNodeType.Int32Text:
                    case XmlBinaryNodeType.Int32TextWithEndElement:
                        elements = arrayElements.IntArray;
                        break;
                    case XmlBinaryNodeType.Int64Text:
                    case XmlBinaryNodeType.Int64TextWithEndElement:
                        elements = arrayElements.LongArray;
                        break;
                    case XmlBinaryNodeType.BoolText:
                    case XmlBinaryNodeType.BoolTextWithEndElement:
                        elements = arrayElements.BoolArray;
                        break;
                    case XmlBinaryNodeType.DateTimeText:
                    case XmlBinaryNodeType.DateTimeTextWithEndElement:
                        elements = arrayElements.DateTimeArray;
                        break;
                    case XmlBinaryNodeType.DecimalText:
                    case XmlBinaryNodeType.DecimalTextWithEndElement:
                        elements = arrayElements.DecimalArray;
                        break;
                    case XmlBinaryNodeType.DoubleText:
                    case XmlBinaryNodeType.DoubleTextWithEndElement:
                        elements = arrayElements.DoubleArray;
                        break;
                    case XmlBinaryNodeType.FloatText:
                    case XmlBinaryNodeType.FloatTextWithEndElement:
                        elements = arrayElements.FloatArray;
                        break;
                    case XmlBinaryNodeType.GuidText:
                    case XmlBinaryNodeType.GuidTextWithEndElement:
                        elements = arrayElements.GuidArray;
                        break;
                    case XmlBinaryNodeType.TimeSpanText:
                    case XmlBinaryNodeType.TimeSpanTextWithEndElement:
                        elements = arrayElements.TimeSpanArray;
                        break;
                }

                if (elements != null)
                {
                    if (nodeTypeName.EndsWith("Text"))
                    {
                        nodeTypeName = nodeTypeName.Substring(0, nodeTypeName.Length - 4);
                    }

                    foreach (var item in elements)
                    {
                        rootNode.Nodes.Add(string.Format(CultureInfo.InvariantCulture, "({0}) {1}", nodeTypeName, item));
                    }
                }
            }
        }
    }
}
