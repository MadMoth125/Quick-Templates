using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuickTemplates.Editor
{
	public static class TemplateAssetManager
	{
		private const string RootDirectory = "Assets/";
		private const string TemplatePrefix = "Template_";
		private const string AssetFilter = "t:TextAsset";

		public static void CreateFileFromTemplate(TextAsset file, string name)
		{
			#if UNITY_EDITOR
			string path = AssetDatabase.GetAssetPath(file);
			if (string.IsNullOrEmpty(path)) return;
			ProjectWindowUtil.CreateScriptAssetFromTemplateFile(path, name);
			#endif
		}

		public static void RequestScriptReload()
		{
			#if UNITY_EDITOR
			if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
			EditorUtility.RequestScriptReload();
			#endif
		}

		public static List<string> GetTemplatePaths(params string[] paths)
		{
			var results = new List<string>();

			#if UNITY_EDITOR
			// Find GUIDs of all text files in project.
			string[] guids = AssetDatabase.FindAssets(AssetFilter, paths);

			foreach (string guid in guids)
			{
				// Get path to text file.
				string templatePath = AssetDatabase.GUIDToAssetPath(guid);

				// Skip paths that don't start with the specified directory.
				if (templatePath.Substring(0, RootDirectory.Length) != RootDirectory)
				{
					continue;
				}

				// Isolate name from path and determine of it is a valid template file name.
				if (IsTemplate(Path.GetFileName(templatePath)))
				{
					results.Add(templatePath);
				}
			}

			#endif

			return results;
		}

		public static TemplateAssetInfo GetTemplateAssetInfoFromPath(string path)
		{
			// The directory of the path, excluding file.
			// Replacing back-slashes with forward-slashes to adhere to Unity's directory structure.
			// [Assets/Scripts/]TemplateFile.cs.txt
			// string pathOnly = Path.GetDirectoryName(path)?.Replace("\\", "/") + "/";

			// The name of the file, including the extension relevant to the template.
			// Assets/Scripts/[TemplateFile.cs].txt
			string fullTemplateName = Path.GetFileNameWithoutExtension(path);

			// The name of the file, excluding any extensions.
			// Assets/Scripts/[TemplateFile].cs.txt
			string onlyName = Path.GetFileNameWithoutExtension(fullTemplateName);

			// The extension relevant to the template.
			// Assets/Scripts/TemplateFile[.cs].txt
			string templateExtension = Path.GetExtension(fullTemplateName);

			return new TemplateAssetInfo(path, onlyName, templateExtension);
		}

		private static bool IsTemplate(string name)
		{
			const string expectedExtension = ".txt";

			// Checks if the name contains the proper "Template_" keyword. (Case-insensitive)
			// [Template_]File.cs.txt, [template_]File.cs.txt
			if (!name.Contains(TemplatePrefix, StringComparison.OrdinalIgnoreCase)) return false;
			string prefix = name.Substring(0, TemplatePrefix.Length);
			bool correctPrefix = prefix == TemplatePrefix || prefix == TemplatePrefix.ToLower();

			// Checks if the name contains multiple extensions for valid template.
			// Template_File[.]cs[.]txt = true, Template_File[.]txt = false
			bool validCount = name.Count(c => c == '.') > 1;

			bool textFile = name.EndsWith(expectedExtension);
			return correctPrefix && validCount && textFile;
		}

		[Obsolete("Use Path.GetFileName() instead.")]
		private static string GetFileNameFromPath(string path)
		{

			// Gets the last sub-folder the path is located at.
			// Assets/Scripts[/]TemplateFile.cs.txt
			int fileNameStart = path.LastIndexOf("/", StringComparison.Ordinal);

			// Return only the file name.
			// Assets/Scripts/[TemplateFile.cs.txt]
			return fileNameStart < 0 ? "" : path.Substring(fileNameStart + 1);
		}
	}
}