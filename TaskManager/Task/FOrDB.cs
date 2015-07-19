using System;
using System.Collections.Generic;
using System.IO;

namespace TaskManager.Task.FOrDB
{
    class FOrDB
    {
        internal static string startDir = @"C:\FOrDB\Database\";

        public string CompleteDir { get { return startDir + DataBaseName; } }

        public string DataBaseName { get; set; }

        public Dictionary<string, Table> Tables { get; set; } = new Dictionary<string, Table>();

        public FOrDB(string dataBaseName)
        {
            DataBaseName = dataBaseName.TrimStart('\\');
            CreateDB();
            Ctor();
        }

        private void Ctor()
        {
            var tables = Directory.GetDirectories(CompleteDir);

            Tables.Clear();

            foreach (var item in tables)
            {
                var table = item.Substring(item.LastIndexOf('\\')).TrimStart('\\');

                Tables.Add(table, new Table(item));
            }
        }
        public void CreateTable(string tableName)
        {
            CheckDir(CompleteDir + "\\" + tableName.TrimStart('\\'));
            Ctor();
        }
        private void CreateDB()
        {
            CheckDir(CompleteDir);
        }
        internal void CheckDir(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public class Table
        {
            public string TablePath { get; set; }
            public Table(string tablePath)
            {
                TablePath = tablePath;
            }
            public Dictionary<string, object> Load()
            {
                var list = Directory.GetFiles(TablePath, "*.jsonf");
                var convertedList = new Dictionary<string, object>();

                foreach (var item in list)
                {
                    var fileId = item.Substring(item.LastIndexOf('\\')).TrimStart('\\').TrimEnd((".jsonf").ToCharArray());
                    var content = File.ReadAllText(item);
                    convertedList.Add(fileId, content);
                }
                return convertedList;
            }
            public Dictionary<string,T> Load<T>()
            {
                var list = Directory.GetFiles(TablePath, "*.jsonf");
                var convertedList = new Dictionary<string,T>();

                foreach (var item in list)
                {
                    var fileId = item.Substring(item.LastIndexOf('\\')).TrimStart('\\').TrimEnd((".jsonf").ToCharArray());
                    var content = File.ReadAllText(item);
                    convertedList.Add(fileId,Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content));
                }

                return convertedList;
            }
            public void Insert(object value)
            {
                var fileName = TablePath + @"\" + (GetHashCode() + DateTime.Now.Ticks) + ".jsonf";
                File.Create(fileName).Close();
                File.WriteAllText(fileName, Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented));
            }
            public void Insert(string id, object value)
            {
                var filePath = TablePath + @"\" + id + ".jsonf";
                if (File.Exists(filePath)) File.Delete(filePath);
                File.Create(filePath).Close();
                File.WriteAllText(filePath, Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented));
            }
            public void Update<T>(KeyValuePair<string, T> old, object value)
            {
                Update(old.Key, value);
            }
            public void Update(string id, object value)
            {
                var filePath = TablePath + @"\" + id + ".jsonf";
                File.Delete(filePath);
                File.Create(filePath).Close();
                File.WriteAllText(filePath, Newtonsoft.Json.JsonConvert.SerializeObject(value));
            }
            public void Delete<T>(KeyValuePair<string, T> value)
            {
                File.Delete(TablePath + @"\" + value.Key + ".jsonf");
            }
            public void Delete(string fileId)
            {
                File.Delete(TablePath + @"\" + fileId + ".jsonf");
            }
            public void Clear()
            {
                foreach (var item in Load())
                {
                    Delete(item.Key);
                }
            }
        }
    }
}

