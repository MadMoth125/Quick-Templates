using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuickTemplates.Builders;
using QuickTemplates.Tools;
using UnityEngine;
using UnityEditor;

namespace QuickTemplates
{
	internal class TemplateConfigObject : ScriptableObject
	{
		[Serializable]
		public enum PathMode
		{
			Absolute,
			Relative,
		}

		[Serializable]
		public enum SortMethod
		{
			Path,
			Name,
			Extension,
		}

		public const string AssetCreatePath = "Assets/Create/";

		[Header("Template Generation")]
		[Tooltip("Absolute: Reads the given path as an absolute path starting from the 'Assets/' directory.\n\n" +
		         "Relative: Reads the given path relative to the directory of the ScriptableObject currently being used.")]
		public PathMode pathMode = PathMode.Relative;

		[Tooltip("Specifies the folder location where the auto-generated script will be saved.\n\n" +
		         "It is recommended to set this path to a folder or subfolder within an Editor Assembly " +
		         "(e.g., \"Assets/Editor/\") to ensure script is properly excluded from builds.")]
		public string folderPath = "/Editor/Generated/";

		[Header("Template Parsing")]
		public string templatePrefix = "Template_";

		public string defaultTemplatePath = "Templates/";

		[Header("Template Config")]
		public List<TemplateData> templates;

		public static IEnumerable<string> GetInstances()
		{
			return AssetDatabase.FindAssets($"t:{nameof(TemplateConfigObject)}").Select(AssetDatabase.GUIDToAssetPath);
		}

		[MenuItem(AssetCreatePath + "QuickTemplates/Template Configuration Asset", priority = int.MaxValue)]
		private static void CreateAsset()
		{
			var instances = GetInstances().ToArray();
			if (instances.Length > 0)
			{
				string combinedPaths = string.Join('\n', instances);
				Debug.LogWarning($"Cannot create multiple instances of type '{nameof(TemplateConfigObject)}' in project.");
				Debug.Log($"'{nameof(TemplateConfigObject)}' instance(s) found at: {combinedPaths}");
				return;
			}

			TemplateConfigObject asset = CreateInstance<TemplateConfigObject>();

			bool hasPath = EditorUtils.TryGetActiveFolderPath(out string path);
			if (!hasPath) path = "Assets";

			AssetDatabase.CreateAsset(asset, $"{path}/NewTemplateConfigAsset.asset");
			AssetDatabase.SaveAssets();
			EditorUtility.FocusProjectWindow();

			Selection.activeObject = asset;
		}

		[MenuItem("QuickTemplates/Re-generate Menu Items")]
		private static void GenerateFromFirstInstance()
		{
			var instances = GetInstances().ToArray();
			if (instances.Length == 0)
			{
				Debug.LogWarning($"Cannot create menu items, no instance of type '{nameof(TemplateConfigObject)}' in project.");
				return;
			}

			var instance = AssetDatabase.LoadAssetAtPath<TemplateConfigObject>(instances[0]);
			instance.Generate();
		}

		[ContextMenu("Re-generate Menu Items")]
		private void Generate()
		{
			string desiredPath = ConstructDirectory();

			// Make sure directory exists before writing to it
			if (!Directory.Exists(desiredPath)) Directory.CreateDirectory(desiredPath);

			Script script = new Script.Builder()
					.WithName("QuickTemplateMenuItems")
					.WithNamespace("QuickTemplates.Generated")
					.AsInternal()
					.AsStatic()
					.WithUsingStatement("UnityEngine")
					.WithUsingStatement("UnityEditor", isEditorOnly: true)
					.WithMethods(CreateMethods())
					.Build();

			// Create/overwrite script file at desired path
			File.WriteAllText(Path.Combine(desiredPath, "QuickTemplateMenuItems.generated.cs"), script.ToString());
			// Refresh assets to compile script
			AssetDatabase.Refresh();

			return;

			IEnumerable<(string, bool)> CreateMethods()
			{
				int count = 0;
				foreach (var data in templates)
				{
					// Unique GUID with invalid characters removed
					string identifier = GUID.Generate().ToSimpleString();

					// Derive info from asset for generated method
					string assetPath = AssetDatabase.GetAssetPath(data.asset);                             // Assets/.../Template_File.cs.txt
					string assetName = data.asset.name.Replace(" ", "_");                                  // Template_File.cs
					string assetNameExtensionless = Path.GetFileNameWithoutExtension(assetName); // Template_File
					string outFileName = $"New{assetName.Replace(templatePrefix, "")}";                    // NewFile.cs

					// Append 'Assets/Create/' to path if it doesn't already exist
					string menuPath = data.path.StartsWith(AssetCreatePath) ? data.path : AssetCreatePath + data.path;

					Method.Builder methodBuilder = new Method.Builder()
							.WithName($"Create{assetNameExtensionless.Replace(" ", "")}_{identifier}")
							.AsPrivate()
							.AsStatic()
							.WithLogic($@"ProjectWindowUtil.CreateScriptAssetFromTemplateFile(""{assetPath}"", ""{outFileName}"")")
							.WithAttribute($@"MenuItem(""{StringUtils.SanitizeDirectory(menuPath, endWithSeparator: false)}"", priority = {count++ - 100})")
						;

					yield return (methodBuilder.Build().ToString(), true);
				}
			}

			string ConstructDirectory(char separator = '/')
			{
				switch (pathMode)
				{
					case PathMode.Absolute:
					{
						string tempDirectory = StringUtils.SanitizeDirectory(folderPath);

						// Check if existing path already has "Assets/" included
						if (!tempDirectory.StartsWith("Assets/") && !tempDirectory.StartsWith($"Assets{Path.DirectorySeparatorChar}"))
						{
							return StringUtils.SanitizeDirectory("Assets/" + tempDirectory, separator);
						}

						return tempDirectory;
					}
					case PathMode.Relative:
					{
						string tempDirectory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this)) + "/";
						return StringUtils.SanitizeDirectory(tempDirectory + folderPath, separator);
					}
					default:
					{
						// Shouldn't reach this point
						return string.Empty;
					}
				}
			}
		}

		[ContextMenu("Find Templates")]
		private void FindTemplates()
		{
			// Ensure paths leads to '.txt' files,
			// and ensure file names have the prefix for templates (optional)
			var paths = AssetDatabase.FindAssets($"t:{nameof(TextAsset)}")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(s => s.EndsWith(".txt") && ContainsValidPrefix(s));

			foreach (var path in paths)
			{
				var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
				string assetName = asset.name.Replace(templatePrefix, "");
				string menuPath = StringUtils.SanitizeDirectory(defaultTemplatePath + "/" + assetName, endWithSeparator: false);
				templates.Add(new TemplateData(menuPath, asset));
			}

			return;

			bool ContainsValidPrefix(in string path)
			{
				return string.IsNullOrWhiteSpace(templatePrefix) || Path.GetFileName(path).StartsWith(templatePrefix);
			}
		}

		#region Template Sorting

		[ContextMenu("Templates/Sort by Path")]
		private void SortTemplatesByPath() => SortTemplates(SortMethod.Path);

		[ContextMenu("Templates/Sort by Name")]
		private void SortTemplatesByName() => SortTemplates(SortMethod.Name);

		[ContextMenu("Templates/Sort by Extension")]
		private void SortTemplatesByExtension() => SortTemplates(SortMethod.Extension);

		private void SortTemplates(SortMethod method)
		{
			if (templates.Count > 1)
			{
				templates.Sort((a, b) =>
				{
					switch (method)
					{
						default:
						case SortMethod.Name:
							return String.Compare(Path.GetFileName(a.path), Path.GetFileName(b.path), StringComparison.Ordinal);
						case SortMethod.Path:
							return String.Compare(Path.GetDirectoryName(a.path), Path.GetDirectoryName(b.path), StringComparison.Ordinal);
						case SortMethod.Extension:
							return String.Compare(Path.GetExtension(a.asset.name), Path.GetExtension(b.asset.name), StringComparison.Ordinal);
					}
				});
			}
			else
			{
				Debug.Log($"Cannot sort with '{templates.Count}' elements!");
			}
		}

		#endregion
	}
}