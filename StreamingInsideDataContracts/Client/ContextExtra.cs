using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public partial class Content : IXmlSerializable
{
    private Stream stream;
    private string streamFileName;

    public Stream DataStream
    {
        set { this.stream = value; }
    }

    public Stream GetDataStream()
    {
        return File.OpenRead(this.streamFileName);
    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
        reader.ReadStartElement(); //wrapping element

        this.name = reader.ReadElementContentAsString();
        this.extension = reader.ReadElementContentAsString();

        string tempFileName = Path.GetTempFileName();
        this.streamFileName = tempFileName;
        using (FileStream fs = File.Create(tempFileName))
        {
            byte[] buffer = new byte[1000];
            int bytesRead;
            reader.ReadStartElement();
            do
            {
                bytesRead = reader.ReadContentAsBase64(buffer, 0, buffer.Length);
                fs.Write(buffer, 0, bytesRead);
            } while (bytesRead > 0);

            reader.ReadEndElement();
        }

        reader.ReadEndElement(); //wrapping element
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("name");
        writer.WriteValue(this.name);
        writer.WriteEndElement();

        writer.WriteStartElement("extension");
        writer.WriteValue(this.extension);
        writer.WriteEndElement();

        writer.WriteStartElement("data");
        XmlDictionaryWriter dictWriter = writer as XmlDictionaryWriter;
        bool isMtom = dictWriter != null && dictWriter is IXmlMtomWriterInitializer;
        if (isMtom)
        {
            dictWriter.WriteValue(new MyStreamProvider(this.stream));
        }
        else
        {
            // fall back to the original behavior
            byte[] buffer = new byte[1000];
            int bytesRead;
            do
            {
                bytesRead = this.stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    writer.WriteBase64(buffer, 0, bytesRead);
                }
            } while (bytesRead > 0);
        }
        writer.WriteEndElement();
    }

    class MyStreamProvider : IStreamProvider
    {
        Stream stream;
        public MyStreamProvider(Stream stream)
        {
            this.stream = stream;
        }

        public Stream GetStream()
        {
            return this.stream;
        }

        public void ReleaseStream(Stream stream)
        {
        }
    }
}
