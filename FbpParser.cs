using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace NTransit {
	public static class FbpParser {
		const string COMMENT = @"^#.*$";
		const string COMPONENT_DECLARATION = @"^[A-Za-z]+(\w|\(|\)|_)*\.[A-Za-z]+(\w|\(|\)|_)*$";
		const string COMPONENT_WITH_TYPE_DECLARATION = @"(\w+)\((\w+)\)";
		const string INITIAL_STRING_DATA = @"^('|"")(.+)\1$";
		const string INITIAL_FLOAT_DATA = @"^\d+\.\d+$";
		const string INITIAL_INT_DATA = @"^\d+$";
		const string INITIAL_PASSED_IN_DATA = @"\<(.+)\>";

		static Dictionary<string, Component> components;

		public static SingleThreadedScheduler Parse(string fbpProgram) {
			return Parse(fbpProgram, null);
		}

		public static SingleThreadedScheduler Parse(string fbpProgram, Dictionary<string, object> initialData) {
			components = new Dictionary<string, Component>();
			var scheduler = new SingleThreadedScheduler();
			var lines = fbpProgram.Split('\n');
			if (initialData == null) {
				initialData = new Dictionary<string, object>();
			}

			for (var i = 0; i < lines.Length; ++i) {
				var line = lines[i].Trim();
				if (Regex.IsMatch(line, COMMENT)) {
					continue;
				}

				var separatorIndex = line.IndexOf("=>");
				if (separatorIndex < 0) {
					continue;
				}

				var senderText = line.Substring(0, separatorIndex).Trim();
				var receiverText = line.Substring(separatorIndex + 2).Trim();

				var receiverParts = receiverText.Split('.');
				if (receiverParts.Length != 2) {
					throw new ArgumentException(string.Format("Invalid input on line {0}, receiver declaration is malformed: '{1}'", i, receiverText));
				}
				
				var receiver = CreateOrRetrieveComponentFromString(receiverParts[0].Trim(), i, scheduler);
				var receiverPort = receiverParts[1].Trim();

				if (Regex.IsMatch(senderText, COMPONENT_DECLARATION)) {
					var senderParts = senderText.Split('.');

					var sender = CreateOrRetrieveComponentFromString(senderParts[0].Trim(), i, scheduler);
					var senderPort = senderParts[1].Trim();
					sender.ConnectTo(senderPort, receiver, receiverPort);
				}
				else if (Regex.IsMatch(senderText, INITIAL_PASSED_IN_DATA)) {
					var match = Regex.Match(senderText, INITIAL_PASSED_IN_DATA);
					var key = match.Groups[1].Value;
					if (initialData.ContainsKey(key)) {
						receiver.SetInitialData(receiverPort, initialData[key]);
					}
					else {
						throw new ArgumentException(string.Format("Initial data {0} was not found in the provided Dictionary", key));
					}
				}
				else if (Regex.IsMatch(senderText, INITIAL_STRING_DATA)) {
					var match = Regex.Match(senderText, INITIAL_STRING_DATA);
					receiver.SetInitialData(receiverPort, match.Groups[2].Value);
				}
				else if (Regex.IsMatch(senderText, INITIAL_FLOAT_DATA)) {
					var match = Regex.Match(senderText, INITIAL_FLOAT_DATA);
					receiver.SetInitialData(receiverPort, float.Parse(match.Value));
				}
				else if (Regex.IsMatch(senderText, INITIAL_INT_DATA)) {
					var match = Regex.Match(senderText, INITIAL_INT_DATA);
					receiver.SetInitialData(receiverPort, int.Parse(match.Value));
				}
				else {
					throw new ArgumentException(string.Format("Invalid input on line {0}, sender declaration is malformed: '{1}'", i, senderText));
				}
			}

			return scheduler;
		}

		static Component CreateOrRetrieveComponentFromString(string stringVersionOfComponent, int sourceLineNumber, SingleThreadedScheduler scheduler) {
			string componentName;
			string typeName = null;

			if (Regex.IsMatch(stringVersionOfComponent, COMPONENT_WITH_TYPE_DECLARATION)) {
				var match = Regex.Match(stringVersionOfComponent, COMPONENT_WITH_TYPE_DECLARATION);
				componentName = match.Groups[1].Value;
				typeName = match.Groups[2].Value;
			}
			else {
				componentName = stringVersionOfComponent;
			}

			if (typeName != null) {
				if (components.ContainsKey(componentName)) {
					throw new ArgumentException(string.Format("Invalid input on line {0}, process '{1}' has already been declared", sourceLineNumber, componentName));
				}
				else {
					Type resolvedType = null;
					var assemblies = AppDomain.CurrentDomain.GetAssemblies();
					foreach (var assembly in assemblies) {
						var types = assembly.GetTypes();
						foreach (var type in types) {
							if (type.Name.EndsWith(typeName)) {
								resolvedType = type;
							}
						}
					}

					if (resolvedType == null) {
						throw new ArgumentException(string.Format("Invalid input on line {0}, the type '{1}'declared for process '{2}' could not be found", sourceLineNumber, typeName, componentName));
					}
					if (!typeof(Component).IsAssignableFrom(resolvedType)) {
						throw new ArgumentException(string.Format("Invalid input on line {0}, the type '{1}'declared for process '{2}' is not a component", sourceLineNumber, typeName, componentName));
					}

					var component = (Component)System.Activator.CreateInstance(resolvedType, new [] { componentName });
					components[componentName] = component;
					scheduler.AddProcess(component);
				}
			}

			return components[componentName];
		}
	}
}