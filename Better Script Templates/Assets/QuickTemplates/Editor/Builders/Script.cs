using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuickTemplates.Tools;

namespace QuickTemplates.Builders
{
	internal class Script
	{
		private enum Modifier
		{
			None,
			Static,
			Abstract,
			Sealed,
		}

		private string _name;
		private Type _type;
		private string _namespace;
		private readonly List<(string content, bool isEditorOnly)> _methods;
		private readonly List<(string name, bool isEditorOnly)> _usingStatements;
		private bool _isPartial;
		private Modifier _modifier;
		private AccessModifier _access;

		private Script()
		{
			_name = string.Empty;
			_type = null;
			_namespace = string.Empty;
			_methods = new List<(string, bool)>();
			_usingStatements = new List<(string, bool)>();
			_isPartial = false;
			_modifier = Modifier.None;
			_access = AccessModifier.Public;
		}

		public class Builder
		{
			private Script _instance = new Script();

			#region Builder Methods

			public Builder WithName(string name)
			{
				_instance._name = name;
				return this;
			}

			public Builder WithNamespace(string name)
			{
				_instance._namespace = name;
				return this;
			}

			public Builder WithMethod(string method, bool isEditorOnly = false)
			{
				_instance._methods.Add((method, isEditorOnly));
				return this;
			}

			public Builder WithMethods(IEnumerable<string> methods)
			{
				_instance._methods.AddRange(methods.Select(m => (m, false)));
				return this;
			}

			public Builder WithMethods(IEnumerable<(string content, bool isEditorOnly)> methods)
			{
				_instance._methods.AddRange(methods);
				return this;
			}

			public Builder WithUsingStatement(string statement, bool isEditorOnly = false)
			{
				_instance._usingStatements.Add((statement, isEditorOnly));
				return this;
			}

			public Builder WithUsingStatements(IEnumerable<(string name, bool isEditorOnly)> statements)
			{
				_instance._usingStatements.AddRange(statements);
				return this;
			}

			public Builder WithUsingStatements(IEnumerable<string> statements)
			{
				_instance._usingStatements.AddRange(statements.Select(s => (s, false)));
				return this;
			}

			public Builder AsType(Type type)
			{
				_instance._type = type;
				return this;
			}

			public Builder AsPublic()
			{
				_instance._access = AccessModifier.Public;
				return this;
			}

			public Builder AsInternal()
			{
				_instance._access = AccessModifier.Internal;
				return this;
			}

			public Builder AsStatic()
			{
				_instance._modifier = Modifier.Static;
				return this;
			}

			public Builder AsSealed()
			{
				_instance._modifier = Modifier.Sealed;
				return this;
			}

			public Builder AsAbstract()
			{
				_instance._modifier = Modifier.Abstract;
				return this;
			}

			public Builder AsPartial()
			{
				_instance._isPartial = true;
				return this;
			}

			#endregion

			public Script Build()
			{
				// Ensure class has name
				if (string.IsNullOrWhiteSpace(_instance._name))
				{
					throw new Exception("ClassName is a required field and cannot be null or empty!");
				}

				return _instance;
			}
		}

		private string MakeClass(string contents)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(_access.ToString().ToLower() + " ");
			if (_modifier != Modifier.None) sb.Append(_modifier.ToString().ToLower() + " ");
			if (_isPartial) sb.Append("partial ");
			sb.Append("class ");
			if (_type != null)
			{
				sb.Append(_name);
				sb.Append(" : ");
				sb.AppendLine(_type.ToString());
			}
			else
			{
				sb.AppendLine(_name);
			}
			sb.AppendLine("{");
			sb.AppendLine(contents.Indent());
			sb.Append("}");

			return sb.ToString();
		}

		private string MakeNamespace(string contents)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("namespace ");
			sb.AppendLine(_namespace);
			sb.AppendLine("{");
			sb.AppendLine(contents.Indent());
			sb.Append("}");

			return sb.ToString();
		}

		private string MakeCombinedMethods()
		{
			StringBuilder sb = new StringBuilder();
			foreach (var method in _methods)
			{
				if (!method.isEditorOnly)
				{
					if (method.GetHashCode() != _methods[^1].GetHashCode())
					{
						sb.AppendLine(method.content);
					}
					else
					{
						sb.Append(method.content);
					}
				}
				else
				{
					if (method.GetHashCode() != _methods[^1].GetHashCode())
					{
						sb.AppendLine("#if UNITY_EDITOR")
							.AppendLine(method.content)
							.AppendLine("#endif");
					}
					else
					{
						sb.AppendLine("#if UNITY_EDITOR")
							.AppendLine(method.content)
							.Append("#endif");
					}
				}
			}

			return sb.ToString();
		}

		private string MakeCombinedUsingStatements()
		{
			if (_usingStatements.Count == 0) return string.Empty;

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < _usingStatements.Count; i++)
			{
				if (i < _usingStatements.Count - 1)
				{
					if (!_usingStatements[i].isEditorOnly)
					{
						sb.Append("using ");
						sb.Append(_usingStatements[i].name);
						sb.AppendLine(";");
					}
					else
					{
						sb.AppendLine("#if UNITY_EDITOR");
						sb.Append("using ");
						sb.Append(_usingStatements[i].name);
						sb.AppendLine(";");
						sb.AppendLine("#endif");
					}
				}
				else
				{
					if (!_usingStatements[i].isEditorOnly)
					{
						sb.Append("using ");
						sb.Append(_usingStatements[i].name);
						sb.Append(";");
					}
					else
					{
						sb.AppendLine("#if UNITY_EDITOR");
						sb.Append("using ");
						sb.Append(_usingStatements[i].name);
						sb.AppendLine(";");
						sb.Append("#endif");
					}
				}
			}

			return sb.ToString();
		}

		public override string ToString()
		{
			if (!string.IsNullOrWhiteSpace(_namespace))
			{
				return MakeCombinedUsingStatements() + (_usingStatements.Count > 0 ? "\n\n" : string.Empty)
				                                     + MakeNamespace(MakeClass(MakeCombinedMethods()));
			}
			else
			{
				return MakeCombinedUsingStatements() + (_usingStatements.Count > 0 ? "\n\n" : string.Empty)
				                                     + MakeClass(MakeCombinedMethods());
			}
		}
	}
}