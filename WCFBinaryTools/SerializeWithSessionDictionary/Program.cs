using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace SerializeWithSessionDictionary
{
    class Program
    {
        static void Main(string[] args)
        {
            List<Character> instance = new List<Character>
            {
                new Character { Name = "Shaggy", Age = 19, DateOfBirth = new DateTime(1991, 9, 13), Type = "Human" },
                new Character { Name = "Scooby", Age = 10, DateOfBirth = new DateTime(2000, 5, 20), Type = "Dog" },
                new Character { Name = "Scrappy", Age = 3, DateOfBirth = new DateTime(2008, 1, 2), Type = "Dog" },
                new Character { Name = "Fred", Age = 22, DateOfBirth = new DateTime(1988, 5, 29), Type = "Human" },
                new Character { Name = "Velma", Age = 24, DateOfBirth = new DateTime(1986, 5, 30), Type = "Human" },
                new Character { Name = "Daphne", Age = 20, DateOfBirth = new DateTime(1990, 5, 1), Type = "Human" },
            };

            Func<Stream, XmlBinaryWriterSession, XmlDictionaryWriter> createTextXmlWriter =
                (s, xbws) => XmlDictionaryWriter.CreateTextWriter(s);
            Func<Stream, XmlBinaryWriterSession, XmlDictionaryWriter> createNoDictBinaryWriter =
                (s, xbws) => XmlDictionaryWriter.CreateBinaryWriter(s);
            Func<Stream, XmlBinaryWriterSession, XmlDictionaryWriter> createDictBinaryWriter =
                (s, xbws) => XmlDictionaryWriter.CreateBinaryWriter(s, null, xbws);

            Func<Stream, XmlBinaryReaderSession, XmlDictionaryReader> createTextXmlReader =
                (s, xbrs) => XmlDictionaryReader.CreateTextReader(s, XmlDictionaryReaderQuotas.Max);
            Func<Stream, XmlBinaryReaderSession, XmlDictionaryReader> createNoDictBinaryReader =
                (s, xbrs) => XmlDictionaryReader.CreateBinaryReader(s, XmlDictionaryReaderQuotas.Max);
            Func<Stream, XmlBinaryReaderSession, XmlDictionaryReader> createDictBinaryReader =
                (s, xbrs) => XmlDictionaryReader.CreateBinaryReader(s, null, XmlDictionaryReaderQuotas.Max, xbrs);

            List<
                Tuple<
                    string,
                    Func<Stream, XmlBinaryWriterSession, XmlDictionaryWriter>,
                    Func<Stream, XmlBinaryReaderSession, XmlDictionaryReader>>> tests;
            tests = new List<Tuple<string, Func<Stream, XmlBinaryWriterSession, XmlDictionaryWriter>, Func<Stream, XmlBinaryReaderSession, XmlDictionaryReader>>>();
            tests.Add(
                new Tuple<string, Func<Stream, XmlBinaryWriterSession, XmlDictionaryWriter>, Func<Stream, XmlBinaryReaderSession, XmlDictionaryReader>>(
                    "Text/XML",
                    createTextXmlWriter,
                    createTextXmlReader));
            tests.Add(
                new Tuple<string, Func<Stream, XmlBinaryWriterSession, XmlDictionaryWriter>, Func<Stream, XmlBinaryReaderSession, XmlDictionaryReader>>(
                    "Binary/no dict",
                    createNoDictBinaryWriter,
                    createNoDictBinaryReader));
            tests.Add(
                new Tuple<string, Func<Stream, XmlBinaryWriterSession, XmlDictionaryWriter>, Func<Stream, XmlBinaryReaderSession, XmlDictionaryReader>>(
                    "Binary/with dict",
                    createDictBinaryWriter,
                    createDictBinaryReader));

            foreach (var test in tests)
            {
                Console.WriteLine(test.Item1);
                bool needsDict = test.Item2 == createDictBinaryWriter;
                MemoryStream ms = new MemoryStream();
                MyWriterSession writerSession = null;
                if (needsDict)
                {
                    writerSession = new MyWriterSession();
                }

                XmlDictionaryWriter xdw = test.Item2(ms, writerSession);
                Serialize(xdw, instance);
                if (needsDict)
                {
                    MemoryStream newMs = new MemoryStream();
                    writerSession.SaveDictionaryStrings(newMs);
                    ms.Position = 0;
                    ms.CopyTo(newMs);
                    ms = newMs;
                }

                Console.WriteLine("  Serialized, size = {0}", ms.Position);
                File.WriteAllBytes(@"c:\temp\serialized.bin", ms.ToArray());
                ms.Position = 0;
                XmlBinaryReaderSession readerSession = null;
                if (needsDict)
                {
                    readerSession = MyReaderSession.CreateReaderSession(ms);
                }

                XmlDictionaryReader xdr = test.Item3(ms, readerSession);
                List<Character> deserialized = Deserialize<List<Character>>(xdr);
                Console.WriteLine("  Deserialized: {0}", deserialized);
            }
        }

        static void Test()
        {
            MemoryStream serializedStream = new MemoryStream();
            XmlBinaryReaderSession readerSession = MyReaderSession.CreateReaderSession(serializedStream);
            XmlDictionaryReader xdr = XmlDictionaryReader.CreateBinaryReader(serializedStream, null, XmlDictionaryReaderQuotas.Max, readerSession);
            DataContractSerializer dcs = new DataContractSerializer(typeof(List<Character>));
            List<Character> result = (List<Character>)dcs.ReadObject(xdr);
        }
        static void Serialize(XmlDictionaryWriter xdw, object instance)
        {
            DataContractSerializer dcs = new DataContractSerializer(instance.GetType());
            dcs.WriteObject(xdw, instance);
            xdw.Flush();
        }

        static T Deserialize<T>(XmlDictionaryReader xdr)
        {
            DataContractSerializer dcs = new DataContractSerializer(typeof(T));
            return (T)dcs.ReadObject(xdr);
        }
    }

    [DataContract(Name = "Character", Namespace = "http://my.namespace.com/cartoonCharacters")]
    public class Character
    {
        [DataMember]
        public string Name;
        [DataMember]
        public int Age;
        [DataMember]
        public DateTime DateOfBirth;
        [DataMember]
        public string Type;
    }

    class MyWriterSession : XmlBinaryWriterSession
    {
        List<string> dictionaryStrings = new List<string>();
        public override bool TryAdd(XmlDictionaryString value, out int key)
        {
            bool result = base.TryAdd(value, out key);
            if (result)
            {
                this.dictionaryStrings.Add(value.Value);
            }

            return result;
        }

        public void SaveDictionaryStrings(Stream stream)
        {
            MemoryStream ms = new MemoryStream();
            foreach (string dicString in this.dictionaryStrings)
            {
                byte[] stringBytes = Encoding.UTF8.GetBytes(dicString);
                this.WriteMB31(ms, stringBytes.Length);
                ms.Write(stringBytes, 0, stringBytes.Length);
            }

            this.WriteMB31(stream, (int)ms.Position);
            ms.Position = 0;
            ms.CopyTo(stream);
        }

        private void WriteMB31(Stream stream, int value)
        {
            byte b = (byte)(value & 0x7F);
            while (value > 0x7F)
            {
                stream.WriteByte((byte)(b | 0x80));
                value = value >> 7;
                b = (byte)(value & 0x7F);
            }

            stream.WriteByte((byte)(b & 0x7F));
        }
    }

    class MyReaderSession
    {
        public static XmlBinaryReaderSession CreateReaderSession(Stream stream)
        {
            XmlBinaryReaderSession result = new XmlBinaryReaderSession();
            int nextId = 0;
            int bytesRead;
            int sessionSize = ReadMB31(stream, out bytesRead);
            while (sessionSize > 0)
            {
                int stringSize = ReadMB31(stream, out bytesRead);
                sessionSize -= bytesRead + stringSize;
                byte[] stringBytes = new byte[stringSize];
                stream.Read(stringBytes, 0, stringBytes.Length);
                string dicString = Encoding.UTF8.GetString(stringBytes);
                result.Add(nextId++, dicString);
            }

            return result;
        }

        private static int ReadMB31(Stream stream, out int bytesRead)
        {
            bytesRead = 0;
            int result = 0;
            int shift = 0;
            int b = stream.ReadByte();
            bytesRead++;
            do
            {
                result = result | (b << shift);
                shift += 7;
                if (b >= 0x80)
                {
                    b = stream.ReadByte();
                    bytesRead++;
                }
            } while (b >= 0x80);

            return result;
        }
    }
}
