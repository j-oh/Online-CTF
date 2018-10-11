using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NetGameShared;

namespace NetGameLauncher
{
    public class FileStateInfo
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public byte[] MD5Hash { get; set; }
        public DateTime LastWrite { get; set; }

        public FileStateInfo() { }

        public FileStateInfo(string fileName)
        {
            FileName = fileName;
            string fileLocation = FormLauncher.RootDirectory.CombinePath(FileName);
            if (File.Exists(fileLocation))
            {
                FileInfo fileInfo = new FileInfo(fileLocation);
                FileSize = fileInfo.Length;
                using (FileStream fileStream = fileInfo.OpenRead())
                {
                    MD5Hash = new MD5CryptoServiceProvider().ComputeHash(fileStream);
                    fileStream.Close();
                }
                LastWrite = fileInfo.LastWriteTimeUtc;
            }
        }

        public bool CheckLocal()
        {
            string fileLocation = FormLauncher.RootDirectory.CombinePath(FileName);
            if (!File.Exists(fileLocation))
                return false;
            FileInfo fileInfo = new FileInfo(fileLocation);
            if (FileSize != fileInfo.Length)
                return false;
            using (FileStream fileStream = fileInfo.OpenRead())
            {
                byte[] hash = new MD5CryptoServiceProvider().ComputeHash((Stream)fileStream);
                fileStream.Close();
                return ((IEnumerable<byte>)MD5Hash).SequenceEqual<byte>((IEnumerable<byte>)hash);
            }
        }
    }
}
