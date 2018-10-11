using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using NetGameShared;

namespace NetGameLauncher
{
    public class ContentFile
    {
        public string GameVersion { get; private set; }
        public string ExecutablePath { get; private set; }
        public FileStateInfo[] Files = new FileStateInfo[0];
        [NonSerialized]
        public List<FileStateInfo> FileList = new List<FileStateInfo>();

        public static ContentFile FromFile(string path)
        {
            try
            {
                ContentFile contentFile = new ContentFile();
                using (FileStream fileStream = new FileStream(path, FileMode.Open))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(FileStateInfo[]));
                    contentFile.Files = (FileStateInfo[])xmlSerializer.Deserialize(fileStream);
                    fileStream.Close();
                }
                using (FileStream fileStream = new FileStream(path.Replace(".xml", ".info.txt"), FileMode.Open))
                {
                    StreamReader streamReader = new StreamReader(fileStream);
                    while (!streamReader.EndOfStream)
                    {
                        string str = streamReader.ReadLine();
                        if (str.StartsWith("GameVersion="))
                            contentFile.GameVersion = str.Replace("GameVersion=", string.Empty);
                        if (str.StartsWith("ExecutablePath="))
                            contentFile.ExecutablePath = str.Replace("ExecutablePath=", string.Empty);
                    }
                    fileStream.Close();
                }
                return contentFile;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return null;
        }

        public void GenerateContentFiles(string sourcePath)
        {
            if (Directory.Exists(sourcePath))
            {
                if (File.Exists("content.xml"))
                    File.Delete("content.xml");
                if (File.Exists("content.info.txt"))
                    File.Delete("content.info.txt");
                if (Directory.Exists("content"))
                    Directory.Delete("content", true);
                Directory.CreateDirectory("content");

                string[] directories = Directory.GetDirectories(sourcePath, "*.*", SearchOption.AllDirectories);
                string[] files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
                string xmlPath = FormLauncher.RootDirectory.CombinePath("content.xml");
                foreach(string directory in directories)
                    Directory.CreateDirectory(directory.Replace(sourcePath, "content/"));
                foreach (string file in files)
                {
                    string filePath = "content/" + file.Replace(sourcePath, string.Empty);
                    File.Copy(file, filePath, true);
                    FileStateInfo fileInfo = new FileStateInfo(filePath);
                    FileList.Add(fileInfo);
                }
                Files = FileList.ToArray();
                using (FileStream fileStream = new FileStream(xmlPath, FileMode.OpenOrCreate))
                {
                    new XmlSerializer(typeof(FileStateInfo[])).Serialize(fileStream, Files);
                    fileStream.Close();
                }
                string[] contents = new string[2]
                {
                    String.Format("GameVersion={0}", Universal.GAME_VERSION),
                    String.Format("ExecutablePath={0}", Universal.EXECUTABLE_PATH)
                };
                File.WriteAllLines(xmlPath.Replace(".xml", ".info.txt"), contents);
            }
        }
    }
}
