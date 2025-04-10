using System.Reflection;
using UnityEditor;

namespace QuickTemplates.Editor.Tools
{
	internal static class EditorUtils
	{
		public static bool TryGetActiveFolderPath(out string path)
		{
			path = string.Empty;
			const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;

			// Use reflection to access the private method TryGetActiveFolderPath
			MethodInfo methodInfo = typeof(ProjectWindowUtil).GetMethod("TryGetActiveFolderPath", flags);

			if (methodInfo != null)
			{
				object[] args = { null };
				bool success = (bool)methodInfo.Invoke(null, args);
				if (success)
				{
					path = args[0] as string;
				}
				return success;
			}

			return false;
		}
	}
}