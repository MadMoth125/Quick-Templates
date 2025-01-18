using System;
using System.Text.RegularExpressions;
using UnityEditor;

namespace QuickTemplates.Editor.Tools
{
	internal static class GuidExtensions
	{
		public static string ToSimpleString(this Guid self, string patternOverride = null)
		{
			return Regex.Replace(self.ToString(), patternOverride ?? "[^a-zA-Z0-9_]", "");
		}

		public static string ToSimpleString(this GUID self, string patternOverride = null)
		{
			return Regex.Replace(self.ToString(), patternOverride ?? "[^a-zA-Z0-9_]", "");
		}
	}
}