using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections.ObjectModel;

namespace BinaryFormatParser
{
    public class XmlBinaryDictionaryString
    {
        public int DictionaryId { get; private set; }

        public XmlBinaryDictionaryString(int dictionaryId)
        {
            this.DictionaryId = dictionaryId;
        }

        public bool IsSession
        {
            get { return (this.DictionaryId % 2) == 1; }
        }

        public string GetValue(IXmlDictionary staticDictionary, XmlBinaryReaderSession readerSession)
        {
            int id = this.DictionaryId / 2;
            XmlDictionaryString dicString = XmlDictionaryString.Empty;
            bool found;
            if (this.IsSession)
            {
                if (readerSession == null)
                {
                    return null;
                }

                found = readerSession.TryLookup(id, out dicString);
            }
            else
            {
                if (staticDictionary == null)
                {
                    return null;
                }

                found = staticDictionary.TryLookup(id, out dicString);
            }

            if (found)
            {
                return dicString.Value;
            }
            else
            {
                throw new ArgumentException("Cannot find value for dictionary string with ID = " + this.DictionaryId);
            }
        }
    }

    public class XmlBinaryNode
    {
        internal byte nodeType;
        List<XmlBinaryNode> children;
        XmlBinaryNode parentNode;
        int depth;
        public string LocalName;
        public string Prefix;
        public string NamespaceURI;
        public XmlBinaryDictionaryString DictionaryLocalName;
        public XmlBinaryDictionaryString DictionaryNamespaceURI;
        public string Text;
        public byte[] Bytes;
        public int IntValue;
        public long LongValue;
        public float FloatValue;
        public double DoubleValue;
        public decimal DecimalValue;
        public DateTime DateTimeValue;
        public TimeSpan TimeSpanValue;
        public XmlBinaryDictionaryString DictionaryText;
        public Guid GuidValue;
        public UniqueId UniqueIdValue;
        public BinaryArrayElements ArrayElements;

        public byte NodeType
        {
            get { return nodeType; }
            set { this.nodeType = value; }
        }

        public string NodeTypeString
        {
            get { return XmlBinaryNodeType.GetNodeName(nodeType); }
        }

        public int Depth
        {
            get { return depth; }
        }

        public ReadOnlyCollection<XmlBinaryNode> Children
        {
            get { return this.children.AsReadOnly(); }
        }

        public void AddChild(XmlBinaryNode childNode)
        {
            this.children.Add(childNode);
            childNode.parentNode = this;
            childNode.depth = this.depth + 1;
        }

        public XmlBinaryNode(byte nodeType) : this(nodeType, 0) { }

        public XmlBinaryNode(byte nodeType, int depth)
        {
            this.nodeType = nodeType;
            this.depth = depth;
            this.children = new List<XmlBinaryNode>();
        }

        void ThrowIfMissingRequiredProperty(string propertyName, object propertyValue)
        {
            if (propertyValue == null)
            {
                throw new InvalidOperationException(
                    String.Format(
                        "Node type {0} (0x{1:X2}) needs the {2} property to be set",
                        XmlBinaryNodeType.GetNodeName(this.NodeType),
                        this.NodeType,
                        propertyName));
            }
        }

        public string ToString(IXmlDictionary staticDictionary, XmlBinaryReaderSession readerSession)
        {
            return this.ToString(staticDictionary, readerSession, true);
        }

        public string ToString(IXmlDictionary staticDictionary, XmlBinaryReaderSession readerSession, bool printChildren)
        {
            StringBuilder sb = new StringBuilder();
            int actualDepth = depth;
            if (nodeType == XmlBinaryNodeType.EndElement)
            {
                actualDepth--;
            }
            string identString = "  ";
            if (printChildren)
            {
                for (int i = 0; i < actualDepth; i++)
                {
                    sb.Append(identString);
                }
            }

            sb.Append(XmlBinaryNodeType.GetNodeName(nodeType));
            switch (nodeType)
            {
                case XmlBinaryNodeType.EndElement:
                    sb.Append(".");
                    break;
                case XmlBinaryNodeType.ShortElement:
                case XmlBinaryNodeType.ShortDictionaryElement:
                case XmlBinaryNodeType.Element:
                case XmlBinaryNodeType.DictionaryElement:
                case XmlBinaryNodeType.PrefixElementA:
                case XmlBinaryNodeType.PrefixElementB:
                case XmlBinaryNodeType.PrefixElementC:
                case XmlBinaryNodeType.PrefixElementD:
                case XmlBinaryNodeType.PrefixElementE:
                case XmlBinaryNodeType.PrefixElementF:
                case XmlBinaryNodeType.PrefixElementG:
                case XmlBinaryNodeType.PrefixElementH:
                case XmlBinaryNodeType.PrefixElementI:
                case XmlBinaryNodeType.PrefixElementJ:
                case XmlBinaryNodeType.PrefixElementK:
                case XmlBinaryNodeType.PrefixElementL:
                case XmlBinaryNodeType.PrefixElementM:
                case XmlBinaryNodeType.PrefixElementN:
                case XmlBinaryNodeType.PrefixElementO:
                case XmlBinaryNodeType.PrefixElementP:
                case XmlBinaryNodeType.PrefixElementQ:
                case XmlBinaryNodeType.PrefixElementR:
                case XmlBinaryNodeType.PrefixElementS:
                case XmlBinaryNodeType.PrefixElementT:
                case XmlBinaryNodeType.PrefixElementU:
                case XmlBinaryNodeType.PrefixElementV:
                case XmlBinaryNodeType.PrefixElementW:
                case XmlBinaryNodeType.PrefixElementX:
                case XmlBinaryNodeType.PrefixElementY:
                case XmlBinaryNodeType.PrefixElementZ:
                case XmlBinaryNodeType.PrefixDictionaryElementA:
                case XmlBinaryNodeType.PrefixDictionaryElementB:
                case XmlBinaryNodeType.PrefixDictionaryElementC:
                case XmlBinaryNodeType.PrefixDictionaryElementD:
                case XmlBinaryNodeType.PrefixDictionaryElementE:
                case XmlBinaryNodeType.PrefixDictionaryElementF:
                case XmlBinaryNodeType.PrefixDictionaryElementG:
                case XmlBinaryNodeType.PrefixDictionaryElementH:
                case XmlBinaryNodeType.PrefixDictionaryElementI:
                case XmlBinaryNodeType.PrefixDictionaryElementJ:
                case XmlBinaryNodeType.PrefixDictionaryElementK:
                case XmlBinaryNodeType.PrefixDictionaryElementL:
                case XmlBinaryNodeType.PrefixDictionaryElementM:
                case XmlBinaryNodeType.PrefixDictionaryElementN:
                case XmlBinaryNodeType.PrefixDictionaryElementO:
                case XmlBinaryNodeType.PrefixDictionaryElementP:
                case XmlBinaryNodeType.PrefixDictionaryElementQ:
                case XmlBinaryNodeType.PrefixDictionaryElementR:
                case XmlBinaryNodeType.PrefixDictionaryElementS:
                case XmlBinaryNodeType.PrefixDictionaryElementT:
                case XmlBinaryNodeType.PrefixDictionaryElementU:
                case XmlBinaryNodeType.PrefixDictionaryElementV:
                case XmlBinaryNodeType.PrefixDictionaryElementW:
                case XmlBinaryNodeType.PrefixDictionaryElementX:
                case XmlBinaryNodeType.PrefixDictionaryElementY:
                case XmlBinaryNodeType.PrefixDictionaryElementZ:
                    sb.Append(": ");
                    if (this.Prefix != null && !XmlBinaryNodeType.IsPrefixNode(this.NodeType))
                    {
                        sb.AppendFormat("{0}:", this.Prefix);
                    }
                    if (XmlBinaryNodeType.IsDictionaryElementNode(nodeType))
                    {
                        this.ThrowIfMissingRequiredProperty("DictionaryLocalName", this.DictionaryLocalName);
                        string value = this.DictionaryLocalName.GetValue(staticDictionary, readerSession);
                        sb.AppendFormat("dict[key={0}{1}{2}]",
                            this.DictionaryLocalName.DictionaryId,
                            this.DictionaryLocalName.IsSession ? " (session)" : "",
                            value == null ? "" : "value=" + value);
                    }
                    else
                    {
                        this.ThrowIfMissingRequiredProperty("LocalName", this.LocalName);
                        sb.Append(this.LocalName);
                    }
                    break;
                case XmlBinaryNodeType.EmptyText:
                case XmlBinaryNodeType.EmptyTextWithEndElement:
                case XmlBinaryNodeType.ZeroText:
                case XmlBinaryNodeType.ZeroTextWithEndElement:
                case XmlBinaryNodeType.OneText:
                case XmlBinaryNodeType.OneTextWithEndElement:
                case XmlBinaryNodeType.FalseText:
                case XmlBinaryNodeType.FalseTextWithEndElement:
                case XmlBinaryNodeType.TrueText:
                case XmlBinaryNodeType.TrueTextWithEndElement:
                case XmlBinaryNodeType.Chars8Text:
                case XmlBinaryNodeType.Chars8TextWithEndElement:
                case XmlBinaryNodeType.Chars16Text:
                case XmlBinaryNodeType.Chars16TextWithEndElement:
                case XmlBinaryNodeType.Chars32Text:
                case XmlBinaryNodeType.Chars32TextWithEndElement:
                case XmlBinaryNodeType.UnicodeChars8Text:
                case XmlBinaryNodeType.UnicodeChars8TextWithEndElement:
                case XmlBinaryNodeType.UnicodeChars16Text:
                case XmlBinaryNodeType.UnicodeChars16TextWithEndElement:
                case XmlBinaryNodeType.UnicodeChars32Text:
                case XmlBinaryNodeType.UnicodeChars32TextWithEndElement:
                case XmlBinaryNodeType.Comment:
                    this.ThrowIfMissingRequiredProperty("Text", this.Text);
                    sb.AppendFormat(" (length={0}): \"{1}\"", this.Text.Length, this.Text);
                    break;
                case XmlBinaryNodeType.DictionaryText:
                case XmlBinaryNodeType.DictionaryTextWithEndElement:
                    this.ThrowIfMissingRequiredProperty("DictionaryText", this.DictionaryText);
                    string dicTextValue = this.DictionaryText.GetValue(staticDictionary, readerSession);
                    sb.AppendFormat(": dict[key={0}{1}{2}]",
                            this.DictionaryText.DictionaryId,
                            this.DictionaryText.IsSession ? "(session)" : "",
                            dicTextValue == null ? "" : ",value=\"" + dicTextValue + "\"");
                    break;
                case XmlBinaryNodeType.Int8Text:
                case XmlBinaryNodeType.Int8TextWithEndElement:
                case XmlBinaryNodeType.Int16Text:
                case XmlBinaryNodeType.Int16TextWithEndElement:
                case XmlBinaryNodeType.Int32Text:
                case XmlBinaryNodeType.Int32TextWithEndElement:
                    sb.AppendFormat(": {0}", this.IntValue);
                    break;
                case XmlBinaryNodeType.Int64Text:
                case XmlBinaryNodeType.Int64TextWithEndElement:
                    sb.AppendFormat(": {0}", this.LongValue);
                    break;
                case XmlBinaryNodeType.UInt64Text:
                case XmlBinaryNodeType.UInt64TextWithEndElement:
                    sb.AppendFormat(": {0}", (ulong)this.LongValue);
                    break;
                case XmlBinaryNodeType.FloatText:
                case XmlBinaryNodeType.FloatTextWithEndElement:
                    sb.AppendFormat(": {0}", this.FloatValue);
                    break;
                case XmlBinaryNodeType.DoubleText:
                case XmlBinaryNodeType.DoubleTextWithEndElement:
                    sb.AppendFormat(": {0}", this.DoubleValue);
                    break;
                case XmlBinaryNodeType.DecimalText:
                case XmlBinaryNodeType.DecimalTextWithEndElement:
                    sb.AppendFormat(": {0}", this.DecimalValue);
                    break;
                case XmlBinaryNodeType.DateTimeText:
                case XmlBinaryNodeType.DateTimeTextWithEndElement:
                    sb.AppendFormat(": {0} ({1})", this.DateTimeValue, this.DateTimeValue.Kind);
                    break;
                case XmlBinaryNodeType.TimeSpanText:
                case XmlBinaryNodeType.TimeSpanTextWithEndElement:
                    sb.AppendFormat(": {0}", this.TimeSpanValue);
                    break;
                case XmlBinaryNodeType.Bytes8Text:
                case XmlBinaryNodeType.Bytes8TextWithEndElement:
                case XmlBinaryNodeType.Bytes16Text:
                case XmlBinaryNodeType.Bytes16TextWithEndElement:
                case XmlBinaryNodeType.Bytes32Text:
                case XmlBinaryNodeType.Bytes32TextWithEndElement:
                    this.ThrowIfMissingRequiredProperty("Bytes", this.Bytes);
                    sb.AppendFormat(" (size={0}): ", this.Bytes.Length);
                    sb.Append('{');
                    for (int i = 0; i < this.Bytes.Length; i++)
                    {
                        if (i != 0) sb.Append(',');
                        sb.AppendFormat("{0:X2}", this.Bytes[i]);
                    }
                    sb.Append('}');
                    break;
                case XmlBinaryNodeType.GuidText:
                case XmlBinaryNodeType.GuidTextWithEndElement:
                    sb.Append(": ");
                    sb.Append(this.GuidValue.ToString("B"));
                    break;
                case XmlBinaryNodeType.UniqueIdText:
                case XmlBinaryNodeType.UniqueIdTextWithEndElement:
                    sb.Append(": ");
                    this.ThrowIfMissingRequiredProperty("UniqueIdValue", this.UniqueIdValue);
                    sb.Append(this.UniqueIdValue.ToString());
                    break;
                case XmlBinaryNodeType.ShortAttribute:
                case XmlBinaryNodeType.ShortDictionaryAttribute:
                case XmlBinaryNodeType.Attribute:
                case XmlBinaryNodeType.DictionaryAttribute:
                case XmlBinaryNodeType.PrefixAttributeA:
                case XmlBinaryNodeType.PrefixAttributeB:
                case XmlBinaryNodeType.PrefixAttributeC:
                case XmlBinaryNodeType.PrefixAttributeD:
                case XmlBinaryNodeType.PrefixAttributeE:
                case XmlBinaryNodeType.PrefixAttributeF:
                case XmlBinaryNodeType.PrefixAttributeG:
                case XmlBinaryNodeType.PrefixAttributeH:
                case XmlBinaryNodeType.PrefixAttributeI:
                case XmlBinaryNodeType.PrefixAttributeJ:
                case XmlBinaryNodeType.PrefixAttributeK:
                case XmlBinaryNodeType.PrefixAttributeL:
                case XmlBinaryNodeType.PrefixAttributeM:
                case XmlBinaryNodeType.PrefixAttributeN:
                case XmlBinaryNodeType.PrefixAttributeO:
                case XmlBinaryNodeType.PrefixAttributeP:
                case XmlBinaryNodeType.PrefixAttributeQ:
                case XmlBinaryNodeType.PrefixAttributeR:
                case XmlBinaryNodeType.PrefixAttributeS:
                case XmlBinaryNodeType.PrefixAttributeT:
                case XmlBinaryNodeType.PrefixAttributeU:
                case XmlBinaryNodeType.PrefixAttributeV:
                case XmlBinaryNodeType.PrefixAttributeW:
                case XmlBinaryNodeType.PrefixAttributeX:
                case XmlBinaryNodeType.PrefixAttributeY:
                case XmlBinaryNodeType.PrefixAttributeZ:
                case XmlBinaryNodeType.PrefixDictionaryAttributeA:
                case XmlBinaryNodeType.PrefixDictionaryAttributeB:
                case XmlBinaryNodeType.PrefixDictionaryAttributeC:
                case XmlBinaryNodeType.PrefixDictionaryAttributeD:
                case XmlBinaryNodeType.PrefixDictionaryAttributeE:
                case XmlBinaryNodeType.PrefixDictionaryAttributeF:
                case XmlBinaryNodeType.PrefixDictionaryAttributeG:
                case XmlBinaryNodeType.PrefixDictionaryAttributeH:
                case XmlBinaryNodeType.PrefixDictionaryAttributeI:
                case XmlBinaryNodeType.PrefixDictionaryAttributeJ:
                case XmlBinaryNodeType.PrefixDictionaryAttributeK:
                case XmlBinaryNodeType.PrefixDictionaryAttributeL:
                case XmlBinaryNodeType.PrefixDictionaryAttributeM:
                case XmlBinaryNodeType.PrefixDictionaryAttributeN:
                case XmlBinaryNodeType.PrefixDictionaryAttributeO:
                case XmlBinaryNodeType.PrefixDictionaryAttributeP:
                case XmlBinaryNodeType.PrefixDictionaryAttributeQ:
                case XmlBinaryNodeType.PrefixDictionaryAttributeR:
                case XmlBinaryNodeType.PrefixDictionaryAttributeS:
                case XmlBinaryNodeType.PrefixDictionaryAttributeT:
                case XmlBinaryNodeType.PrefixDictionaryAttributeU:
                case XmlBinaryNodeType.PrefixDictionaryAttributeV:
                case XmlBinaryNodeType.PrefixDictionaryAttributeW:
                case XmlBinaryNodeType.PrefixDictionaryAttributeX:
                case XmlBinaryNodeType.PrefixDictionaryAttributeY:
                case XmlBinaryNodeType.PrefixDictionaryAttributeZ:
                    sb.Append(": ");
                    if (this.Prefix != null && !XmlBinaryNodeType.IsPrefixNode(this.NodeType))
                    {
                        sb.AppendFormat("{0}:", this.Prefix);
                    }
                    if (XmlBinaryNodeType.IsDictionaryAttributeNode(nodeType))
                    {
                        this.ThrowIfMissingRequiredProperty("DictionaryLocalName", this.DictionaryLocalName);
                        string prefixDictAttrValue = this.DictionaryLocalName.GetValue(staticDictionary, readerSession);
                        sb.AppendFormat("dict[key={0}{1}{2}]",
                            this.DictionaryLocalName.DictionaryId,
                            this.DictionaryLocalName.IsSession ? "(session)" : "",
                            prefixDictAttrValue == null ? "" : ",value=" + prefixDictAttrValue);
                    }
                    else
                    {
                        this.ThrowIfMissingRequiredProperty("LocalName", this.LocalName);
                        sb.Append(this.LocalName);
                    }
                    break;
                case XmlBinaryNodeType.ShortXmlnsAttribute:
                case XmlBinaryNodeType.XmlnsAttribute:
                case XmlBinaryNodeType.ShortDictionaryXmlnsAttribute:
                case XmlBinaryNodeType.DictionaryXmlnsAttribute:
                    sb.Append(": xmlns");
                    if (this.Prefix != null)
                    {
                        sb.Append(':');
                        sb.Append(this.Prefix);
                    }
                    sb.Append('=');
                    if (XmlBinaryNodeType.IsDictionaryAttributeNode(nodeType))
                    {
                        this.ThrowIfMissingRequiredProperty("DictionaryNamespaceURI", this.DictionaryNamespaceURI);
                        string namespaceUriDictValue = this.DictionaryNamespaceURI.GetValue(staticDictionary, readerSession);
                        sb.AppendFormat("dict[key={0}{1}{2}]",
                            this.DictionaryNamespaceURI.DictionaryId,
                            this.DictionaryNamespaceURI.IsSession ? "(session)" : "",
                            namespaceUriDictValue == null ? "" : ",value=" + namespaceUriDictValue);
                    }
                    else
                    {
                        sb.AppendFormat("\"{0}\"", this.NamespaceURI);
                    }
                    break;
                case XmlBinaryNodeType.Array:
                    sb.Append(':');
                    if (printChildren)
                    {
                        sb.Append(Environment.NewLine);
                        sb.Append(this.Children[0].ToString(staticDictionary, readerSession)); // element node
                        for (int i = 0; i < this.Children[0].Depth; i++) sb.Append(identString);
                        sb.Append(this.ArrayElements.ToString());
                        sb.Append(Environment.NewLine);
                        if (this.Children.Count > 1)  // also has an EndElement node
                        {
                            sb.Append(this.Children[1].ToString(staticDictionary, readerSession));
                        }
                    }
                    break;
                case XmlBinaryNodeType.StartListText:
                case XmlBinaryNodeType.StartListTextWithEndElement:
                    sb.Append(":");
                    break;
                case XmlBinaryNodeType.EndListText:
                case XmlBinaryNodeType.EndListTextWithEndElement:
                    sb.Append(".");
                    break;
                default:
                    throw new ArgumentException(String.Format("Error, the code is not ready to handle the type {0:X2} ({1}) yet", nodeType, XmlBinaryNodeType.GetNodeName(nodeType)));
            }

            if (nodeType != XmlBinaryNodeType.Array) // already taken care of
            {
                if (printChildren)
                {
                    sb.Append(Environment.NewLine);
                    for (int i = 0; i < children.Count; i++)
                    {
                        sb.Append(children[i].ToString(staticDictionary, readerSession));
                    }
                }
            }

            return sb.ToString();
        }
    }

    public class BinaryArrayElements
    {
        int elementCount;
        byte nodeType;
        public bool[] BoolArray;
        public DateTime[] DateTimeArray;
        public decimal[] DecimalArray;
        public double[] DoubleArray;
        public Guid[] GuidArray;
        public Int16[] ShortArray;
        public Int32[] IntArray;
        public Int64[] LongArray;
        public Single[] FloatArray;
        public TimeSpan[] TimeSpanArray;

        private bool allowInvalidNodeTypes = false;

        public bool AllowInvalidNodeTypes
        {
            get { return allowInvalidNodeTypes; }
            set { allowInvalidNodeTypes = value; }
        }

        public byte NodeType
        {
            get { return nodeType; }
            internal set { this.nodeType = value; }
        }

        public string NodeTypeString
        {
            get { return XmlBinaryNodeType.GetNodeName(nodeType); }
        }

        public int ElementCount
        {
            get { return elementCount; }
        }

        public BinaryArrayElements(byte nodeType, int count)
        {
            this.nodeType = nodeType;
            this.elementCount = count;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} (size={1}) [", XmlBinaryNodeType.GetNodeName(nodeType), elementCount);
            bool hasEndElement = XmlBinaryNodeType.IsTextNodeWithEndElement(nodeType);
            switch (nodeType)
            {
                case XmlBinaryNodeType.BoolText:
                case XmlBinaryNodeType.BoolTextWithEndElement:
                    for (int i = 0; i < BoolArray.Length; i++)
                    {
                        if (i != 0) sb.Append(',');
                        sb.Append(BoolArray[i]);
                    }
                    sb.Append(']');
                    break;
                case XmlBinaryNodeType.DateTimeText:
                case XmlBinaryNodeType.DateTimeTextWithEndElement:
                    for (int i = 0; i < DateTimeArray.Length; i++)
                    {
                        if (i != 0) sb.Append(", ");
                        sb.Append(DateTimeArray[i].ToString("u"));
                    }
                    sb.Append(']');
                    break;
                case XmlBinaryNodeType.DecimalText:
                case XmlBinaryNodeType.DecimalTextWithEndElement:
                    for (int i = 0; i < DecimalArray.Length; i++)
                    {
                        if (i != 0) sb.Append(", ");
                        sb.Append(DecimalArray[i]);
                    }
                    sb.Append(']');
                    break;
                case XmlBinaryNodeType.DoubleText:
                case XmlBinaryNodeType.DoubleTextWithEndElement:
                    for (int i = 0; i < DoubleArray.Length; i++)
                    {
                        if (i != 0) sb.Append(", ");
                        sb.Append(DoubleArray[i]);
                    }
                    sb.Append(']');
                    break;
                case XmlBinaryNodeType.GuidText:
                case XmlBinaryNodeType.GuidTextWithEndElement:
                    for (int i = 0; i < GuidArray.Length; i++)
                    {
                        if (i != 0) sb.Append(", ");
                        sb.Append(GuidArray[i].ToString("B"));
                    }
                    sb.Append(']');
                    break;
                case XmlBinaryNodeType.Int16Text:
                case XmlBinaryNodeType.Int16TextWithEndElement:
                    for (int i = 0; i < ShortArray.Length; i++)
                    {
                        if (i != 0) sb.Append(',');
                        sb.Append(ShortArray[i]);
                    }
                    sb.Append(']');
                    break;
                case XmlBinaryNodeType.Int32Text:
                case XmlBinaryNodeType.Int32TextWithEndElement:
                    for (int i = 0; i < IntArray.Length; i++)
                    {
                        if (i != 0) sb.Append(',');
                        sb.Append(IntArray[i]);
                    }
                    sb.Append(']');
                    break;
                case XmlBinaryNodeType.Int64Text:
                case XmlBinaryNodeType.Int64TextWithEndElement:
                    for (int i = 0; i < LongArray.Length; i++)
                    {
                        if (i != 0) sb.Append(',');
                        sb.Append(LongArray[i]);
                    }
                    sb.Append(']');
                    break;
                case XmlBinaryNodeType.FloatText:
                case XmlBinaryNodeType.FloatTextWithEndElement:
                    for (int i = 0; i < FloatArray.Length; i++)
                    {
                        if (i != 0) sb.Append(", ");
                        sb.Append(FloatArray[i]);
                    }
                    sb.Append(']');
                    break;
                case XmlBinaryNodeType.TimeSpanText:
                case XmlBinaryNodeType.TimeSpanTextWithEndElement:
                    for (int i = 0; i < TimeSpanArray.Length; i++)
                    {
                        if (i != 0) sb.Append(", ");
                        sb.Append(TimeSpanArray[i]);
                    }
                    break;
                default:
                    if (!allowInvalidNodeTypes)
                    {
                        throw new Exception(
                            String.Format("Don't know how to work with type {0:X2} ({1})",
                                nodeType, XmlBinaryNodeType.GetNodeName(nodeType)));
                    }
                    break;
            }
            return sb.ToString();
        }
    }
}
