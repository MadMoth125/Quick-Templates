using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuickTemplates.Editor
{
	public static class TemplateManager
	{
		public const string RootDirectory = "Assets/";
		public const string TemplatePrefix = "Template_";
		public const string AssetFilter = "t:TextAsset";

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

		[InitializeOnLoadMethod]
		private static void Initialize()
		{
			EditorApplication.delayCall += PopulateMenus;
		}

		private static void PopulateMenus()
		{
			if (!TemplateUtils.GetConfigAssets(out List<TemplateConfigScriptableObject> config))
			{
				Debug.LogError($"No '{typeof(TemplateConfigScriptableObject)}' instances found in project.");
				return;
			}

			if (config.Count > 1)
			{
				Debug.LogWarning($"Multiple '{typeof(TemplateConfigScriptableObject)}' instances found in project.");
			}

			foreach (var template in config[0].templates)
			{
				Menu.AddMenuItem(name: template.MenuPath,
				                 shortcut: "",
				                 @checked: false,
				                 priority: config[0].templates.IndexOf(template) - 100,
				                 () => CreateFileFromTemplate(template.Template, template.FileName),
				                 () => true);
			}

			EditorApplication.delayCall -= PopulateMenus;
		}
	}
}