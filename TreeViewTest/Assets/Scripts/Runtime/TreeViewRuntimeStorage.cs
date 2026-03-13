using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Runtime
{
    [Serializable]
    public sealed class TreeViewRuntimeData
    {
        public List<DataItem> items = new List<DataItem>();
    }

    public static class TreeViewRuntimeStorage
    {
        private const string RuntimeDataFolder = "RuntimeData";

        public static bool TryLoad(string fileName, out TreeViewRuntimeData data, out string error)
        {
            data = null;
            error = null;

            var path = GetPath(fileName);
            if (!File.Exists(path))
            {
                error = "No persisted file exists yet.";
                return false;
            }

            try
            {
                var raw = File.ReadAllText(path, Encoding.UTF8);
                data = JsonUtility.FromJson<TreeViewRuntimeData>(raw);
                return data != null;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TrySave(string fileName, TreeViewRuntimeData data, out string error)
        {
            error = null;

            try
            {
                var path = GetPath(fileName);
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonUtility.ToJson(data ?? new TreeViewRuntimeData(), true);
                File.WriteAllText(path, json, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static string GetPath(string fileName)
        {
            var safeName = string.IsNullOrWhiteSpace(fileName) ? "treeview-runtime-state.json" : fileName;
            return Path.Combine(Application.persistentDataPath, RuntimeDataFolder, safeName);
        }
    }
}
