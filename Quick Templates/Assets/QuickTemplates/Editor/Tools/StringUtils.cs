using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace QuickTemplates.Editor.Tools
{
	internal static class StringUtils
	{
		/*
		 * Thanks to Filippo Bottega on StackOverflow for the reference
		 * https://stackoverflow.com/questions/22875444/indent-multiple-lines-of-text
		 */
		public static string Indent(this string input, int count, char character = '\t')
		{
			string finalIndent = string.Empty.PadLeft(count, character);
			return string.Join(Environment.NewLine, input.Split(Environment.NewLine).Select(item => string.IsNullOrEmpty(item.Trim()) ? item : finalIndent + item));
		}

		public static string Indent(this string input, char character = '\t')
		{
			return string.Join(Environment.NewLine, input.Split(Environment.NewLine).Select(item => string.IsNullOrEmpty(item.Trim()) ? item : character + item));
		}

		public static string SanitizeDirectory(string input, char separator = '/', bool startWithSeparator = false, bool endWithSeparator = true)
		{
			if (string.IsNullOrEmpty(input))
			{
				throw new ArgumentException("Input path cannot be null or empty.");
			}

			string separatorAsString = separator.ToString();

			// Replace invalid characters with an underscore
			string sanitizedPath = string.Join("_", input.Split(Path.GetInvalidPathChars()));

			// Normalize directory separators to the system's preferred separator
			sanitizedPath = sanitizedPath.Replace(Path.DirectorySeparatorChar, separator)
				.Replace('\\', separator)
				.Replace('/', separator);

			// Remove consecutive directory separators
			string regexPattern = $"{Regex.Escape(separatorAsString)}+";
			sanitizedPath = Regex.Replace(sanitizedPath, regexPattern, separatorAsString);

			// Ensure the path starts/doesn't start with a directory separator
			if (startWithSeparator)
			{
				if (!sanitizedPath.StartsWith(separatorAsString))
				{
					sanitizedPath = separatorAsString + sanitizedPath;
				}
			}
			else
			{
				if (sanitizedPath.StartsWith(separatorAsString))
				{
					sanitizedPath = sanitizedPath.Substring(1);
				}
			}

			// Ensure the path ends/doesn't end with a directory separator
			if (endWithSeparator)
			{
				if (!sanitizedPath.EndsWith(separatorAsString))
				{
					sanitizedPath += separatorAsString;
				}
			}
			else
			{
				if (sanitizedPath.EndsWith(separatorAsString))
				{
					sanitizedPath = sanitizedPath.Substring(0, sanitizedPath.Length - 1);
				}
			}

			return sanitizedPath;
		}
	}
}