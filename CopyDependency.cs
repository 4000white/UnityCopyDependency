using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using System;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public class CopyDependency : EditorWindow
    {
        private static List<string> includeList = new List<string>{ ".prefab" , ".mat", ".anim", ".controller"};
        private static List<string> ignoreList = new List<string>{".meta", ".DS_Store"};
        private const string strCache1 = "New Fold";
        private const string strCache2 = "Old Fold";
        Object oldFold;
        Object newFold;
        private static string oldPath = "";
        private static string newPath = "";

        private static CopyDependency window;

        [MenuItem("Window/CopyDependency")]
		private static void Init()
		{
			window = (CopyDependency)EditorWindow.GetWindow(typeof(CopyDependency));
			window.minSize = new Vector2(300, 300);
            window.titleContent = new GUIContent("CopyDependency");
		}

        private void OnGUI()
        {
            EditorGUILayout.LabelField(strCache2);
            oldFold = EditorGUILayout.ObjectField(oldFold, typeof(Object), false);
            if (oldFold != null)
            {
                if (oldFold.GetType() == typeof(DefaultAsset))
                {
                    oldPath = AssetDatabase.GetAssetPath((DefaultAsset)oldFold);
                }
            }

            EditorGUILayout.LabelField(strCache1);
            newFold = EditorGUILayout.ObjectField(newFold, typeof(Object), false);
            if (newFold != null)
            {
                if (newFold.GetType() == typeof(DefaultAsset))
                {
                    newPath = AssetDatabase.GetAssetPath((DefaultAsset)newFold);
                }
            }

            if (GUILayout.Button("Copy", GUILayout.Width(50), GUILayout.Height(50)))
            {
                GetDependencyMap();
                ReplaceDependencyGuid();
                window.Close();
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// key:old guid value:new guid
        /// </summary>
        private Dictionary<string, string> guidMap = new Dictionary<string, string>();
        private void GetDependencyMap()
        {
            guidMap.Clear();
            var fullPath = Path.GetFullPath(oldPath);
            //var filePaths = IOUtil.GetFiles(fullPath);
            var filePaths = GetAllFiles(fullPath);
            var fullPath2 = Path.GetFullPath(newPath);

            var length = fullPath.Length + 1;
            foreach (var filePath in filePaths)
            {
                string extension = Path.GetExtension(filePath);
                if (!ignoreList.Contains(extension))
                {
                    string assetPath = GetRelativeAssetPath(filePath);
                    string relativePath = filePath.Remove(0, length);
                    string guid = AssetDatabase.AssetPathToGUID(assetPath);
                    string copyPath = newPath + "/" + relativePath;
                    string copyGuid = AssetDatabase.AssetPathToGUID(copyPath);
                    if(copyGuid != null)
                    {
                        guidMap[guid] = copyGuid;
                        Debug.Log(guid + "\n" + copyGuid);
                    }
                }
            }
        }

        private void ReplaceDependencyGuid()
        {
            var fullPath = Path.GetFullPath(newPath);
            //var filePaths = IOUtil.GetFiles(fullPath);
            var filePaths = GetAllFiles(fullPath);
            foreach (var filePath in filePaths)
            {
                string extension = Path.GetExtension(filePath);
                if (includeList.Contains(extension))
                {
                    var assetPath = GetRelativeAssetPath(filePath);
                    string[] deps = AssetDatabase.GetDependencies(assetPath, true);
                    var fileString = File.ReadAllText(filePath);
                    bool bChanged = false;
                    foreach (var v in deps)
                    {
                        var guid = AssetDatabase.AssetPathToGUID(v);
                        if (guidMap.ContainsKey(guid))
                        {
                            if (Regex.IsMatch(fileString, guid))
                            {
                                fileString = Regex.Replace(fileString, guid, guidMap[guid]);
                                bChanged = true;
                                var oldFile = AssetDatabase.GUIDToAssetPath(guid);
                                var newFile = AssetDatabase.GUIDToAssetPath(guidMap[guid]);
                                Debug.Log(oldFile+"\nTo\n"+newFile);

                            }
                        }
                    }
                    if(bChanged){
                        File.WriteAllText(filePath, fileString);
                    }
                }
            }
        }

        private static string GetRelativeAssetPath(string fullPath)
        {
            fullPath = fullPath.Replace("\\", "/");
            int index = fullPath.IndexOf("Assets");
            string relativePath = fullPath.Substring(index);
            return relativePath;
        }

        private static string[] GetAllFiles(string fullPath)
        {
            List<string> files = new List<string>();
            foreach (string file in GetFiles(fullPath))
            {
                files.Add(file);
            }
            return files.ToArray();
        }

        private static IEnumerable<string> GetFiles(string path)
        {
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                try
                {
                    foreach (string subDir in Directory.GetDirectories(path))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                if (files != null)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        yield return files[i];
                    }
                }
            }
        }
    }
}
