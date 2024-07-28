using System;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuickTemplates.Editor
{
	/// <summary>
	/// Class wrapper for the <see cref="UnityEditor.Menu"/> class found in the UnityEditor.UI assembly.
	/// Uses reflection to expose necessary methods related to adding entries in the /Create menu.
	/// </summary>
	public static class Menu
	{
		/// <summary>
		/// Exposes internal Unity  method <see cref="UnityEditor.Menu.AddMenuItem"/> for public use.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="shortcut"></param>
		/// <param name="checked"></param>
		/// <param name="priority"></param>
		/// <param name="execute"></param>
		/// <param name="validate"></param>
		public static void AddMenuItem(string name, string shortcut, bool @checked, int priority, Action execute, Func<bool> validate)
		{
			#if UNITY_EDITOR
			// Equivalent to calling ' Menu.AddMenuItem(name, shortcut, @checked, priority, execute, validate); '
			GetReflectedMethod<UnityEditor.Menu>("AddMenuItem", new object[] { name, shortcut, @checked, priority, execute, validate, });
			#endif
		}

		/// <summary>
		/// Exposes internal Unity method <see cref="UnityEditor.Menu.RemoveMenuItem"/> for public use.
		/// </summary>
		/// <param name="name"></param>
		public static void RemoveMenuItem(string name)
		{
			#if UNITY_EDITOR
			// Equivalent to calling ' Menu.RemoveMenuItem(name); '
			GetReflectedMethod<UnityEditor.Menu>("RemoveMenuItem", new object[] { name, });
			#endif
		}

		/// <summary>
		/// Exposes internal Unity method <see cref="UnityEditor.Menu.MenuItemExists"/> for public use.
		/// </summary>
		/// <param name="menuPath"></param>
		public static bool MenuItemExists(string menuPath)
		{
			#if UNITY_EDITOR
			// Equivalent to calling ' return Menu.MenuItemExists(menuPath); '
			(MethodInfo methodInfo, object returnValue) result = GetReflectedMethod<UnityEditor.Menu>("MenuItemExists", new object[] { menuPath, });
			return result.returnValue != null && (bool)result.returnValue;
			#else
			return false;
			#endif
		}

		private static (MethodInfo, object) GetReflectedMethod<T>(string methodName, object[] parameters, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static)
		{
			MethodInfo info = typeof(T).GetMethod(methodName, bindingFlags);
			object value = info?.Invoke(typeof(T), parameters);
			return (info, value);
		}
	}
}