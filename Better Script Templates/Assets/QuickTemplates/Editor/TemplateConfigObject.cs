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
		/// Generates a C# script that creates custom menu items for asset templates.
		/// The script is created in a specified directory and includes methods for each
		/// template, allowing for easy asset creation from the Unity editor.
		/// </summary>
		[ContextMenu("Generate Templates")]
		private void Generate()
		{
			const string @namespace = "QuickTemplates.Editor.Generated";
			const string @class = "QuickTemplateMenuItems";
			const string scriptName = "QuickTemplateMenuItems.generated.cs";

			string desiredPath = ConstructDirectory();

			// Make sure directory exists before writing to it
			if (!Directory.Exists(desiredPath)) Directory.CreateDirectory(desiredPath);

			#region Namespace/Class Generation

			// Create root compile unit for other C# elements to be assigned
			CodeCompileUnit compileUnit = new CodeCompileUnit()
			{
				Namespaces =
				{
					new CodeNamespace() // Define namespace
					{
						Name = @namespace,
						Types =
						{
							new CodeTypeDeclaration() // Define class within namespace
							{
								Name = @class,
								IsClass = true,
								// Can't find a way to make class static, so internal abstract is the best I can do
								TypeAttributes = TypeAttributes.NotPublic | TypeAttributes.Abstract
							} // class end
						},
					} // namespace end
				}
			};

			#endregion

			#region Method Generation

			// Pull reference from compile unit for easier access
			CodeTypeDeclaration classDeclaration = compileUnit.Namespaces[0].Types[0];

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

			#region Writing Code to File

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
				File.WriteAllText(Path.Combine(desiredPath, scriptName), writer.GetStringBuilder().ToString());

				// Refresh assets to compile script
				AssetDatabase.Refresh();
			}

			#endregion

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

		#region Find Asset(s) Methods

		public static IEnumerable<string> FindAssetPaths()
		{
			return AssetDatabase.FindAssets($"t:{nameof(TemplateConfigObject)}").Select(AssetDatabase.GUIDToAssetPath);
		}

		public static TemplateConfigObject FindFirstAsset()
		{
			var paths = FindAssetPaths().ToArray();
			return paths.Length > 0 ? AssetDatabase.LoadAssetAtPath<TemplateConfigObject>(paths[0]) : null;
		}

		#endregion

		#region TemplateData List Options

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

		#endregion

		#region Menu Item Methods

		/// <summary>
		/// Creates instances of TemplateConfigObject via a MenuItem, allowing for validation
		/// to prevent duplicate assets in the project.
		/// </summary>
		[MenuItem(AssetCreatePath + "QuickTemplates/Template Configuration Asset", priority = int.MaxValue)]
		private static void CreateAsset()
		{
			// Find ALL asset paths so the user can check where duplicates are
			var instances = FindAssetPaths().ToArray();
			if (instances.Length > 0)
			{
				string combinedPaths = string.Join('\n', instances);
				Debug.LogWarning($"Creation failed! An instance of '{nameof(TemplateConfigObject)}' already exists in the project.");
				Debug.Log($"Instance(s) of '{nameof(TemplateConfigObject)}' found at:\n{combinedPaths}");
				return;
			}

			TemplateConfigObject asset = CreateInstance<TemplateConfigObject>();
			EditorUtility.SetDirty(asset);

			bool hasPath = EditorUtils.TryGetActiveFolderPath(out string path);
			if (!hasPath) path = "Assets";

			AssetDatabase.CreateAsset(asset, $"{path}/NewTemplateConfigAsset.asset");
			AssetDatabase.SaveAssetIfDirty(asset);
			EditorUtility.FocusProjectWindow();

			Selection.activeObject = asset;
		}

		[MenuItem("QuickTemplates/Generate Templates")]
		private static void StaticGenerate()
		{
			var inst = FindFirstAsset();
			if (!inst)
			{
				Debug.LogWarning($"Generation failed! No instance of '{nameof(TemplateConfigObject)}' found in the project.");
				return;
			}

			inst.Generate();
		}

		#endregion
	}
}