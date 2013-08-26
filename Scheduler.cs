using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Transit
{
	public class Scheduler
	{
		public static void Main ()
		{
			var scheduler = new Scheduler ();
			
			var reader = new FileReader ();
			reader.Name = "Get File Contents";
			scheduler.AddComponent (reader);

			var writer = new FileWriter ();
			writer.Name = "Write File Contents";
			scheduler.AddComponent (writer);

			var consoleWriter = new ConsoleWriter ();
			consoleWriter.Name = "Console Output";
			scheduler.AddComponent (consoleWriter);

			scheduler.SetupPorts ();

			scheduler.Connect (reader, "File Contents", writer, "Text To Write");
			scheduler.Connect (reader, "Errors", consoleWriter, "In");
			scheduler.Connect (writer, "Errors", consoleWriter, "In");

			scheduler.SetInitialData (reader, "File Name", "test.txt");
			scheduler.SetInitialData (writer, "File Name", "test2.txt");
			scheduler.Go ();
		}

		List<Component> components;
		List<Connection> connections;
//		LinkedList<IEnumerator> coroutines;
		Dictionary<Component, IEnumerator> componentExecutionState;

		public Scheduler ()
		{
			components = new List<Component> ();
			connections = new List<Connection> ();
//			coroutines = new LinkedList<IEnumerator<object>> ();
			componentExecutionState = new Dictionary<Component, IEnumerator> ();
		}

		public void Go ()
		{
			foreach (var component in components) 
			{
				componentExecutionState[component] = component.Execute ();
			}

			while (true) 
			{
				System.Threading.Thread.Sleep (0);
//				Console.WriteLine ("Tick");

				foreach (var kvp in componentExecutionState) 
				{
					var current = kvp.Value.Current;
					if(current is WaitForPacketOn) 
					{
						var waitForPacket = current as WaitForPacketOn;
						var allHavePacket = true;
						foreach (var port in waitForPacket.Ports) 
						{
							allHavePacket = allHavePacket && port.HasPacketsWaiting;
						}

						if (allHavePacket)
						{
							kvp.Value.MoveNext ();
						}
					}
					else if (current is WaitForCapacityOn) 
					{
						var waitForCapacity = current as WaitForCapacityOn;
						var allHaveCapacity = true;
						foreach (var port in waitForCapacity.Ports) 
						{
							allHaveCapacity = allHaveCapacity && !port.Connection.Empty;
						}

						if (allHaveCapacity)
						{
							kvp.Value.MoveNext ();
						}
					}
					else if (current is WaitForTime) 
					{
						// calculate elapsed time since last tick
						// update elapsed time on value
						// if elasped time >= time interval
							// call move next
					}
					else{
						kvp.Value.MoveNext ();
					}
				}
			}
		}

		public void SetupPorts ()
		{
			foreach (var component in components) 
			{
				foreach (var field in component.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)) 
				{
					foreach (Attribute attr in field.GetCustomAttributes(true)) 
					{
						if (attr is InputPortAttribute) 
						{
							var inputPort = attr as InputPortAttribute;
							CreatePort (component, field, inputPort.Name);
						} 
						else if (attr is OutputPortAttribute) 
						{
							var outputPort = attr as OutputPortAttribute;
							CreatePort (component, field, outputPort.Name);
						}
					}
				}
			}
		}

		public void AddComponent (Component component)
		{
			components.Add (component);
		}

		public void Connect (Component firstComponent, string outPortName, Component secondComponent, string inPortName)
		{
			var outPort = GetOutPortFromComponentNamed (firstComponent, outPortName);
			var inPort = GetInPortFromComponentNamed (secondComponent, inPortName);

			Console.WriteLine ("found inPort: " + (inPort != null) + ", type: " + inPort.GetType ());
			if (!inPort.HasConnection) 
			{
				var connection = new Connection ();
				inPort.Connection = connection;
				connection.SetReceiver (inPort);
			} 
			outPort.Connection = inPort.Connection;
			inPort.Connection.AddSender (outPort);
		}

		public void SetInitialData (Component component, string portName, object value)
		{
			var ip = new InformationPacket (value);
			var port = GetInPortFromComponentNamed (component, portName);

			if (!port.HasConnection) 
			{
				var connection = new Connection ();
				port.Connection = connection;
				connection.SetReceiver (port);
			} 
			port.Connection.SetInitialData (ip);
		}

		void CreatePort (Component component, FieldInfo field, string name)
		{
			var createMethod = GetType ().GetMethod ("InstantiatePortForField", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod (field.FieldType);
			createMethod.Invoke (this, new object[] { component, field, name });
		}

		T InstantiatePortForField<T> (Component component, FieldInfo field, string name) where T : IPort
		{
			var port = Activator.CreateInstance<T> ();
			port.Name = name;
			port.Component = component;
			field.SetValue (component, port);
			return port;
		}

		IOutputPort GetOutPortFromComponentNamed (Component component, string name)
		{
			try {
				var matchingField = component.GetType ().GetFields (BindingFlags.NonPublic | BindingFlags.Instance).First (field => {
					return field.GetCustomAttributes (true).FirstOrDefault (attr => {
						return (attr is OutputPortAttribute) && (attr as OutputPortAttribute).Name == name;
					}) != null;
				});

				return matchingField.GetValue (component) as IOutputPort;
			} catch (InvalidOperationException) {
				throw new InvalidOperationException (string.Format ("Component '{0}' of type '{1}' does not contain an output port named '{2}'", component.Name, component.GetType (), name));
			}
		}

		IInputPort GetInPortFromComponentNamed (Component component, string name)
		{
			try {
				var matchingField = component.GetType ().GetFields (BindingFlags.NonPublic | BindingFlags.Instance).First (field => {
					return field.GetCustomAttributes (true).FirstOrDefault (attr => {
						return (attr is InputPortAttribute) && (attr as InputPortAttribute).Name == name;
					}) != null;
				});

				return matchingField.GetValue (component) as IInputPort;
			} catch (InvalidOperationException) {
				throw new InvalidOperationException (string.Format ("Component '{0}' of type '{1}' does not contain an input port named '{2}'", component.Name, component.GetType (), name));
			}
		}
	}
}