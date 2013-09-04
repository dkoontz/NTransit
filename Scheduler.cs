using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

// Big TODO's:
//
// FBP / Json importer
// Auto ports
// Support thread pool and individual threads
// Sub-Networks
// Array Inputs
// Array Outputs
// Auto wire unconnected output ports to Drop component
// Auto wire unconnected Errors component port to default ConsoleWriter or settable property
// Closeable ports that cannot be re-opened due to incoming IPs
// Figure out IP ownership and tracking/disposing
// Allowing components to execute during Fixed/Late Update
// Type checking of IP content at Port/Connection level based on parameter(s) to InputPort / OutputPort attributes

namespace NTransit {
	public interface IProcessRunner {
		IEnumerator Start(Component process);
		bool Tick(IEnumerator iterator);
	}

	public class SingleThreadProcessRunner : IProcessRunner {
		public IEnumerator Start(Component process) {
			return process.Execute();
		}

		public bool Tick(IEnumerator iterator) {
			return iterator.MoveNext();
		}
	}

	public class Scheduler {
		public bool HasProcessesThatAreCurrentlyRunning { get; private set; }

		IProcessRunner processRunner;
		List<Component> processes;
		Dictionary<Component, IEnumerator> processExecutionStates;
		LinkedList<Component> processesThatHaveTerminated;
		LinkedList<Component> processesPendingAutoOutSend;
		Dictionary<Component, bool> currentlyRunningProcesses;

		bool shuttingDown;

		public Scheduler() : this(new SingleThreadProcessRunner()) {}

		public Scheduler(IProcessRunner processRunner) {
			this.processRunner = processRunner;

			processes = new List<Component>();
			processExecutionStates = new Dictionary<Component, IEnumerator>();
			processesThatHaveTerminated = new LinkedList<Component>();
			processesPendingAutoOutSend = new LinkedList<Component>();
			currentlyRunningProcesses = new Dictionary<Component, bool>();
		}

		public void AutoRun() {
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			long lastUpdateTime = stopwatch.ElapsedMilliseconds;

			Init();


			while (true) {
				var currentTime = stopwatch.ElapsedMilliseconds;
				if(!Tick(currentTime - lastUpdateTime)) {
					break;
				}
				lastUpdateTime = currentTime;
			}
		}

		public void Init() {
			foreach (var process in processes) {
				UnityEngine.Debug.Log("Auto start process: '" + process.Name + "'? " + IsAutoStartProcess(process));
				if (IsAutoStartProcess(process)) {
					processExecutionStates[process] = processRunner.Start(process);
				}
			}
		}

		public bool Tick(long elapsedTime) {
			HasProcessesThatAreCurrentlyRunning = false;
			bool moveNext;

			if (shuttingDown) {
				foreach (var kvp in processExecutionStates) {
					kvp.Key.Close();
				}
				return false;
			}

			if (processExecutionStates.Count > 0) {
				processesThatHaveTerminated.Clear();
				currentlyRunningProcesses.Clear();

//				UnityEngine.Debug.Log("Running scheduler processes: " + processExecutionStates.Count);
				foreach (var kvp in processExecutionStates) {
//					UnityEngine.Debug.Log("  - " + kvp.Key.Name);
					moveNext = false;
					currentlyRunningProcesses[kvp.Key] = true;
					var current = kvp.Value.Current;

					if (current is WaitForPacketOn) {
						var waitForPacket = current as WaitForPacketOn;
						var allHavePacket = true;
						foreach (var port in waitForPacket.Ports) {
							allHavePacket = allHavePacket && port.HasPacketsWaiting;
						}

						if (allHavePacket) moveNext = true;
						if (allHavePacket) UnityEngine.Debug.Log("Process: " + kvp.Key.Name + " has data on incoming port");
					}
					else if (current is WaitForCapacityOn) {
						var waitForCapacity = current as WaitForCapacityOn;
						var allHaveCapacity = true;
						foreach (var port in waitForCapacity.Ports) {
							allHaveCapacity = allHaveCapacity && !port.Connection.Full;
						}

						if (allHaveCapacity) moveNext = true;
						if (allHaveCapacity) UnityEngine.Debug.Log("Process: " + kvp.Key.Name + " has capacity on outgoing port");
					}
					else if (current is WaitForPacketOrCapacityOnAny) {
						var waitOnAny = current as WaitForPacketOrCapacityOnAny;
						UnityEngine.Debug.Log("Checking: '" + kvp.Key.Name + "' for packet or capacity, inports: " + waitOnAny.InputPorts.Count() + " outports: " + waitOnAny.OutputPorts.Count());
						var anyAreReady = false;
						foreach (var port in waitOnAny.InputPorts) {
							UnityEngine.Debug.Log("port: " + port.Name + " packet waiting? " + port.HasPacketsWaiting);
							anyAreReady = anyAreReady || port.HasPacketsWaiting;
						}

						foreach (var port in waitOnAny.OutputPorts) {
							UnityEngine.Debug.Log("port: " + port.Name + " capacity? " + !port.Connection.Full);
							anyAreReady = anyAreReady || !port.Connection.Full;
						}

						if (anyAreReady) moveNext = anyAreReady;
						if (anyAreReady) UnityEngine.Debug.Log("Process: " + kvp.Key.Name + " has packets on incoming port or capacity on outgoing port");

					} 
					else if (current is WaitForTime) {
						var waitForTime = current as WaitForTime;
						waitForTime.ElapsedTime += elapsedTime;

						if (waitForTime.ElapsedTime >= waitForTime.Milliseconds) moveNext = true;
						if (waitForTime.ElapsedTime >= waitForTime.Milliseconds) UnityEngine.Debug.Log("Process: " + kvp.Key.Name + " has reached wait time");
					}
					else {
						moveNext = true;
					}

					if (moveNext) {
						HasProcessesThatAreCurrentlyRunning = true;
						if (!processRunner.Tick(kvp.Value))	processesThatHaveTerminated.AddLast(kvp.Key);
					}
				}

				foreach (var process in processesThatHaveTerminated) {
					UnityEngine.Debug.Log("Process: " + process.Name + " has terminated");
					if (process.AutoOutPort.HasConnection) processesPendingAutoOutSend.AddLast(process);
					currentlyRunningProcesses.Remove(process);
					processExecutionStates.Remove(process);
					process.ResetInitialDataAvailability();
				}

				var processThatHaveCompletedAutoSend = new Queue<Component>();
				foreach (var process in processesPendingAutoOutSend) {
					UnityEngine.Debug.Log("process: " + process.Name + " sending auto out");
					if (process.AutoOutPort.TrySend(InformationPacket.AutoPacket)) {
						UnityEngine.Debug.Log("auto out sent");
						processThatHaveCompletedAutoSend.Enqueue(process);
					}
				}
				while (processThatHaveCompletedAutoSend.Count > 0) {
					processesPendingAutoOutSend.Remove(processThatHaveCompletedAutoSend.Dequeue());
				}

//				UnityEngine.Debug.Log(" ======= PROCESS LIST ========");
//				foreach (var p in processes) {
//					UnityEngine.Debug.Log(p.Name);
//				}
//				UnityEngine.Debug.Log(" ======= PROCESS LIST END ========");

				var nonExecutingProcesses = processes.FindAll(c => !currentlyRunningProcesses.ContainsKey(c));

				foreach (var process in nonExecutingProcesses) {
					UnityEngine.Debug.Log(process.Name + " has waiting packets? " + (process.HasPacketOnAnyNonIipInputPort()));
					if (process.HasPacketOnAnyNonIipInputPort()) {
						UnityEngine.Debug.Log(process.Name + " has incoming packets and was woken up");
						processExecutionStates[process] = processRunner.Start(process);
					}
				}

				return true;
			}
			else return false;
		}

		public void Shutdown() {
			shuttingDown = true;
		}

		void SetupPorts(Component process) {
			var inputPortList = new List<IInputPort>();
			var outputPortList = new List<IOutputPort>();

//			var inputPortListProperty = process.GetType().GetProperty("InputPorts", BindingFlags.Public | BindingFlags.Instance);
//			inputPortListProperty.SetValue(process, inputPortList, null);

			foreach (var property in process.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				foreach (Attribute attr in property.GetCustomAttributes(true)) {
					if (attr is InputPortAttribute) {
						if (!typeof(IInputPort).IsAssignableFrom(property.PropertyType)) {
							throw new InvalidOperationException(
								string.Format("The InputPort attribute on field '{0}' of component '{1}' is not valid, type must be IInputPort but was '{2}'",
								              property.Name, 
							                  process.Name, 
							              	  property.PropertyType));
						}
						var inputPort = attr as InputPortAttribute;
						process.SetInputPort(inputPort.Name, CreatePort(process, property, inputPort.Name) as IInputPort);
						inputPortList.Add(property.GetValue(process, null) as IInputPort);
					}
					else if (attr is OutputPortAttribute) {
						if (!typeof(IOutputPort).IsAssignableFrom(property.PropertyType)) {
							throw new InvalidOperationException(
								string.Format("The OutputPort attribute on field '{0}' of component '{1}' is not valid, type must be IOutputPort but was '{1}'",
								              property.Name, 
							                  process.Name,
							              	  property.PropertyType));

						}
						var outputPort = attr as OutputPortAttribute;
						process.SetOutputPort(outputPort.Name, CreatePort(process, property, outputPort.Name) as IOutputPort);
					}
				}
			}
			process.InputPorts = inputPortList.ToArray();
			process.OutputPorts = outputPortList.ToArray();
			process.ConnectedInputPorts = inputPortList.Where(p => p.HasConnection).ToArray();
			process.ConnectedOutputPorts = outputPortList.Where(p => p.HasConnection).ToArray();
		}

		public void AddProcess(Component process) {
			processes.Add(process);
			SetupPorts(process);
		}

		public void Connect(Component firstProcess, string outPortName, Component secondProcess, string inPortName) {
			var outPort = GetOutPortFromProcessNamed(firstProcess, outPortName);
			var inPort = GetInPortFromProcessNamed(secondProcess, inPortName);

			if (!inPort.HasConnection) {
				var connection = new Connection();
				inPort.Connection = connection;
				connection.SetReceiver(inPort);
			} 
			outPort.Connection = inPort.Connection;
		}

		public void SetInitialData(Component process, string portName, object value) {
			var ip = new InformationPacket(value);
			var port = GetInPortFromProcessNamed(process, portName);

			if (!port.HasConnection) {
				var connection = new Connection();
				port.Connection = connection;
				connection.SetReceiver(port);
			} 
			port.Connection.SetInitialData(ip);
		}

		bool IsAutoStartProcess(Component process) {
			var autoStart = true;
			IInputPort inputPort;

//			UnityEngine.Debug.Log("Checking '" + process.Name + "' for auto start");
			foreach (var property in process.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
//				UnityEngine.Debug.Log("- checking " + property.Name);
				foreach (Attribute attr in property.GetCustomAttributes(true)) {
					if (attr is InputPortAttribute) {
						inputPort = (property.GetValue(process, null) as IInputPort);
						autoStart = autoStart && inputPort.HasConnection && inputPort.Connection.HasInitialInformationPacket;
						UnityEngine.Debug.Log("checking port: '" + inputPort.Name + "connection: " + inputPort.HasConnection);
						if (inputPort.HasConnection) UnityEngine.Debug.Log("  has initial data: " + inputPort.Connection.HasInitialInformationPacket);
					}
				}
			}

			return autoStart;
		}

		IPort CreatePort(Component process, PropertyInfo property, string name) {
			var createMethod = GetType().GetMethod("InstantiatePortForField", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(property.PropertyType);
			var port = createMethod.Invoke(this, new object[] { process, name }) as IPort;

			port.Name = name;
			port.Process = process;
			return port;
		}

		T InstantiatePortForField<T>(Component process, string name) where T : IPort {
			var port = Activator.CreateInstance<T>();
			return port;
		}

		IOutputPort GetOutPortFromProcessNamed(Component process, string name) {
//			if ("*" == name) return process.AutoOutPort;

			try {
				var matchingProperty = process.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).First(property => {
					return property.GetCustomAttributes(true).FirstOrDefault(attr => {
						return (attr is OutputPortAttribute) && (attr as OutputPortAttribute).Name == name;
					}) != null;
				});

				return matchingProperty.GetValue(process, null) as IOutputPort;
			}
			catch (InvalidOperationException) {
				throw new InvalidOperationException(string.Format("Component '{0}' of type '{1}' does not contain an output port named '{2}'", process.Name, process.GetType(), name));
			}
		}

		IInputPort GetInPortFromProcessNamed(Component process, string name) {
//			if ("*" == name) return process.AutoInPort;

			try {
				var matchingProperty = process.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).First(field => {
					return field.GetCustomAttributes(true).FirstOrDefault(attr => {
						return (attr is InputPortAttribute) && (attr as InputPortAttribute).Name == name;
					}) != null;
				});

				return matchingProperty.GetValue(process, null) as IInputPort;
			}
			catch (InvalidOperationException) {
				throw new InvalidOperationException(string.Format("Component '{0}' of type '{1}' does not contain an input port named '{2}'", process.Name, process.GetType(), name));
			}
		}
	}
}