using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;
using QuickTemplates.Editor.Tools;
using UnityEditor;
using UnityEngine;

namespace QuickTemplates.Editor
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

		/// <summary>
		/// Finds all paths of TemplateConfigObject assets in project.
		/// </summary>
		/// <returns>A collection of strings representing paths of assets in the project folder.</returns>
		public static IEnumerable<string> GetInstances()
		{
			return AssetDatabase.FindAssets($"t:{nameof(TemplateConfigObject)}").Select(AssetDatabase.GUIDToAssetPath);
		}

		/// <summary>
		/// Static method with the MenuItem attribute for creating TemplateConfigObject instances,
		/// mimicking the functionality of the CreateAssetMenu attribute for ScriptableObjects.
		/// A dedicated method is used instead of class attribute, so we can have validation when creating instances.
		/// </summary>
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

		/// <summary>
		/// Generates a C# script that creates custom "Assets/Create" menu items based on the defined templates
		/// from the first found TemplateConfigObject instance.
		/// </summary>
		[MenuItem("QuickTemplates/Generate Templates")]
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

		/// <summary>
		/// Generates a C# script that creates custom "Assets/Create" menu items based on the defined templates.
		/// </summary>
		[ContextMenu("Generate Templates")]
		private void Generate()
		{
			string desiredPath = ConstructDirectory();

			// Make sure directory exists before writing to it
			if (!Directory.Exists(desiredPath)) Directory.CreateDirectory(desiredPath);

			// Create root compile unit for other C# elements to be assigned
			CodeCompileUnit compileUnit = new CodeCompileUnit()
			{
				Namespaces =
				{
					new CodeNamespace() // Define namespace
					{
						Name = "QuickTemplates.Editor.Generated",
						Imports =
						{
							// No need to import anything
						},
						Types =
						{
							new CodeTypeDeclaration() // Define class within namespace
							{
								Name = "QuickTemplateMenuItems",
								IsClass = true,
								// Can't find a way to make class static, so internal abstract is the best I can do
								TypeAttributes = TypeAttributes.NotPublic | TypeAttributes.Abstract
							} // class end
						},
					} // namespace end
				}
			};

			// Pull reference(s) from compile unit for name/namespace to have easier access
			CodeNamespace namespaceDeclaration = compileUnit.Namespaces[^1];
			CodeTypeDeclaration classDeclaration = compileUnit.Namespaces[^1].Types[^1];

			#region Method Generation

			for (var i = 0; i < templates.Count; i++)
			{
				// Unique GUID with invalid characters removed
				string identifier = GUID.Generate().ToSimpleString();

				// Derive info from asset for generated method
				string assetPath = AssetDatabase.GetAssetPath(templates[i].asset); // Assets/.../Template_File.cs.txt
				string assetName = templates[i].asset.name.Replace(" ", "_"); // Template_File.cs
				string assetNameExtensionless = Path.GetFileNameWithoutExtension(assetName); // Template_File
				string outFileName = $"New{assetName.Replace(templatePrefix, "")}"; // NewFile.cs

				// Append 'Assets/Create/' to path if it doesn't already exist
				string menuPath = templates[i].path.StartsWith(AssetCreatePath) ? templates[i].path : AssetCreatePath + templates[i].path;

				// Define method base with custom name & method attributes
				var method = new CodeMemberMethod()
				{
					Name = $"Create{assetNameExtensionless.Replace(" ", "")}_{identifier}",
					// ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
					Attributes = MemberAttributes.Private | MemberAttributes.Static,
					CustomAttributes =
					{
						new CodeAttributeDeclaration() // Define attribute [MenuItem()]
						{
							Name = typeof(UnityEditor.MenuItem).ToString(),
							Arguments =
							{
								// 'Assets/Create/' menu path
								new CodeAttributeArgument()
								{
									Value = new CodePrimitiveExpression(
										$"{StringUtils.SanitizeDirectory(menuPath, endWithSeparator: false)}")
								},
								// Menu order
								new CodeAttributeArgument()
								{
									Name = "priority",
									Value = new CodePrimitiveExpression(i - 100)
								}
							}
						} // attribute end [MenuItem()]
					},
					Statements =
					{
						new CodeMethodInvokeExpression(
								new CodeTypeReferenceExpression(typeof(UnityEditor.ProjectWindowUtil)), // Type to call method from
								nameof(UnityEditor.ProjectWindowUtil.CreateScriptAssetFromTemplateFile)) // Method name to call from type
							{
								Parameters =
								{
									new CodePrimitiveExpression($"{assetPath}"), // Template asset location
									new CodePrimitiveExpression($"{outFileName}"), // Output file location
								}
							}
					}
				};

				// Add method to outer class
				classDeclaration.Members.Add(method);

				// Add preprocessor directive to ensure method is editor-only
				method.StartDirectives.Add(new CodeRegionDirective()
				{
					RegionMode = CodeRegionMode.Start,
					RegionText = "#if UNITY_EDITOR"
				});

				method.EndDirectives.Add(new CodeRegionDirective()
				{
					RegionMode = CodeRegionMode.End,
					RegionText = "#endif"
				});
			}

			#endregion

			// Begin generating C# code from compile unit
			var provider = new CSharpCodeProvider();
			using (var writer = new StringWriter())
			{
				var options = new CodeGeneratorOptions()
				{
					IndentString = "\t", // Ensure tabs are used instead of spaces
					BracingStyle = "C",  // Ensure brackets are placed on new lines
				};

				// Assign code to the current StringWriter
				provider.GenerateCodeFromCompileUnit(compileUnit, writer, options);

				// Create/overwrite script file at desired path
				File.WriteAllText(Path.Combine(desiredPath, "QuickTemplateMenuItems.generated.cs"), writer.GetStringBuilder().ToString());

				// Refresh assets to compile script
				AssetDatabase.Refresh();
			}

			return;

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

		/// <summary>
		/// Finds all valid text file templates in the project, filters them based on an optional prefix,
		/// and adds their data to the templates list with estimated menu paths.
		/// </summary>
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