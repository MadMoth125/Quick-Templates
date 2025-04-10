using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace QuickTemplates.Editor
{
	/// <summary>
	/// Simple class that exposes the main <see cref="TemplateConfigObject"/> in the project settings.
	/// </summary>
	public class TemplateSettingsProvider : SettingsProvider
	{
		private TemplateConfigObject _configObject;
		private SerializedObject _serializedObject;
		private Vector2 _scrollPosition;

		public TemplateSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
			: base(path, scopes, keywords)
		{
			// No additional code needed
		}

		public override void OnActivate(string searchContext, VisualElement rootElement)
		{
			_configObject = TemplateConfigObject.FindFirstAsset();
			_serializedObject = new SerializedObject(_configObject);
		}

		public override void OnGUI(string searchContext)
		{
			if (_configObject)
			{
				/*EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.LabelField($"Active configuration asset: '{_instancePath}'");
				EditorGUI.EndDisabledGroup();*/

				_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

				SerializedObject serializedObject = new SerializedObject(_configObject);
				SerializedProperty property = serializedObject.GetIterator();

				property.Next(true);
				while (property.NextVisible(false))
				{
					if (property.name == "m_Script") continue;
					EditorGUILayout.PropertyField(property, true);
				}

				serializedObject.ApplyModifiedProperties();

				EditorGUILayout.EndScrollView();
			}

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Generate Templates"))
			{
				TemplateConfigObject.StaticGenerate();
			}
			EditorGUILayout.EndHorizontal();
		}

		[SettingsProvider]
		public static SettingsProvider CreateTestSettings()
		{
			return new TemplateSettingsProvider("Project/Quick Templates", SettingsScope.Project);
		}
	}
}