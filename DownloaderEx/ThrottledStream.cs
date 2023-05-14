using System;
using System.IO;
using System.Threading;

namespace DownloaderEx
{
    public class ThrottledStream : Stream
    {

        public const long Infinite = 0;

        private Stream _baseStream;

        private long _byteCount;

        private long _start;

        private long _maximumBytesPerSecond;

        protected long CurrentMilliseconds{ get{return Environment.TickCount;}}

        public long MaximumBytesPerSecond
        { 
            get { return _maximumBytesPerSecond;}
            set
            {
                if (MaximumBytesPerSecond != value)
                {
                    _maximumBytesPerSecond = value;
                    Reset();
                }
            }
        }

        public override bool CanRead { get{return _baseStream.CanRead;}}

        public override bool CanSeek { get{return _baseStream.CanSeek;}}

        public override bool CanWrite { get{return _baseStream.CanWrite;}}

        public override long Length{ get {return _baseStream.Length;}}

        public override long Position { get { return _baseStream.Position;} set{ _baseStream.Position = value;}}

        public ThrottledStream(Stream baseStream)
            : this(baseStream, ThrottledStream.Infinite){}

        public ThrottledStream(Stream baseStream, long maximumBytesPerSecond)
        {
            if (baseStream == null)
            {
                throw new ArgumentNullException("baseStream");
            }

            if (maximumBytesPerSecond < 0)
            {
                throw new ArgumentOutOfRangeException("maximumBytesPerSecond",maximumBytesPerSecond, "Кол-во байтов в секунду не может быть отрицательным");
            }

            _baseStream = baseStream;
            _maximumBytesPerSecond = maximumBytesPerSecond;
            _start = CurrentMilliseconds;
            _byteCount = 0;
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Throttle(count);

            return _baseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Throttle(count);

            _baseStream.Write(buffer, offset, count);
        }


        public override string ToString()
        {
            return _baseStream.ToString();
        }
  
        protected void Throttle(int bufferSizeInBytes)
        {
            
            if (_maximumBytesPerSecond <= 0 || bufferSizeInBytes <= 0)
            {
                return;
            }

            _byteCount += bufferSizeInBytes;
            long elapsedMilliseconds = CurrentMilliseconds - _start;

            if (elapsedMilliseconds > 0)
            {
                long bps = _byteCount * 1000L / elapsedMilliseconds;

                if (bps > _maximumBytesPerSecond)
                {
                    long wakeElapsed = _byteCount * 1000L / _maximumBytesPerSecond;
                    int toSleep = (int)(wakeElapsed - elapsedMilliseconds);

                    if (toSleep > 1)
                    {
                        try
                        {
                            Thread.Sleep(toSleep);
                        }
                        catch (ThreadAbortException)
                        {

                        }
                        Reset();
                    }
                }
            }
        }
        protected void Reset()
        {
            long difference = CurrentMilliseconds - _start;

            if (difference > 1000)
            {
                _byteCount = 0;
                _start = CurrentMilliseconds;
            }
        }
    }
}
