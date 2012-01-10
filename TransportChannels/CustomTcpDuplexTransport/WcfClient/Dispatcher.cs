using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace CustomTcpDuplex.WcfClient
{
    class Dispatcher
    {
        internal byte[] DispatchOperation(byte[] buffer, int offset, int count)
        {
            MemoryStream ms = new MemoryStream(buffer, offset, count);
            XmlDocument doc = new XmlDocument();
            doc.Load(ms);
            XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("s", "http://www.w3.org/2003/05/soap-envelope");
            nsManager.AddNamespace("a", "http://www.w3.org/2005/08/addressing");
            nsManager.AddNamespace("tempuri", "http://tempuri.org/");
            string action = doc.SelectSingleNode("s:Envelope/s:Header/a:Action", nsManager).InnerText;
            string messageId = doc.SelectSingleNode("s:Envelope/s:Header/a:MessageID", nsManager).InnerText;
            string operation = action.Substring(action.LastIndexOf('/') + 1);
            XmlElement body = doc.SelectSingleNode("s:Envelope/s:Body/tempuri:" + operation, nsManager) as XmlElement;
            int x = int.Parse(body.ChildNodes[0].InnerText);
            int y = int.Parse(body.ChildNodes[1].InnerText);

            int result = operation == "Add" ? (x + y) : (x - y);

            string response = @"<s:Envelope
        xmlns:s=""http://www.w3.org/2003/05/soap-envelope""
        xmlns:a=""http://www.w3.org/2005/08/addressing"">
    <s:Header>
        <a:Action s:mustUnderstand=""1"">http://tempuri.org/ITypedTest/OPERATIONResponse</a:Action>
        <a:RelatesTo>MESSAGEID</a:RelatesTo>
    </s:Header>
    <s:Body>
        <OPERATIONResponse xmlns=""http://tempuri.org/"">
            <OPERATIONResult>RESULT</OPERATIONResult>
        </OPERATIONResponse>
    </s:Body>
</s:Envelope>";
            response = response.Replace("OPERATION", operation)
                .Replace("MESSAGEID", messageId)
                .Replace("RESULT", result.ToString());

            return new UTF8Encoding(false).GetBytes(response);
        }
    }
}
