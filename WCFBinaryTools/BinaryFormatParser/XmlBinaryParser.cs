using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace BinaryFormatParser
{
    public class XmlBinaryParser
    {
        private byte[] xmlDoc;
        private int offset;
        private int depth;
        private XmlBinaryNode root;

        private static readonly Encoding textEncoding = Encoding.UTF8;
        private static readonly Encoding unicodeEncoding = Encoding.Unicode;

        public XmlBinaryNode RootNode
        {
            get { return root; }
        }

        public XmlBinaryParser(byte[] xmlDoc)
        {
            this.xmlDoc = xmlDoc;
            this.offset = 0;
            this.depth = 0;
            Parse(ref root);
            if (depth != 0)
            {
                ThrowIncorrectDepthException("After reading all the document", 0);
            }
        }

        public static int ReadMB32(byte[] buffer, int offset, int count)
        {
            int result = 0;
            if (count <= 0) throw new ArgumentException();
            int toShift = 0;
            for (int i = 0; i < 5; i++, toShift += 7)
            {
                if (count <= i) throw new ArgumentException();
                byte temp = buffer[offset + i];
                bool finish = ((temp & 0x80) == 0);
                temp = (byte)(temp & 0x7F);
                result |= (temp << toShift);
                if (finish)
                {
                    break;
                }
            }
            return result;
        }

        private void ThrowOffsetException(String message)
        {
            throw new Exception(String.Format("{0}, offset ({1}) greater than xmlDoc.Length ({1})", message, offset, xmlDoc.Length));
        }

        private void ThrowIncorrectDepthException(String message, int expectedDepth)
        {
            throw new Exception(String.Format("{0}, depth at offset {1} should be {2}, but it is {3}", message, offset, depth, expectedDepth));
        }

        private int ReadMB32()
        {
            int count = 1;
            int originalOffset = offset;
            while ((xmlDoc[offset] & 0x80) != 0)
            {
                count++;
                offset++;
                if (offset >= xmlDoc.Length)
                {
                    ThrowOffsetException("Error reading MB32");
                }
            }
            int result = ReadMB32(xmlDoc, originalOffset, count);
            offset++;
            return result;
        }

        private ulong ReadUInt64()
        {
            return (ulong)ReadLong(8);
        }

        private int ReadInt(int numberOfBytes)
        {
            return (int)ReadLong(numberOfBytes);
        }

        private long ReadLong()
        {
            return ReadLong(8);
        }

        private long ReadLong(int numberOfBytes)
        {
            if (offset + numberOfBytes > xmlDoc.Length)
            {
                ThrowOffsetException("Error reading integer value");
            }
            long result = 0;
            for (int i = 0; i < numberOfBytes; i++)
            {
                long temp = (long)((uint)xmlDoc[offset]);
                result |= temp << (i * 8);
                offset++;
            }
            return result;
        }

        private DateTime ReadDateTime()
        {
            ulong temp = ReadUInt64();
            DateTimeKind kind;
            DateTime result;
            if ((temp & 0x8000000000000000) > 0)
            {
                 // local
                kind = DateTimeKind.Local;
            }
            else if ((temp & 0x4000000000000000) > 0)
            {
                kind = DateTimeKind.Utc;
            }
            else
            {
                kind = DateTimeKind.Unspecified;
            }
            long ticks = (long)temp & 0x3FFFFFFFFFFFFFFF;

            if (kind == DateTimeKind.Local)
            {
                result = new DateTime(ticks, DateTimeKind.Utc).ToLocalTime();
            }
            else
            {
                result = new DateTime(ticks, kind);
            }
            return result;
        }

        private string ReadString(int stringLenSizeInBits)
        {
            int stringSize;
            if (stringLenSizeInBits <= 0)
            {
                stringSize = ReadMB32();
            }
            else
            {
                switch (stringLenSizeInBits)
                {
                    case 8:
                    case 16:
                    case 32:
                        stringSize = ReadInt(stringLenSizeInBits / 8);
                        break;
                    default:
                        throw new ArgumentException("Unknown string len size: " + stringLenSizeInBits);
                }
            }
            if ((offset + stringSize) > xmlDoc.Length)
            {
                ThrowOffsetException("Error reading string");
            }
            string result = textEncoding.GetString(xmlDoc, offset, stringSize);
            offset += stringSize;
            return result;
        }

        private float ReadFloat()
        {
            int sizeToAdvance = sizeof(float);
            if ((offset + sizeToAdvance) > xmlDoc.Length)
            {
                ThrowOffsetException("Error reading float");
            }
            float result;
            uint temp = (uint)ReadLong(4);

            bool negative = (temp & 0x80000000) > 0;
            temp = temp & 0x7FFFFFFF;

            int exponent = (int)(temp & 0x7F800000);
            exponent = exponent >> 23;
            int significand = (int)(temp & 0x007FFFFF);
            float significandDecimal = (float)significand;
            for (int i = 0; i < 23; i++)
            {
                significandDecimal = significandDecimal / 2;
            }

            if (exponent == 255)
            {
                if (significand != 0)
                {
                    return float.NaN;
                }
                else
                {
                    if (negative)
                    {
                        return float.NegativeInfinity;
                    }
                    else
                    {
                        return float.PositiveInfinity;
                    }
                }
            }
            else if (exponent == 0)
            {
                if (significand == 0)
                {
                    if (negative)
                    {
                        return -0f;
                    }
                    else
                    {
                        return 0f;
                    }
                }
                else
                {
                    result = significandDecimal;
                    for (int i = 0; i < 126; i++)
                    {
                        result /= 2;
                    }
                }
            }
            else
            {
                result = 1 + significandDecimal;
                exponent -= 127;
                while (exponent > 0)
                {
                    exponent--;
                    result *= 2;
                }
                while (exponent < 0)
                {
                    exponent++;
                    result /= 2;
                }
            }
            if (negative)
            {
                result = -result;
            }
            return result;
        }

        private double ReadDouble()
        {
            int sizeToAdvance = sizeof(double);
            if ((offset + sizeToAdvance) > xmlDoc.Length)
            {
                ThrowOffsetException("Error reading double");
            }
            double result = 0;
            ulong temp = ReadUInt64();

            bool negative = (temp & 0x8000000000000000) > 0;
            temp = temp & 0x7FFFFFFFFFFFFFFF;

            long exponent = (long)(temp & 0x7FF0000000000000);
            exponent = exponent >> 52;
            long significand = (long)(temp & 0x000FFFFFFFFFFFFF);
            double significandDecimal = (double)significand;
            for (int i = 0; i < 52; i++)
            {
                significandDecimal = significandDecimal / 2;
            }

            if (exponent == 2047)
            {
                if (significand != 0)
                {
                    return double.NaN;
                }
                else
                {
                    if (negative)
                    {
                        return double.NegativeInfinity;
                    }
                    else
                    {
                        return double.PositiveInfinity;
                    }
                }
            }
            else if (exponent == 0)
            {
                if (significand == 0)
                {
                    if (negative)
                    {
                        return -0.0;
                    }
                    else
                    {
                        return 0.0;
                    }
                }
                else
                {
                    result = significandDecimal;
                    for (int i = 0; i < 1022; i++)
                    {
                        result /= 2;
                    }
                }
            }
            else
            {
                result = 1 + significandDecimal;
                exponent -= 1023;
                while (exponent > 0)
                {
                    exponent--;
                    result *= 2;
                }
                while (exponent < 0)
                {
                    exponent++;
                    result /= 2;
                }
            }
            if (negative)
            {
                result = -result;
            }
            return result;
        }

        private decimal ReadDecimal()
        {
            int sizeToAdvance = sizeof(decimal);
            if ((offset + sizeToAdvance) > xmlDoc.Length)
            {
                ThrowOffsetException("Error reading double");
            }
            decimal result;
            int signAndExponent = this.ReadInt(4);
            int high = this.ReadInt(4);
            int low = this.ReadInt(4);
            int mid = this.ReadInt(4);
            bool negative = false;
            if (signAndExponent < 0)
            {
                signAndExponent = -signAndExponent;
                negative = true;
            }
            signAndExponent = signAndExponent >> 16;
            if (signAndExponent > 28)
            {
                signAndExponent = 28;
            }
            result = new decimal(low, mid, high, negative, (byte)signAndExponent);
            return result;
        }

        private Guid ReadGuid()
        {
            byte[] guidBytes = new byte[16];
            if ((offset + guidBytes.Length) > xmlDoc.Length)
            {
                ThrowOffsetException("Error reading Guid");
            }
            Buffer.BlockCopy(xmlDoc, offset, guidBytes, 0, guidBytes.Length);
            Guid result = new Guid(guidBytes);
            offset += guidBytes.Length;
            return result;
        }

        private XmlBinaryDictionaryString ReadDictionaryString()
        {
            int dictionaryId = ReadMB32();
            return new XmlBinaryDictionaryString(dictionaryId);
        }

        private void ParseContentNode(XmlBinaryNode parent)
        {
            byte nodeType = xmlDoc[offset];
            offset++;
            XmlBinaryNode newNode = new XmlBinaryNode(nodeType, depth);
            parent.AddChild(newNode);
            switch (nodeType)
            {
                case XmlBinaryNodeType.Chars8Text:
                case XmlBinaryNodeType.Chars8TextWithEndElement:
                    newNode.Text = ReadString(8);
                    break;
                case XmlBinaryNodeType.Chars16Text:
                case XmlBinaryNodeType.Chars16TextWithEndElement:
                    newNode.Text = ReadString(16);
                    break;
                case XmlBinaryNodeType.Chars32Text:
                case XmlBinaryNodeType.Chars32TextWithEndElement:
                    newNode.Text = ReadString(32);
                    break;
                case XmlBinaryNodeType.UnicodeChars8Text:
                case XmlBinaryNodeType.UnicodeChars8TextWithEndElement:
                case XmlBinaryNodeType.UnicodeChars16Text:
                case XmlBinaryNodeType.UnicodeChars16TextWithEndElement:
                case XmlBinaryNodeType.UnicodeChars32Text:
                case XmlBinaryNodeType.UnicodeChars32TextWithEndElement:
                    {
                        int unicodeTextLenSize = 0;
                        switch (nodeType)
                        {
                            case XmlBinaryNodeType.UnicodeChars8Text:
                            case XmlBinaryNodeType.UnicodeChars8TextWithEndElement:
                                unicodeTextLenSize = 1;
                                break;
                            case XmlBinaryNodeType.UnicodeChars16Text:
                            case XmlBinaryNodeType.UnicodeChars16TextWithEndElement:
                                unicodeTextLenSize = 2;
                                break;
                            case XmlBinaryNodeType.UnicodeChars32Text:
                            case XmlBinaryNodeType.UnicodeChars32TextWithEndElement:
                                unicodeTextLenSize = 4;
                                break;
                        }
                        int unicodeTextLength = ReadInt(unicodeTextLenSize);
                        if ((offset + unicodeTextLength) > xmlDoc.Length)
                        {
                            ThrowOffsetException("Error reading UnicodeCharsXText");
                        }
                        byte[] temp = new byte[unicodeTextLength];
                        Buffer.BlockCopy(xmlDoc, offset, temp, 0, unicodeTextLength);
                        newNode.Text = unicodeEncoding.GetString(temp, 0, temp.Length);
                        offset += unicodeTextLength;
                    }
                    break;
                case XmlBinaryNodeType.Bytes8Text:
                case XmlBinaryNodeType.Bytes8TextWithEndElement:
                case XmlBinaryNodeType.Bytes16Text:
                case XmlBinaryNodeType.Bytes16TextWithEndElement:
                case XmlBinaryNodeType.Bytes32Text:
                case XmlBinaryNodeType.Bytes32TextWithEndElement:
                    {
                        int byteArrayLenSize = 0;
                        switch (nodeType)
                        {
                            case XmlBinaryNodeType.Bytes8Text:
                            case XmlBinaryNodeType.Bytes8TextWithEndElement:
                                byteArrayLenSize = 1;
                                break;
                            case XmlBinaryNodeType.Bytes16Text:
                            case XmlBinaryNodeType.Bytes16TextWithEndElement:
                                byteArrayLenSize = 2;
                                break;
                            case XmlBinaryNodeType.Bytes32Text:
                            case XmlBinaryNodeType.Bytes32TextWithEndElement:
                                byteArrayLenSize = 4;
                                break;
                        }
                        int byteArrayLength = ReadInt(byteArrayLenSize);
                        if ((offset + byteArrayLength) > xmlDoc.Length)
                        {
                            ThrowOffsetException("Error reading BytesXText");
                        }
                        newNode.Bytes = new byte[byteArrayLength];
                        Buffer.BlockCopy(xmlDoc, offset, newNode.Bytes, 0, byteArrayLength);
                        offset += byteArrayLength;
                    }
                    break;
                case XmlBinaryNodeType.Int8Text:
                case XmlBinaryNodeType.Int8TextWithEndElement:
                    newNode.IntValue = ReadInt(1);
                    break;
                case XmlBinaryNodeType.Int16Text:
                case XmlBinaryNodeType.Int16TextWithEndElement:
                    newNode.IntValue = ReadInt(2);
                    break;
                case XmlBinaryNodeType.Int32Text:
                case XmlBinaryNodeType.Int32TextWithEndElement:
                    newNode.IntValue = ReadInt(4);
                    break;
                case XmlBinaryNodeType.Int64Text:
                case XmlBinaryNodeType.Int64TextWithEndElement:
                case XmlBinaryNodeType.UInt64Text:
                case XmlBinaryNodeType.UInt64TextWithEndElement:
                    newNode.LongValue = ReadLong();
                    break;
                case XmlBinaryNodeType.FloatText:
                case XmlBinaryNodeType.FloatTextWithEndElement:
                    newNode.FloatValue = ReadFloat();
                    break;
                case XmlBinaryNodeType.DoubleText:
                case XmlBinaryNodeType.DoubleTextWithEndElement:
                    newNode.DoubleValue = ReadDouble();
                    break;
                case XmlBinaryNodeType.DecimalText:
                case XmlBinaryNodeType.DecimalTextWithEndElement:
                    newNode.DecimalValue = ReadDecimal();
                    break;
                case XmlBinaryNodeType.DateTimeText:
                case XmlBinaryNodeType.DateTimeTextWithEndElement:
                    newNode.DateTimeValue = ReadDateTime();
                    break;
                case XmlBinaryNodeType.TimeSpanText:
                case XmlBinaryNodeType.TimeSpanTextWithEndElement:
                    newNode.TimeSpanValue = TimeSpan.FromTicks(ReadLong());
                    break;
                case XmlBinaryNodeType.EmptyText:
                case XmlBinaryNodeType.EmptyTextWithEndElement:
                    newNode.Text = "";
                    break;
                case XmlBinaryNodeType.ZeroText:
                case XmlBinaryNodeType.ZeroTextWithEndElement:
                    newNode.Text = "0";
                    break;
                case XmlBinaryNodeType.OneText:
                case XmlBinaryNodeType.OneTextWithEndElement:
                    newNode.Text = "1";
                    break;
                case XmlBinaryNodeType.TrueText:
                case XmlBinaryNodeType.TrueTextWithEndElement:
                    newNode.Text = "true";
                    break;
                case XmlBinaryNodeType.FalseText:
                case XmlBinaryNodeType.FalseTextWithEndElement:
                    newNode.Text = "false";
                    break;
                case XmlBinaryNodeType.DictionaryText:
                case XmlBinaryNodeType.DictionaryTextWithEndElement:
                    newNode.DictionaryText = ReadDictionaryString();
                    break;
                case XmlBinaryNodeType.GuidText:
                case XmlBinaryNodeType.GuidTextWithEndElement:
                    newNode.GuidValue = ReadGuid();
                    break;
                case XmlBinaryNodeType.UniqueIdText:
                case XmlBinaryNodeType.UniqueIdTextWithEndElement:
                    {
                        byte[] uniqueIdBytes = new byte[16];
                        if ((offset + uniqueIdBytes.Length) > xmlDoc.Length)
                        {
                            ThrowOffsetException("Error reading UniqueId");
                        }
                        Buffer.BlockCopy(xmlDoc, offset, uniqueIdBytes, 0, uniqueIdBytes.Length);
                        newNode.UniqueIdValue = new UniqueId(uniqueIdBytes);
                        offset += uniqueIdBytes.Length;
                    }
                    break;
                case XmlBinaryNodeType.StartListTextWithEndElement:
                    throw new ArgumentException("StartListTextWithEndElement is an invalid node!");
                case XmlBinaryNodeType.StartListText:
                    {
                        depth++;
                        ParseList(newNode);
                        depth--;
                    }
                    break;
                default:
                    throw new ArgumentException(String.Format("Error, the test is not ready to handle the type {0:X2} ({1}) yet", nodeType, XmlBinaryNodeType.GetNodeName(nodeType)));
            }
        }

        private void ParseList(XmlBinaryNode listNode)
        {
            if (offset >= xmlDoc.Length) return;
            while (xmlDoc[offset] != XmlBinaryNodeType.EndListText && xmlDoc[offset] != XmlBinaryNodeType.EndListTextWithEndElement)
            {
                ParseContentNode(listNode);
                if (offset >= xmlDoc.Length) return;
            }
            listNode.AddChild(new XmlBinaryNode(xmlDoc[offset], depth));
            offset++; // EndListText
        }

        private void ParseArray(XmlBinaryNode parent)
        {
            Parse(ref parent, false, true);
            if ((offset + 1) >= xmlDoc.Length) ThrowOffsetException("Inside array");
            byte arrayNodeType = xmlDoc[offset];
            offset++;
            int elementCount = ReadMB32();
            parent.ArrayElements = new BinaryArrayElements(arrayNodeType, elementCount);
            for (int i = 0; i < elementCount; i++)
            {
                switch (arrayNodeType)
                {
                    case XmlBinaryNodeType.BoolTextWithEndElement:
                        if (i == 0)
                        {
                            parent.ArrayElements.BoolArray = new bool[elementCount];
                        }
                        if (offset >= xmlDoc.Length) ThrowOffsetException("Error in BoolArray");
                        parent.ArrayElements.BoolArray[i] = (xmlDoc[offset] != 0x00);
                        offset++;
                        break;
                    case XmlBinaryNodeType.DateTimeTextWithEndElement:
                        if (i == 0)
                        {
                            parent.ArrayElements.DateTimeArray = new DateTime[elementCount];
                        }
                        parent.ArrayElements.DateTimeArray[i] = ReadDateTime();
                        break;
                    case XmlBinaryNodeType.DecimalTextWithEndElement:
                        if (i == 0)
                        {
                            parent.ArrayElements.DecimalArray = new decimal[elementCount];
                        }
                        parent.ArrayElements.DecimalArray[i] = ReadDecimal();
                        break;
                    case XmlBinaryNodeType.DoubleTextWithEndElement:
                        if (i == 0)
                        {
                            parent.ArrayElements.DoubleArray = new double[elementCount];
                        }
                        parent.ArrayElements.DoubleArray[i] = ReadDouble();
                        break;
                    case XmlBinaryNodeType.GuidTextWithEndElement:
                        if (i == 0)
                        {
                            parent.ArrayElements.GuidArray = new Guid[elementCount];
                        }
                        parent.ArrayElements.GuidArray[i] = ReadGuid();
                        break;
                    case XmlBinaryNodeType.Int16TextWithEndElement:
                        if (i == 0)
                        {
                            parent.ArrayElements.ShortArray = new short[elementCount];
                        }
                        parent.ArrayElements.ShortArray[i] = (short)ReadInt(2);
                        break;
                    case XmlBinaryNodeType.Int32TextWithEndElement:
                        if (i == 0)
                        {
                            parent.ArrayElements.IntArray = new int[elementCount];
                        }
                        parent.ArrayElements.IntArray[i] = ReadInt(4);
                        break;
                    case XmlBinaryNodeType.Int64TextWithEndElement:
                        if (i == 0)
                        {
                            parent.ArrayElements.LongArray = new long[elementCount];
                        }
                        parent.ArrayElements.LongArray[i] = ReadLong();
                        break;
                    case XmlBinaryNodeType.FloatTextWithEndElement:
                        if (i == 0)
                        {
                            parent.ArrayElements.FloatArray = new float[elementCount];
                        }
                        parent.ArrayElements.FloatArray[i] = ReadFloat();
                        break;
                    case XmlBinaryNodeType.TimeSpanTextWithEndElement:
                        if (i == 0)
                        {
                            parent.ArrayElements.TimeSpanArray = new TimeSpan[elementCount];
                        }
                        parent.ArrayElements.TimeSpanArray[i] = TimeSpan.FromTicks(ReadLong());
                        break;
                    default:
                        throw new ArgumentException(String.Format("Invalid array node type: {0:X2} - {1}",
                            arrayNodeType, XmlBinaryNodeType.GetNodeName(arrayNodeType)));
                }
            }
            if (XmlBinaryNodeType.IsTextNodeWithEndElement(arrayNodeType))
            {
                depth--;
            }
        }

        private void Parse(ref XmlBinaryNode parent)
        {
            Parse(ref parent, false, false);
        }

        private void Parse(ref XmlBinaryNode parent, bool insideAttribute)
        {
            Parse(ref parent, insideAttribute, false);
        }

        private void Parse(ref XmlBinaryNode parent, bool insideAttribute, bool insideArray)
        {
            if (offset >= xmlDoc.Length) return;
            bool first = true;
            while (offset < xmlDoc.Length)
            {
                if (!first && insideAttribute)
                {
                    return;
                }
                first = false;
                byte nodeType = xmlDoc[offset];
                if (XmlBinaryNodeType.IsTextNode(nodeType) || XmlBinaryNodeType.IsTextNodeWithEndElement(nodeType))
                {
                    ParseContentNode(parent);
                    if (XmlBinaryNodeType.IsTextNodeWithEndElement(nodeType))
                    {
                        depth--;
                        return;
                    }
                }
                else
                {
                    offset++;
                    XmlBinaryNode newNode = new XmlBinaryNode(nodeType, depth);
                    if (parent == null)
                    {
                        parent = newNode;
                    }
                    else
                    {
                        parent.AddChild(newNode);
                    }
                    switch (nodeType)
                    {
                        case XmlBinaryNodeType.EndElement:
                            depth--;
                            return;
                        case XmlBinaryNodeType.Comment:
                            newNode.Text = ReadString(0);
                            break;
                        case XmlBinaryNodeType.ShortElement:
                        case XmlBinaryNodeType.ShortDictionaryElement:
                        case XmlBinaryNodeType.Element:
                        case XmlBinaryNodeType.DictionaryElement:
                            {
                                bool isDictionaryElement = XmlBinaryNodeType.IsDictionaryElementNode(nodeType);
                                bool isShortElement = XmlBinaryNodeType.GetNodeName(nodeType).StartsWith("Short");
                                if (!isShortElement)
                                {
                                    newNode.Prefix = ReadString(0);
                                }
                                if (isDictionaryElement)
                                {
                                    newNode.DictionaryLocalName = ReadDictionaryString();
                                }
                                else
                                {
                                    newNode.LocalName = ReadString(0);
                                }
                                int oldDepth = depth;
                                depth++;
                                Parse(ref newNode);
                                if (depth != oldDepth) ThrowIncorrectDepthException("After " + XmlBinaryNodeType.GetNodeName(nodeType), oldDepth);
                                if (insideArray)
                                {
                                    return;
                                }
                            }
                            break;
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
                            {
                                char prefixLetter;
                                bool isDictionaryElement = XmlBinaryNodeType.IsDictionaryElementNode(nodeType);
                                prefixLetter = (char)((int)'a' + (int)nodeType - (int)(isDictionaryElement ? XmlBinaryNodeType.PrefixDictionaryElementA : XmlBinaryNodeType.PrefixElementA));
                                newNode.Prefix = new string(prefixLetter, 1);
                                if (isDictionaryElement)
                                {
                                    newNode.DictionaryLocalName = ReadDictionaryString();
                                }
                                else
                                {
                                    newNode.LocalName = ReadString(0);
                                }
                                int oldDepth = depth;
                                depth++;
                                Parse(ref newNode);
                                if (depth != oldDepth) ThrowIncorrectDepthException("After " + XmlBinaryNodeType.GetNodeName(nodeType), oldDepth);
                                if (insideArray)
                                {
                                    return;
                                }
                            }
                            break;
                        case XmlBinaryNodeType.ShortAttribute:
                        case XmlBinaryNodeType.ShortDictionaryAttribute:
                        case XmlBinaryNodeType.Attribute:
                        case XmlBinaryNodeType.DictionaryAttribute:
                            {
                                bool isDictionaryAttribute = XmlBinaryNodeType.IsDictionaryAttributeNode(nodeType);
                                bool isShortAttribute = XmlBinaryNodeType.GetNodeName(nodeType).StartsWith("Short");
                                if (!isShortAttribute)
                                {
                                    newNode.Prefix = ReadString(0);
                                }
                                if (isDictionaryAttribute)
                                {
                                    newNode.DictionaryLocalName = ReadDictionaryString();
                                }
                                else
                                {
                                    newNode.LocalName = ReadString(0);
                                }
                                int oldDepth = depth;
                                depth++;
                                Parse(ref newNode, true);
                                if (depth != (oldDepth + 1)) ThrowIncorrectDepthException("After " + XmlBinaryNodeType.GetNodeName(nodeType), oldDepth);
                                depth--;
                                if (newNode.Children.Count != 1)
                                {
                                    throw new ArgumentException("Error, attribute nodes must have exactly one child, at offset " + offset);
                                }
                            }
                            break;
                        case XmlBinaryNodeType.ShortXmlnsAttribute:
                        case XmlBinaryNodeType.ShortDictionaryXmlnsAttribute:
                        case XmlBinaryNodeType.XmlnsAttribute:
                        case XmlBinaryNodeType.DictionaryXmlnsAttribute:
                            {
                                bool isDictionaryAttribute = XmlBinaryNodeType.IsDictionaryAttributeNode(nodeType);
                                bool isShortAttribute = XmlBinaryNodeType.GetNodeName(nodeType).StartsWith("Short");
                                if (!isShortAttribute)
                                {
                                    newNode.Prefix = ReadString(0);
                                }
                                if (isDictionaryAttribute)
                                {
                                    newNode.DictionaryNamespaceURI = ReadDictionaryString();
                                }
                                else
                                {
                                    newNode.NamespaceURI = ReadString(0);
                                }
                            }
                            break;
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
                            {
                                char prefixLetter;
                                bool isDictionaryAttribute = XmlBinaryNodeType.IsDictionaryAttributeNode(nodeType);
                                prefixLetter = (char)((int)'a' + (int)nodeType - (int)(isDictionaryAttribute ? XmlBinaryNodeType.PrefixDictionaryAttributeA : XmlBinaryNodeType.PrefixAttributeA));
                                newNode.Prefix = new string(prefixLetter, 1);
                                if (isDictionaryAttribute)
                                {
                                    newNode.DictionaryLocalName = ReadDictionaryString();
                                }
                                else
                                {
                                    newNode.LocalName = ReadString(0);
                                }
                                int oldDepth = depth;
                                depth++;
                                Parse(ref newNode, true);
                                if (depth != (oldDepth + 1)) ThrowIncorrectDepthException("After " + XmlBinaryNodeType.GetNodeName(nodeType), oldDepth);
                                depth--;
                                if (newNode.Children.Count != 1)
                                {
                                    throw new ArgumentException("Error, attribute nodes must have exactly one child, at offset " + offset);
                                }
                            }
                            break;
                        case XmlBinaryNodeType.Array:
                            {
                                int oldDepth = depth;
                                depth++;
                                ParseArray(newNode);
                                if (depth != oldDepth) ThrowIncorrectDepthException("After " + XmlBinaryNodeType.GetNodeName(nodeType), oldDepth);
                            }
                            break;
                        default:
                            throw new ArgumentException(String.Format("Error, the test is not ready to handle the type {0:X2} ({1}) yet", nodeType, XmlBinaryNodeType.GetNodeName(nodeType)));
                    }
                }
            }
        }
    }
}
