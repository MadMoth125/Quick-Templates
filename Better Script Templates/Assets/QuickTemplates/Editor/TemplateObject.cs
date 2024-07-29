using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuickTemplates.Editor
{
	[Serializable]
	public class TemplateObject
	{
		[Tooltip("The menu path that this template can be created from.")]
		[SerializeField]
		private string menuPath;

		[Tooltip("The name of the file created from the template.")]
		[SerializeField]
		private string fileName;

		[Tooltip("The asset reference to the template text file.")]
		[SerializeField]
		private TextAsset template;

		/// <summary>
		/// The menu path that this template can be created from.
		/// </summary>
		public string MenuPath => menuPath;

		/// <summary>
		/// The name of the file created from the template.
		/// </summary>
		public string FileName => fileName;

		/// <summary>
		/// The asset reference to the template text file.
		/// </summary>
		public TextAsset Template => template;

		public TemplateObject()
		{
			menuPath = "Assets/Create/New File";
			fileName = "NewFile.txt";
		}

		public TemplateObject(string menuPath, string fileName, TextAsset template)
		{
			this.menuPath = menuPath;
			this.fileName = fileName;
			this.template = template;
		}

		/// <summary>
		/// Gets the path of the template file assigned for this ScriptableObject.
		/// </summary>
		public string GetTemplatePath()
		{
			#if UNITY_EDITOR
			if (template != null)
			{
				return AssetDatabase.GetAssetPath(template);
			}
			else
			{
				Debug.LogError($"{nameof(TemplateObject)}: 'Template' Object reference is null.");
			}
			#endif

			return "";
		}
	}
}