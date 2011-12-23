using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace StreamingInsideDataContractsClient
{
    class Program
    {
        static void Main(string[] args)
        {
            SomeServiceClient c = new SomeServiceClient();

            byte[] fileContents = new byte[100];
            for (int i = 0; i < fileContents.Length; i++)
            {
                fileContents[i] = (byte)'a';
            }

            RequestClass request = new RequestClass
            {
                id = "id",
                token = "token",
                content = new Content
                {
                    name = "file",
                    extension = "ext",
                    //data = fileContents,
                    DataStream = new MemoryStream(fileContents),
                },
            };

            c.SendRequest(request);

            ResponseClass resp = c.GetResponse(234);
            Console.WriteLine(resp.content.name);
            Stream dataStream = resp.content.GetDataStream();
            string text = new StreamReader(dataStream).ReadToEnd();
            Console.WriteLine("Data stream: {0}", text);
        }
    }
}
