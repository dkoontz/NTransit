using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace NTransit {
	public class FbpParser {
		const string COMPONENT_DECLARATION = @"^[A-Za-z]+(\w|\(|\)|_)*\.[A-Za-z]+(\w|\(|\)|_)*$";
		const string INITIAL_STRING_DATA = @"^('|"")(.+)\1$";
		const string INITIAL_FLOAT_DATA = @"^\d+\.\d+$";
		const string INITIAL_INT_DATA = @"^\d+$";
		const string COMPONENT_WITH_TYPE_DECLARATION = @"(\w+)\((\w+)\)";
		Dictionary<string, Component> components = new Dictionary<string, Component>();

		public SingleThreadedScheduler Parse(string fbpProgram) {
			var scheduler = new SingleThreadedScheduler();
			var lines = fbpProgram.Split('\n');

			for (var i = 0; i < lines.Length; ++i) {
				var line = lines[i].Trim();
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

		Component CreateOrRetrieveComponentFromString(string stringVersionOfComponent, int sourceLineNumber, SingleThreadedScheduler scheduler) {
			string name;
			string type = null;

			if (Regex.IsMatch(stringVersionOfComponent, COMPONENT_WITH_TYPE_DECLARATION)) {
				var match = Regex.Match(stringVersionOfComponent, COMPONENT_WITH_TYPE_DECLARATION);
				name = match.Groups[1].Value;
				type = match.Groups[2].Value;
			}
			else {
				name = stringVersionOfComponent;
			}

			if (type != null) {
				if (components.ContainsKey(name)) {
					throw new ArgumentException(string.Format("Invalid input on line {0}, process '{1}' has already been declared", sourceLineNumber, name));
				}
				else {
					try {
						var component = (Component)System.Activator.CreateInstance(Type.GetType("NTransit." + type, true), new [] { name });
						components[name] = component;
						scheduler.AddProcess(component);
					}
					catch (TypeLoadException) {
						throw new ArgumentException(string.Format("Invalid input on line {0}, the type '{1}'declared for process '{2}' could not be found", sourceLineNumber, type, name));
					}
					catch (InvalidCastException) {
						throw new ArgumentException(string.Format("Invalid input on line {0}, the type '{1}'declared for process '{2}' is not a component", sourceLineNumber, type, name));
					}
				}
			}

			return components[name];
		}
	}
}