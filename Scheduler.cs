using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace nTransit {
	public class Scheduler {
		public static void Main() {
			var scheduler = new Scheduler();

			var consoleWriter = new ConsoleWriter("Console Output");
			scheduler.AddComponent(consoleWriter);

//			var reader = new FileReader ("Get File Contents");
//			scheduler.AddComponent (reader);
//
//			var writer = new FileWriter ("Write File Contents");
//			scheduler.AddComponent (writer);

			var rng = new RandomNumberGenerator("Number generator");
			scheduler.AddComponent(rng);

			var delay = new Delay("Delayer");
			scheduler.AddComponent(delay);

			scheduler.SetupPorts();

//			scheduler.Connect (reader, "File Contents", writer, "Text To Write");
//			scheduler.Connect (reader, "Errors", consoleWriter, "In");
//			scheduler.Connect (writer, "Errors", consoleWriter, "In");
//			scheduler.SetInitialData (reader, "File Name", "test1.txt");
//			scheduler.SetInitialData (writer, "File Name", "test2.txt");

			scheduler.Connect(rng, "Number", delay, "In");
			scheduler.Connect(delay, "Out", consoleWriter, "In");
			scheduler.SetInitialData(delay, "Seconds Between Packets", .5f);

			scheduler.Go();
		}

		List<Component> components;
		Dictionary<Component, IEnumerator> componentExecutionState;
		LinkedList<Component> componentsThatHaveTerminated;
		Dictionary<Component, bool> currentlyRunningComponents;
		Stopwatch stopwatch;
		long lastUpdateTime;

		public Scheduler() {
			components = new List<Component>();
			componentExecutionState = new Dictionary<Component, IEnumerator>();
			componentsThatHaveTerminated = new LinkedList<Component>();
			currentlyRunningComponents = new Dictionary<Component, bool>();
			stopwatch = new Stopwatch();
		}

		public void Go() {
			foreach (var component in components) {
				if (IsAutoStartComponent(component)) {
					componentExecutionState[component] = component.Execute();
				}
			}

			stopwatch.Start();
			lastUpdateTime = stopwatch.ElapsedMilliseconds;
			bool moveNext;

			while (componentExecutionState.Count > 0) {
				System.Threading.Thread.Sleep(0);
				componentsThatHaveTerminated.Clear();
				currentlyRunningComponents.Clear();

				var currentTime = stopwatch.ElapsedMilliseconds;
				foreach (var kvp in componentExecutionState) {
					moveNext = false;
					currentlyRunningComponents[kvp.Key] = true;
					var current = kvp.Value.Current;

					if (current is WaitForPacketOn) {
						var waitForPacket = current as WaitForPacketOn;
						var allHavePacket = true;
						foreach (var port in waitForPacket.Ports) {
							allHavePacket = allHavePacket && port.HasPacketsWaiting;
						}

						if (allHavePacket) moveNext = true;
					}
					else if (current is WaitForCapacityOn) {
						var waitForCapacity = current as WaitForCapacityOn;
						var allHaveCapacity = true;
						foreach (var port in waitForCapacity.Ports) {
							allHaveCapacity = allHaveCapacity && !port.Connection.Full;
						}

						if (allHaveCapacity) moveNext = true;
					}
					else if (current is WaitForTime) {
						var waitForTime = current as WaitForTime;
						waitForTime.ElapsedTime += (currentTime - lastUpdateTime);
//						Console.WriteLine ("Elapsed wait time for " + kvp.Key.Name + ": " + waitForTime.ElapsedTime);
						if (waitForTime.ElapsedTime >= waitForTime.Milliseconds) moveNext = true;
					}
					else {
						moveNext = true;
					}

					if (moveNext) {
						if (!kvp.Value.MoveNext()) componentsThatHaveTerminated.AddLast(kvp.Key);
					}
				}

				foreach (var component in componentsThatHaveTerminated) {
					currentlyRunningComponents.Remove(component);
					componentExecutionState.Remove(component);
				}

				var nonExecutingComponents = components.FindAll(c => !currentlyRunningComponents.ContainsKey(c));

				foreach (var component in nonExecutingComponents) {
					if (component.HasPacketOnAnyNonIipInputPort()) {
						componentExecutionState[component] = component.Execute();
					}
				}

				lastUpdateTime = currentTime;
			}
		}

		public void SetupPorts() {
			List<IInputPort> inputPortList;
			FieldInfo inputPortListField;

			foreach (var component in components) {
				inputPortList = new List<IInputPort>();
				inputPortListField = component.GetType().GetField("InputPorts", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField | BindingFlags.Public);
				inputPortListField.SetValue(component, inputPortList);

				foreach (var field in component.GetType ().GetFields (BindingFlags.NonPublic | BindingFlags.Instance)) {
					foreach (Attribute attr in field.GetCustomAttributes (true)) {
						if (attr is InputPortAttribute) {
							if (!typeof(IInputPort).IsAssignableFrom(field.FieldType)) {
								throw new InvalidOperationException(
									string.Format("The InputPort attribute on '{0}' is not valid, type must be IInputPort but was '{1}'",
								                  field.Name, field.FieldType));
							}
							var inputPort = attr as InputPortAttribute;
							CreatePort(component, field, inputPort.Name);
							inputPortList.Add(field.GetValue(component) as IInputPort);
						}
						else if (attr is OutputPortAttribute) {
							if (!typeof(IOutputPort).IsAssignableFrom(field.FieldType)) {
								throw new InvalidOperationException(
									string.Format("The OutputPort attribute on '{0}' is not valid, type must be IOutputPort but was '{1}'",
								                  field.Name, field.FieldType));

							}
							var outputPort = attr as OutputPortAttribute;
							CreatePort(component, field, outputPort.Name);
						}
					}
				}
			}
		}

		public void AddComponent(Component component) {
			components.Add(component);
		}

		public void Connect(Component firstComponent, string outPortName, Component secondComponent, string inPortName) {
			var outPort = GetOutPortFromComponentNamed(firstComponent, outPortName);
			var inPort = GetInPortFromComponentNamed(secondComponent, inPortName);

			if (!inPort.HasConnection) {
				var connection = new Connection();
				inPort.Connection = connection;
				connection.SetReceiver(inPort);
			} 
			outPort.Connection = inPort.Connection;
		}

		public void SetInitialData(Component component, string portName, object value) {
			var ip = new InformationPacket(value);
			var port = GetInPortFromComponentNamed(component, portName);

			if (!port.HasConnection) {
				var connection = new Connection();
				port.Connection = connection;
				connection.SetReceiver(port);
			} 
			port.Connection.SetInitialData(ip);
		}

		bool IsAutoStartComponent(Component component) {
			var autoStart = true;

			foreach (var field in component.GetType ().GetFields (BindingFlags.NonPublic | BindingFlags.Instance)) {
				foreach (Attribute attr in field.GetCustomAttributes (true)) {
					if (attr is InputPortAttribute) {
						autoStart = autoStart && (field.GetValue(component) as IInputPort).Connection.IsInitialInformationPacket;
					}
				}
			}

			return autoStart;
		}

		void CreatePort(Component component, FieldInfo field, string name) {
			var createMethod = GetType().GetMethod("InstantiatePortForField", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(field.FieldType);
			createMethod.Invoke(this, new object[] { component, field, name });
		}

		T InstantiatePortForField<T>(Component component, FieldInfo field, string name) where T : IPort {
			var port = Activator.CreateInstance<T>();
			port.Name = name;
			port.Component = component;
			field.SetValue(component, port);
			return port;
		}

		IOutputPort GetOutPortFromComponentNamed(Component component, string name) {
			try {
				var matchingField = component.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).First(field => {
					return field.GetCustomAttributes(true).FirstOrDefault(attr => {
						return (attr is OutputPortAttribute) && (attr as OutputPortAttribute).Name == name;
					}) != null;
				});

				return matchingField.GetValue(component) as IOutputPort;
			}
			catch (InvalidOperationException) {
				throw new InvalidOperationException(string.Format("Component '{0}' of type '{1}' does not contain an output port named '{2}'", component.Name, component.GetType(), name));
			}
		}

		IInputPort GetInPortFromComponentNamed(Component component, string name) {
			try {
				var matchingField = component.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).First(field => {
					return field.GetCustomAttributes(true).FirstOrDefault(attr => {
						return (attr is InputPortAttribute) && (attr as InputPortAttribute).Name == name;
					}) != null;
				});

				return matchingField.GetValue(component) as IInputPort;
			}
			catch (InvalidOperationException) {
				throw new InvalidOperationException(string.Format("Component '{0}' of type '{1}' does not contain an input port named '{2}'", component.Name, component.GetType(), name));
			}
		}
	}
}