#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuickTemplates.Editor
{
	public static class TemplateMenuManager
	{
		[InitializeOnLoadMethod]
		private static void Initialize()
		{
			EditorApplication.delayCall += PopulateMenus;
		}

		private static void PopulateMenus()
		{
			var config = TemplateConfigScriptableObject.GetTemplateConfigs();
			if (config == null) return;

			foreach (var template in config.templates)
			{
				Menu.AddMenuItem(name: template.MenuPath,
					shortcut: "",
					@checked: false,
					priority: config.templates.IndexOf(template) - 100,
					() => TemplateAssetManager.CreateFileFromTemplate(template.Template, template.FileName),
					() => true);
			}

			EditorApplication.delayCall -= PopulateMenus;
		}
	}
}