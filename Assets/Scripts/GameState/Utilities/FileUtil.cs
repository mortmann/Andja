using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace Andja.Utility {
    public class FileUtil {
        public static string Unzip(byte[] bytes) {
            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(msi, CompressionMode.Decompress)) {
                CopyTo(gs, mso);
            }
            return System.Text.Encoding.UTF8.GetString(mso.ToArray());
        }

        public static void CopyTo(Stream src, Stream dest) {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0) {
                dest.Write(bytes, 0, cnt);
            }
        }
        public static byte[] Zip(string str) {
            var bytes = System.Text.Encoding.UTF8.GetBytes(str);
            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(mso, CompressionMode.Compress)) {
                CopyTo(msi, gs);
            }
            return mso.ToArray();
        }
    }
}
