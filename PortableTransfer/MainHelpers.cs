using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.ServiceModel.Channels;
using System.Security.Cryptography;

namespace PortableTransfer.Helpers {
    public class CommonHelper {
        public static Mutex WaitForMutext(string mutexName) {
            Mutex mutex = new Mutex(false, mutexName);
            mutex.WaitOne();
            return mutex;
        }
        public static Mutex WaitForMutext(string mutexName, int milisecondsTimeout) {
            Mutex mutex = new Mutex(false, mutexName);
            mutex.WaitOne(milisecondsTimeout);
            return mutex;
        }
        public static void ReleaseMutex(Mutex mutex) {
            if (mutex == null) return;
            mutex.ReleaseMutex();
        }
        public static void RunInMutex(string mutexName, ThreadStart work) {
            Mutex m = CommonHelper.WaitForMutext(mutexName);
            try {
                if(work != null) work();
            } finally {
                CommonHelper.ReleaseMutex(m);
            }
        }
        public static byte[] ReadAllBytes(string filePath) {
            using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
                byte[] result = new byte[file.Length];
                if (result.LongLength > Int32.MaxValue) throw new IOException(string.Format("File '{0}' is to large.", filePath));
                file.Read(result, 0, result.Length);
                return result;
            }
        }
    }

    public class CollectionHelper {
        public static T[] EnumerableToArray<T>(IEnumerable<T> enumerable) {
            if (enumerable == null) return new T[0];
            List<T> result = new List<T>();
            foreach (T item in enumerable) {
                result.Add(item);
            }
            return result.ToArray();
        }

        public static Dictionary<K, T> CollectionToDictionary<K, T>(ICollection<T> collection, GetKeyFromObjectHander<K, T> getKey) {
            Dictionary<K, T> dict = new Dictionary<K, T>();
            if (collection != null) {
                foreach (T item in collection) {
                    dict[getKey(item)] = item;
                }
            }
            return dict;
        }
        public static bool BytesAreEquals(byte[] a, byte[] b) {
            if (a == b) return true;
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
    public delegate K GetKeyFromObjectHander<K, T>(T obj);

    public abstract class GZIPCompressor {
        static GZIPCompressor standartInstance;
        static GZIPCompressor CreateIonic() {
            return new IonicGZIPCompressor();
        }
        static void Return(GZIPCompressor compressor) {
            lock (pool) {
                if (standartInstance == compressor)
                    return;
                pool.Push(compressor);
            }
        }
        static Stack<GZIPCompressor> pool = new Stack<GZIPCompressor>();
        static GZIPCompressor Take() {
            lock (pool) {
                if (standartInstance != null)
                    return standartInstance;
                if (pool.Count > 0)
                    return pool.Pop();
                try {
                    return CreateIonic();
                } catch {
                    standartInstance = new StandartGZIPCompressor();
                    return standartInstance;
                }
            }
        }
        public static bool Compress(byte[] data, int offset, int lenght, Stream memoryStream, byte[] tempBuffer, bool gzip) {
            GZIPCompressor compressor = Take();
            bool res = compressor.CompressBuffer(data, offset, lenght, memoryStream, tempBuffer, gzip);
            Return(compressor);
            return res;
        }
        public static void Decompress(byte[] data, int offset, int lenght, Stream decompressedStream, byte[] tempBuffer, bool gzip) {
            GZIPCompressor compressor = Take();
            compressor.DecompressBuffer(data, offset, lenght, decompressedStream, tempBuffer, gzip);
            Return(compressor);
        }
        public abstract bool CompressBuffer(byte[] data, int offset, int lenght, Stream memoryStream, byte[] tempBuffer, bool gzip);
        public abstract void DecompressBuffer(byte[] data, int offset, int lenght, Stream decompressedStream, byte[] tempBuffer, bool gzip);
    }
    public class StandartGZIPCompressor : GZIPCompressor {
        public override bool CompressBuffer(byte[] data, int offset, int lenght, Stream memoryStream, byte[] tempBuffer, bool gzip) {
            int compressLen = Math.Min(lenght, tempBuffer.Length);
            Stream gzStream = gzip ? (Stream)new GZipStream(memoryStream, CompressionMode.Compress, true) : new DeflateStream(memoryStream, CompressionMode.Compress, true);
            using (gzStream) {
                gzStream.Write(data, offset, compressLen);
                if (lenght != compressLen) {
                    if (memoryStream.Length > compressLen)
                        return false;
                    else
                        gzStream.Write(data, offset + compressLen, lenght - compressLen);
                }
            }
            return true;
        }
        public override void DecompressBuffer(byte[] data, int offset, int lenght, Stream decompressedStream, byte[] tempBuffer, bool gzip) {
            MemoryStream memoryStream = new MemoryStream(data, offset, lenght);
            Stream gzStream = gzip ? (Stream)new GZipStream(memoryStream, CompressionMode.Decompress) : new DeflateStream(memoryStream, CompressionMode.Decompress);
            using (gzStream) {
                while (true) {
                    int bytesRead = gzStream.Read(tempBuffer, 0, tempBuffer.Length);
                    if (bytesRead == 0)
                        break;
                    decompressedStream.Write(tempBuffer, 0, bytesRead);
                }
            }
        }
    }
    public class IonicGZIPCompressor : GZIPCompressor {
        Ionic.Zlib.ZlibCodec deflate;
        Ionic.Zlib.ZlibCodec inflate;
        delegate int ResetInflate();
        ResetInflate resetInflate;
        public IonicGZIPCompressor() {
            deflate = new Ionic.Zlib.ZlibCodec(Ionic.Zlib.CompressionMode.Compress);
            deflate.InitializeDeflate(Ionic.Zlib.CompressionLevel.Level3, false);
            inflate = new Ionic.Zlib.ZlibCodec(Ionic.Zlib.CompressionMode.Decompress);
            inflate.InitializeInflate(false);
            object istate = inflate.GetType().GetField("istate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(inflate);
            System.Reflection.MethodInfo reset = istate.GetType().GetMethod("Reset", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            resetInflate = (ResetInflate)Delegate.CreateDelegate(typeof(ResetInflate), istate, reset);
        }
        static byte[] gzipHeader = new byte[] { 0x1F, 0x8B, 8, 0, 0, 0, 0, 0, 0, 0 };
        public override bool CompressBuffer(byte[] data, int offset, int lenght, Stream memoryStream, byte[] tempBuffer, bool gzip) {
            if (gzip)
                memoryStream.Write(gzipHeader, 0, gzipHeader.Length);
            deflate.ResetDeflate();

            deflate.OutputBuffer = tempBuffer;

            deflate.InputBuffer = data;
            try {
                deflate.NextIn = offset;
                deflate.AvailableBytesIn = lenght;

                double size = 0;
                do {
                    deflate.NextOut = 0;
                    deflate.AvailableBytesOut = tempBuffer.Length;

                    int rc = deflate.Deflate(Ionic.Zlib.FlushType.Finish);
                    if (rc != Ionic.Zlib.ZlibConstants.Z_OK && rc != Ionic.Zlib.ZlibConstants.Z_STREAM_END)
                        throw new Exception(String.Format("deflating:  rc={0}  msg={1}", rc, deflate.Message));

                    int outsize = tempBuffer.Length - deflate.AvailableBytesOut;
                    size += outsize;
                    if (deflate.NextIn < size * 1.2)
                        return false;

                    memoryStream.Write(tempBuffer, 0, outsize);
                } while (deflate.AvailableBytesIn != 0 || deflate.AvailableBytesOut == 0);
                if (gzip) {
                    Ionic.Zlib.CRC32 crc = new Ionic.Zlib.CRC32();
                    crc.SlurpBlock(data, offset, lenght);
                    int crc32 = crc.Crc32Result;
                    tempBuffer[0] = (byte)crc32;
                    tempBuffer[1] = (byte)(crc32 >> 8);
                    tempBuffer[2] = (byte)(crc32 >> 0x10);
                    tempBuffer[3] = (byte)(crc32 >> 0x18);
                    memoryStream.Write(tempBuffer, 0, 4);
                    tempBuffer[0] = (byte)lenght;
                    tempBuffer[1] = (byte)(lenght >> 8);
                    tempBuffer[2] = (byte)(lenght >> 0x10);
                    tempBuffer[3] = (byte)(lenght >> 0x18);
                    memoryStream.Write(tempBuffer, 0, 4);
                }
            } finally {
                deflate.OutputBuffer = null;
                deflate.InputBuffer = null;
            }
            return true;
        }
        public override void DecompressBuffer(byte[] data, int offset, int lenght, Stream decompressedStream, byte[] tempBuffer, bool gzip) {
            resetInflate();

            inflate.OutputBuffer = tempBuffer;

            inflate.InputBuffer = data;
            try {
                inflate.NextIn = offset;
                inflate.AvailableBytesIn = lenght;
                if (gzip) {
                    inflate.NextIn += 10;
                    inflate.AvailableBytesIn -= 18;
                }

                do {
                    inflate.NextOut = 0;
                    inflate.AvailableBytesOut = tempBuffer.Length;

                    int rc = inflate.Inflate(Ionic.Zlib.FlushType.Finish);
                    if (rc != Ionic.Zlib.ZlibConstants.Z_OK && rc != Ionic.Zlib.ZlibConstants.Z_STREAM_END)
                        throw new Exception(String.Format("inflating:  rc={0}  msg={1}", rc, inflate.Message));

                    decompressedStream.Write(tempBuffer, 0, tempBuffer.Length - inflate.AvailableBytesOut);

                    if (rc == Ionic.Zlib.ZlibConstants.Z_STREAM_END)
                        break;
                } while (inflate.AvailableBytesIn != 0 || inflate.AvailableBytesOut == 0);
            } finally {
                inflate.OutputBuffer = null;
                inflate.InputBuffer = null;
            }
        }
    }

    public class CompressHelper {
        const int compressKeyData = 0x71F1A431;
        static BufferManager bufferManager = BufferManager.CreateBufferManager(0x80000L, 0x10000);
        public static byte[] TryToDecompressData(byte[] data) {
            if (data == null || !IsCompressed(data)) { return data; }
            return DecompressData(data);
        }
        public static byte[] TryToCompressData(byte[] data) {
            if (IsCompressed(data)) return data;
            if (data == null || data.Length < 200) { return data; }
            byte[] compressedData = CompressDataInternal(data);
            if ((compressedData == data) || (compressedData.Length == 0 && data.Length != 0) || ((double)compressedData.Length / data.Length > .9)) { return data; }
            byte[] result = new byte[compressedData.Length + 4];
            Array.Copy(BitConverter.GetBytes(compressKeyData), 0, result, 0, 4);
            Array.Copy(compressedData, 0, result, 4, compressedData.Length);
            return result;
        }

        public static byte[] DecompressData(byte[] data) {
            int maybeKey = BitConverter.ToInt32(data, 0);
            switch (maybeKey) {
                case compressKeyData:
                    return DecompressDataInternal(data, 4);
            }
            throw new InvalidOperationException("Data is not compressed.");
        }
        public static bool IsCompressed(byte[] data) {
            if (data == null) return false;
            if (data.Length < 8) return false;
            int maybeKey = BitConverter.ToInt32(data, 0);
            return maybeKey == compressKeyData;
        }

        static byte[] CompressDataInternal(byte[] data) {
            MemoryStream ms = new MemoryStream();
            using(ms) {
                byte[] buffer = bufferManager.TakeBuffer(64 * 1024);
                try {
                    if(!GZIPCompressor.Compress(data, 0, data.Length, ms, buffer, false)) {
                        return data;
                    }
                } finally {
                    bufferManager.ReturnBuffer(buffer);
                }
            }
            return ms.ToArray();
        }

        static byte[] DecompressDataInternal(byte[] data, int beginIndex) {
            MemoryStream resultStream = new MemoryStream();
            using(resultStream) {
                byte[] buffer = bufferManager.TakeBuffer(64 * 1024);
                try {
                    GZIPCompressor.Decompress(data, beginIndex, data.Length - beginIndex, resultStream, buffer, false);
                } finally {
                    bufferManager.ReturnBuffer(buffer);
                }
            }
            return resultStream.ToArray();
        }

        static MD5 md5;
        public static MD5 Md5 {
            get {
                MD5 value = md5;
                if (value == null) {
                    value = new MD5CryptoServiceProvider();
                    md5 = value;
                }
                return value;
            }
        }

    }

}
