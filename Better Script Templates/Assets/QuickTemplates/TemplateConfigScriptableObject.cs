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

				templates.Add(new TemplateObject("Assets/Create/", templateInfo.Name + templateInfo.Extension, template));
			}

			templates = templates.OrderBy(t => Path.GetDirectoryName(t.GetTemplatePath())?.Replace("\\", "/")).ToList();
		}

		[ContextMenu("Refresh Menu")]
		public void RefreshMenu()
		{
			TemplateAssetManager.RequestScriptReload();
		}

		public static TemplateConfigScriptableObject GetTemplateConfigs()
		{
			var paths = AssetDatabase.FindAssets($"t:{typeof(TemplateConfigScriptableObject)}", null);
			var assets = paths.Select(AssetDatabase.LoadAssetAtPath<TemplateConfigScriptableObject>).ToList();
			return assets[0];

		}
	}
}