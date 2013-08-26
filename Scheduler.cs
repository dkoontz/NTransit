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
			
			var reader = new FileReader ("Get File Contents");
			scheduler.AddComponent (reader);

			var writer = new FileWriter ("Write File Contents");
			scheduler.AddComponent (writer);

			var consoleWriter = new ConsoleWriter ("Console Output");
			scheduler.AddComponent (consoleWriter);

			scheduler.SetupPorts ();

			scheduler.Connect (reader, "File Contents", writer, "Text To Write");
			scheduler.Connect (reader, "Errors", consoleWriter, "In");
			scheduler.Connect (writer, "Errors", consoleWriter, "In");

			scheduler.SetInitialData (reader, "File Name", "test1.txt");
			scheduler.SetInitialData (writer, "File Name", "test2.txt");
			scheduler.Go ();
		}

		List<Component> components;
//		List<Connection> connections;
//		LinkedList<IEnumerator> coroutines;
		Dictionary<Component, IEnumerator> componentExecutionState;
		LinkedList<Component> componentsThatHaveTerminated;
		Queue<Component> componentsToAddToExecutionList;

		public Scheduler ()
		{
			components = new List<Component> ();
//			connections = new List<Connection> ();
//			coroutines = new LinkedList<IEnumerator<object>> ();
			componentExecutionState = new Dictionary<Component, IEnumerator> ();
			componentsThatHaveTerminated = new LinkedList<Component> ();
			componentsToAddToExecutionList = new Queue<Component> ();
		}

		public void Go ()
		{
			foreach (var component in components) 
			{
				Console.WriteLine ("component is null?: " + (component == null));
				if (IsAutoStartComponent (component))
				{
					componentExecutionState[component] = component.Execute ();
				}
			}

			while (true) 
			{
				System.Threading.Thread.Sleep (0);
				componentsThatHaveTerminated.Clear ();

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
							if(!kvp.Value.MoveNext ()) componentsThatHaveTerminated.AddLast (kvp.Key);
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
							if(!kvp.Value.MoveNext ()) componentsThatHaveTerminated.AddLast (kvp.Key);
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
						if(!kvp.Value.MoveNext ()) componentsThatHaveTerminated.AddLast (kvp.Key);
					}
				}

				foreach (var component in componentsThatHaveTerminated) 
				{
					Console.WriteLine ("Removing terminated component: " + component.Name + " of type: " + component.GetType());
					componentExecutionState.Remove (component);
				}

				while(componentsToAddToExecutionList.Count > 0)
				{
					var component = componentsToAddToExecutionList.Dequeue ();
					componentExecutionState[component] = component.Execute ();
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
				connection.NotifyWhenPacketReceived += PacketReceivedCallback;
			} 
			outPort.Connection = inPort.Connection;
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
				connection.NotifyWhenPacketReceived += PacketReceivedCallback;
			} 
			port.Connection.SetInitialData (ip);
		}

		void CreatePort (Component component, FieldInfo field, string name)
		{
			var createMethod = GetType ().GetMethod ("InstantiatePortForField", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod (field.FieldType);
			createMethod.Invoke (this, new object[] { component, field, name });
		}

		bool IsAutoStartComponent (Component component) 
		{
			var autoStart = true;

			foreach (var field in component.GetType ().GetFields (BindingFlags.NonPublic | BindingFlags.Instance)) 
			{
				foreach (Attribute attr in field.GetCustomAttributes (true)) 
				{
					if (attr is InputPortAttribute) 
					{
						autoStart = autoStart && (field.GetValue(component) as IInputPort).Connection.IsInitialInformationPacket;
					}
				}
			}

			return autoStart;
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

		void PacketReceivedCallback(Component component) {
			if(!componentExecutionState.ContainsKey(component)) {
				componentsToAddToExecutionList.Enqueue (component);
			}
		}
	}
}