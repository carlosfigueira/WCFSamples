using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace ChangeXmlPrefixes
{
    public class PrefixReplacer
    {
        const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";
        Dictionary<string, string> namespaceToNewPrefixMapping = new Dictionary<string, string>();

        public void AddNamespace(string namespaceUri, string newPrefix)
        {
            this.namespaceToNewPrefixMapping.Add(namespaceUri, newPrefix);
        }

        public void ChangePrefixes(XmlDocument doc)
        {
            XmlElement element = doc.DocumentElement;
            XmlElement newElement = ChangePrefixes(doc, element);
            doc.LoadXml(newElement.OuterXml);
        }

        private XmlElement ChangePrefixes(XmlDocument doc, XmlElement element)
        {
            string newPrefix;
            if (this.namespaceToNewPrefixMapping.TryGetValue(element.NamespaceURI, out newPrefix))
            {
                XmlElement newElement = doc.CreateElement(newPrefix, element.LocalName, element.NamespaceURI);
                List<XmlNode> children = new List<XmlNode>(element.ChildNodes.Cast<XmlNode>());
                List<XmlAttribute> attributes = new List<XmlAttribute>(element.Attributes.Cast<XmlAttribute>());
                foreach (XmlNode child in children)
                {
                    newElement.AppendChild(child);
                }

                foreach (XmlAttribute attr in attributes)
                {
                    newElement.Attributes.Append(attr);
                }

                element = newElement;
            }

            List<XmlAttribute> newAttributes = new List<XmlAttribute>();
            bool modified = false;
            for (int i = 0; i < element.Attributes.Count; i++)
            {
                XmlAttribute attr = element.Attributes[i];
                if (this.namespaceToNewPrefixMapping.TryGetValue(attr.NamespaceURI, out newPrefix))
                {
                    XmlAttribute newAttr = doc.CreateAttribute(newPrefix, attr.LocalName, attr.NamespaceURI);
                    newAttr.Value = attr.Value;
                    newAttributes.Add(newAttr);
                    modified = true;
                }
                else if (attr.NamespaceURI == XmlnsNamespace && this.namespaceToNewPrefixMapping.TryGetValue(attr.Value, out newPrefix))
                {
                    XmlAttribute newAttr;
                    if (newPrefix != "")
                    {
                        newAttr = doc.CreateAttribute("xmlns", newPrefix, XmlnsNamespace);
                    }
                    else
                    {
                        newAttr = doc.CreateAttribute("xmlns");
                    }

                    newAttr.Value = attr.Value;
                    newAttributes.Add(newAttr);
                    modified = true;
                }
                else
                {
                    newAttributes.Add(attr);
                }
            }

            if (modified)
            {
                element.Attributes.RemoveAll();
                foreach (var attr in newAttributes)
                {
                    element.Attributes.Append(attr);
                }
            }

            List<KeyValuePair<XmlNode, XmlNode>> toReplace = new List<KeyValuePair<XmlNode, XmlNode>>();
            foreach (XmlNode child in element.ChildNodes)
            {
                XmlElement childElement = child as XmlElement;
                if (childElement != null)
                {
                    XmlElement newChildElement = ChangePrefixes(doc, childElement);
                    if (newChildElement != childElement)
                    {
                        toReplace.Add(new KeyValuePair<XmlNode, XmlNode>(childElement, newChildElement));
                    }
                }
            }

            if (toReplace.Count > 0)
            {
                for (int i = 0; i < toReplace.Count; i++)
                {
                    element.InsertAfter(toReplace[i].Value, toReplace[i].Key);
                    element.RemoveChild(toReplace[i].Key);
                }
            }

            return element;
        }
    }
}
