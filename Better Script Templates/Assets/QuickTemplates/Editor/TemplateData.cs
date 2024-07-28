namespace QuickTemplates.Editor
{
	public struct TemplateData
	{
		/// <summary>
		/// Asset directory path to the template.
		/// </summary>
		public readonly string Path;

		/// <summary>
		/// Name of the template. (Extensions type(s) excluded.)
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// Extension of the template. (".cs", ".json", etc...)
		/// </summary>
		public readonly string Extension;

		public TemplateData(string path, string name, string extension)
		{
			Path = path;
			Name = name;
			Extension = extension;
		}
	}
}