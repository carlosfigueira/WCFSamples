using System;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.IO;

namespace SLApp.Web
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class WcfDownloadService : IWcfDownloadService
    {
        public Stream Download(string fileName, long fileSize)
        {
            return new MyStream(fileSize);
        }

        // dummy stream, for testing purposes
        class MyStream : Stream
        {
            long size;
            long bytesRemaining;

            public MyStream(long size)
            {
                this.size = this.bytesRemaining = size;
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override void Flush()
            {
            }

            public override long Length
            {
                get { return this.size; }
            }

            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int toReturn = (int)Math.Min((long)count, this.bytesRemaining);
                this.bytesRemaining -= toReturn;
                return toReturn;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}
