using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace com.b_velop.Slipways.Data.Extensions
{
    public static class CacheExtensions
    {
        public static byte[] ToByteArray<T>(this T obj) where T : class
        {
            var bf = new BinaryFormatter();
            using var ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        public static T ToObject<T>(this byte[] arrBytes) where T : class
        {
            using var memStream = new MemoryStream();
            var binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            var obj = binForm.Deserialize(memStream) as T;
            return obj;
        }
    }
}
