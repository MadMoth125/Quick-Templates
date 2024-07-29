using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuickTemplates.Editor
{
	[CreateAssetMenu(fileName = "NewTemplateConfiguration", menuName = "QuickTemplates/Template Configuration")]
	public class TemplateConfigScriptableObject : ScriptableObject
	{
		public List<TemplateObject> templates;

		[ContextMenu("Load Templates")]
		public void LoadTemplates()
		{
			#if UNITY_EDITOR
			foreach (string path in TemplateUtils.GetTemplateAssetPaths())
			{
				if (templates.Any(t => t.GetTemplatePath() == path)) continue;

				var template = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
				if (template == null) continue;

				string nonPrefixName = TemplateUtils.GetTemplateName(path, includePrefix: false);
				string templateExtension = TemplateUtils.GetTemplateExtension(path);
				templates.Add(new TemplateObject("Assets/Create/Templates/" + nonPrefixName, $"New{nonPrefixName}{templateExtension}", template));
			}
			#endif
		}

		[ContextMenu("Sort Order")]
		public void SortOrder()
		{
			templates = templates.OrderBy(t => Path.GetDirectoryName(t.MenuPath)).ToList();
		}

		[ContextMenu("Refresh Menu")]
		public void RefreshMenu()
		{
			TemplateManager.RequestScriptReload();
		}
	}
}