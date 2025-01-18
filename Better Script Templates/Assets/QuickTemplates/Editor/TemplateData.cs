using System;
using UnityEngine;

namespace QuickTemplates.Editor
{
	[Serializable]
	internal class TemplateData
	{
		public string path;

		public TextAsset asset;

		public TemplateData(string path, TextAsset asset)
		{
			this.path = path;
			this.asset = asset;
		}
	}
}