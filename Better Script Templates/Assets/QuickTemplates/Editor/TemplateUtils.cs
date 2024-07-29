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
	public static class TemplateUtils
	{
		public static List<string> GetTemplateAssetPaths(string[] paths = null)
		{
			var results = new List<string>();

			#if UNITY_EDITOR
			// Find GUIDs of all text files in project.
			// If the 'paths' param in left null, it searches only the directory specified in the manager.
			string[] guids = AssetDatabase.FindAssets(TemplateManager.AssetFilter, paths ?? new[] { TemplateManager.RootDirectory, });

			foreach (string guid in guids)
			{
				// Get path to text file.
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);

				// Determine if it is a valid template file based on name.
				if (IsTemplate(assetPath))
				{
					results.Add(assetPath);
				}
			}
			#endif

			return results;
		}

		#pragma warning disable CS0162
		public static bool IsTemplate(TextAsset asset)
		{
			#if UNITY_EDITOR
			return IsTemplate(AssetDatabase.GetAssetPath(asset));
			#endif
			return false;
		}

		public static bool IsTemplate(string path)
		{
			if (string.IsNullOrEmpty(path)) return false;

			// Get substring of everything beyond the directory.
			string fileName = path.Substring(GetTemplateDirectory(path).Length);

			// Check if file name contains correct prefix.
			// Check if it's located at the beginning of the string.
			bool containsPrefix = fileName.ToLower().Contains(TemplateManager.TemplatePrefix.ToLower()) &&
			                      fileName.Substring(0, TemplateManager.TemplatePrefix.Length).ToLower() == TemplateManager.TemplatePrefix.ToLower();

			// Checks if the file name contains multiple extensions.
			// Template_File[.]cs[.]txt = true, Template_File[.]txt = false
			bool hasMultipleExtensions = fileName.Count(c => c == '.') > 1;

			// Checks if the true extension of the file is a ".txt" so the template can be read properly.
			bool isTextFile = fileName.EndsWith(".txt");

			return containsPrefix && hasMultipleExtensions && isTextFile;
		}

		public static string GetTemplateDirectory(string path)
		{
			#if UNITY_EDITOR
			return Path.GetDirectoryName(path)?.Replace("\\", "/") + "/";
			#endif
			return "";
		}

		public static string GetTemplateName(string path, bool includePrefix = true)
		{
			#if UNITY_EDITOR
			string fullName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path));
			if (includePrefix) return fullName;

			// Check if file name contains correct prefix.
			// Check if it's located at the beginning of the string.
			bool containsPrefix = fullName.ToLower().Contains(TemplateManager.TemplatePrefix.ToLower()) &&
			                      fullName.Substring(0, TemplateManager.TemplatePrefix.Length).ToLower() == TemplateManager.TemplatePrefix.ToLower();

			return containsPrefix ? fullName.Substring(TemplateManager.TemplatePrefix.Length) : fullName;
			#endif
			return "";
		}

		public static string GetTemplateExtension(string path)
		{
			#if UNITY_EDITOR
			return Path.GetExtension(Path.GetFileNameWithoutExtension(path));
			#endif
			return "";
		}

		public static bool GetConfigAssets(out List<TemplateConfigScriptableObject> assets)
		{
			assets = GetConfigAssets();
			return assets != null && assets.Count > 0;
		}

		public static List<TemplateConfigScriptableObject> GetConfigAssets()
		{
			#if UNITY_EDITOR
			return AssetDatabase.FindAssets($"t:{typeof(TemplateConfigScriptableObject)}", new string[] { TemplateManager.RootDirectory, })
			                    .Select(AssetDatabase.GUIDToAssetPath)
			                    .Select(AssetDatabase.LoadAssetAtPath<TemplateConfigScriptableObject>)
			                    .ToList();
			#endif
			return new List<TemplateConfigScriptableObject>();
		}
		#pragma warning restore CS0162
	}
}