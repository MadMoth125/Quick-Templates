using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuickTemplates.Editor;
using UnityEditor;
using UnityEngine;

namespace QuickTemplates
{
	[CreateAssetMenu(fileName = "NewTemplateConfig", menuName = "QuickTemplates/Template Config")]
	public class TemplateConfigScriptableObject : ScriptableObject
	{
		public List<TemplateObject> templates;

		[ContextMenu("Load Templates")]
		public void LoadTemplates()
		{
			List<string> paths = TemplateAssetManager.GetTemplatePaths(paths: null);
			foreach (string path in paths)
			{
				if (templates.Any(t => t.GetTemplatePath() == path)) continue;

				TemplateAssetInfo templateInfo = TemplateAssetManager.GetTemplateAssetInfoFromPath(path);
				TextAsset template = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
				if (template == null) continue;

				string trimmedName = templateInfo.Name.Substring(templateInfo.Name.IndexOf("_", StringComparison.Ordinal) + 1);
				templates.Add(new TemplateObject("Assets/Create/" + trimmedName, $"New{trimmedName}{templateInfo.Extension}", template));
			}
		}

		[ContextMenu("Sort Order")]
		public void SortOrder()
		{
			templates = templates.OrderBy(t => Path.GetDirectoryName(t.MenuPath)).ToList();
		}

		[ContextMenu("Refresh Menu")]
		public void RefreshMenu()
		{
			TemplateAssetManager.RequestScriptReload();
		}

		public static TemplateConfigScriptableObject GetTemplateConfigs()
		{
			List<TemplateConfigScriptableObject> assets = AssetDatabase.FindAssets($"t:{typeof(TemplateConfigScriptableObject)}", null)
			                                                           .Select(AssetDatabase.GUIDToAssetPath)
			                                                           .Select(AssetDatabase.LoadAssetAtPath<TemplateConfigScriptableObject>)
			                                                           .ToList();
			return assets[0];

		}
	}
}