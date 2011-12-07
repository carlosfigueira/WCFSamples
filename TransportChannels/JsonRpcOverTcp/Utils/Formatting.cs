using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonRpcOverTcp.Utils
{
    public static class Formatting
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
    }
}
