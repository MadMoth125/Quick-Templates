using System;
using System.Collections.Generic;
using System.Text;
using QuickTemplates.Tools;
using UnityEngine;

namespace QuickTemplates.Builders
{
	internal class Method
	{
		private enum Modifier
		{
			None,
			Static,
			Abstract,
			Virtual,
		}

		private string _name;
		private List<(Type type, string name)> _parameters;
		private Type _returnType;
		private List<string> _attributes;
		private string _logic;
		private Modifier _modifier;
		private AccessModifier _access;

		private Method()
		{
			_name = string.Empty;
			_parameters = new List<(Type, string)>();
			_returnType = null;
			_attributes = new List<string>();
			_logic = string.Empty;
			_modifier = Modifier.None;
			_access = AccessModifier.Public;
		}

		public class Builder
		{
			private Method _instance = new Method();

			#region Builder Methods

			public Builder WithName(string name)
			{
				_instance._name = name;
				return this;
			}

			public Builder WithInputParameter<T>(string name)
			{
				_instance._parameters.Add((typeof(T), name));
				return this;
			}

			public Builder WithInputParameter(Type type, string name)
			{
				_instance._parameters.Add((type, name));
				return this;
			}

			public Builder WithInputParameters(IEnumerable<(Type type, string name)> parameters)
			{
				_instance._parameters.AddRange(parameters);
				return this;
			}

			public Builder WithReturnType<T>()
			{
				_instance._returnType = typeof(T);
				return this;
			}

			public Builder WithReturnType(Type type)
			{
				_instance._returnType = type;
				return this;
			}

			public Builder WithAttribute(string attribute)
			{
				_instance._attributes.Add(attribute);
				return this;
			}

			public Builder WithMethodAttributes(IEnumerable<string> attributes)
			{
				_instance._attributes.AddRange(attributes);
				return this;
			}

			public Builder WithLogic(string logic)
			{
				if (!string.IsNullOrWhiteSpace(logic) && !logic.EndsWith(";"))
				{
					logic += ";";
				}
				_instance._logic = logic;
				return this;
			}

			public Builder AsPublic()
			{
				_instance._access = AccessModifier.Public;
				return this;
			}

			public Builder AsPrivate()
			{
				_instance._access = AccessModifier.Private;
				return this;
			}

			public Builder AsProtected()
			{
				_instance._access = AccessModifier.Protected;
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

			public Builder AsAbstract()
			{
				_instance._modifier = Modifier.Abstract;
				return this;
			}

			public Builder AsVirtual()
			{
				_instance._modifier = Modifier.Virtual;
				return this;
			}

			#endregion

			public Method Build()
			{
				if (string.IsNullOrWhiteSpace(_instance._logic))
				{
					Debug.LogWarning($"No logic found in '{_instance._name}', but method was still built");
				}

				return _instance;
			}
		}

		private string MakeMethod(string content)
		{
			StringBuilder sb = new StringBuilder();

			if (_attributes.Count > 0)
			{
				sb.AppendLine(MakeCombinedMethodAttributes());
			}

			sb.Append(_access.ToString().ToLower() + " ");
			if (_modifier != Modifier.None)
			{
				sb.Append(_modifier.ToString().ToLower() + " ");
			}

			if (_returnType != null)
			{
				sb.Append(_returnType + " ");
			}
			else
			{
				sb.Append("void ");
			}

			sb.Append(_name)
				.Append("(")
				.Append(MakeCombinedInputParameters())
				.AppendLine(")")
				.AppendLine("{")
				.AppendLine(content.Indent())
				.Append("}")
				;

			return sb.ToString();
		}

		private string MakeCombinedInputParameters()
		{
			if (_parameters.Count == 0) return string.Empty;

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < _parameters.Count; i++)
			{
				if (i < _parameters.Count - 1)
				{
					sb.Append(_parameters[i].type)
						.Append(" ")
						.Append(_parameters[i].name)
						.Append(", ");
				}
				else
				{
					sb.Append(_parameters[i].type)
						.Append(" ")
						.Append(_parameters[i].name);
				}
			}

			return sb.ToString();
		}

		private string MakeCombinedMethodAttributes()
		{
			if (_attributes.Count == 0) return string.Empty;

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < _attributes.Count; i++)
			{
				if (i < _attributes.Count - 1)
				{
					if (!_attributes[i].StartsWith("["))
					{
						sb.Append("[");
					}

					sb.Append(_attributes[i]);

					if (!_attributes[i].EndsWith("]"))
					{
						sb.AppendLine("]");
					}
				}
				else
				{
					if (!_attributes[i].StartsWith("["))
					{
						sb.Append("[");
					}

					sb.Append(_attributes[i]);

					if (!_attributes[i].EndsWith("]"))
					{
						sb.Append("]");
					}
				}
			}

			return sb.ToString();
		}

		public override string ToString()
		{
			return MakeMethod(_logic);
		}
	}
}