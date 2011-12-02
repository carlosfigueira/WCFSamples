using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleSocketService
{
    public static class Util
    {
        public static int BytesToSize(byte[] bytes, int offset)
        {
            return bytes[offset] << 24 |
                bytes[offset + 1] << 16 |
                bytes[offset + 2] << 8 |
                bytes[offset + 3];
        }

        public static void SizeToBytes(int size, byte[] bytes, int offset)
        {
            bytes[offset + 0] = (byte)(size >> 24);
            bytes[offset + 1] = (byte)(size >> 16);
            bytes[offset + 2] = (byte)(size >> 8);
            bytes[offset + 3] = (byte)(size);
        }

        public static void PrintBytes(byte[] bytes)
        {
            PrintBytes(bytes, bytes.Length);
        }

        public static void PrintBytes(byte[] bytes, int count)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if (i > 0 && ((i % 16) == 0))
                {
                    Console.WriteLine("   {0}", sb.ToString());
                    sb.Length = 0;
                }
                else if (i > 0 && ((i % 8) == 0))
                {
                    Console.Write(" ");
                    sb.Append(' ');
                }

                Console.Write(" {0:X2}", (int)bytes[i]);
                if (' ' <= bytes[i] && bytes[i] <= '~')
                {
                    sb.Append((char)bytes[i]);
                }
                else
                {
                    sb.Append('.');
                }
            }

            if ((count % 16) > 0)
            {
                int spacesToPrint = 3 * (16 - (count % 16));
                if ((count % 16) <= 8)
                {
                    spacesToPrint++;
                }

                Console.Write(new string(' ', spacesToPrint));
            }

            Console.WriteLine("   {0}", sb.ToString());
        }
    }
}
