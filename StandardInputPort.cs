using System;
using System.Collections;
using System.Collections.Generic;

namespace NTransit {
	public class StandardInputPort : IInputPort {
		public string Name { get; set; }
		public Component Process { get; set; }
		public bool Greedy { get; set; }
		public int ConnectionCapacity { get; set; }
		public bool Connected { get { return upstreamPorts.Count > 0; } }
		public bool AllUpstreamPortsClosed {
			get {
				var allClosed = upstreamPorts.Count > 0;
				foreach (var port in upstreamPorts) {
					if (port.Connected && !port.Closed) {
						allClosed = false;
					}
				}
				return allClosed;
			}
		}
		public bool HasPacketWaiting { get { return queue.Count > 0 || (HasInitialData && !initialIpSent); } }
		public bool HasCapacity { get { return queue.Count < ConnectionCapacity; } }
		public bool HasInitialData { get { return initialIp != null; } }
		public int QueuedPacketCount { get { return queue.Count; } }
		public bool Closed { get; private set; }

		public Action<IpOffer> Receive { get; set; }
		public Action<IpOffer> SequenceStart { get; set; }
		public Action<IpOffer> SequenceEnd { get; set; }

		object lockObject = new object();
		Queue<InformationPacket> queue;
		IpOffer ipOffer;
		InformationPacket initialIp;
		bool initialIpSent;
		List<IOutputPort> upstreamPorts;

		public StandardInputPort(int connectionCapacity, Component process) {
			this.ConnectionCapacity = connectionCapacity;
			queue = new Queue<InformationPacket>(connectionCapacity);
			upstreamPorts = new List<IOutputPort>();
			Closed = false;
			Process = process;
			SequenceStart = data => data.Accept();
			SequenceEnd = data => data.Accept();
		}

		public void SetInitialData(InformationPacket ip) {
			initialIp = ip;
		}

		public bool TrySend(InformationPacket ip) {
			if (Closed)	{
				throw new InvalidOperationException(string.Format("Cannot send data to a closed port '{0}.{1}'", Process.Name, Name));
			}
			lock (lockObject) {
				if (queue.Count < ConnectionCapacity) {
					// check packet contents for type if a type restriction exists
					if (ValidatePacketContentType(ip)) {
						queue.Enqueue(ip);
						return true;
					}
					throw new ArgumentException(string.Format("IP content of type {0} received on {1}.{2} is not valid due to the type restriction for Input port's type restriction, valid types are {3}", ip.Content.GetType(), Process.Name, Name, ValidTypeDescription));
				}
				
				return false;
			}
		}

		public bool Tick() {
			if (Closed) {
				return false;
			}

			var packetWasSent = false;
			do {
				if (ipOffer != null) {
					DispatchOffer(ipOffer);
				}
				else if (!initialIpSent && initialIp != null) {
					ipOffer = new IpOffer(initialIp);
					DispatchOffer(ipOffer);
				}
				else if (queue.Count > 0) {
					ipOffer = new IpOffer(queue.Peek());
					DispatchOffer(ipOffer);
				}

				if (ipOffer != null && ipOffer.Accepted) {
					if (!initialIpSent && initialIp != null) {
						initialIpSent = true;
					}
					else {
						lock (lockObject) {
							queue.Dequeue();
						}
					}
					
					ipOffer = null;
					packetWasSent = true;
				}
			}
			while (Greedy && queue.Count > 0 && ipOffer == null);

			return packetWasSent;
		}

		public void NotifyOfConnection(IOutputPort port) {
			upstreamPorts.Add(port);
		}

		public void Close() {
			Closed = true;
		}

		protected virtual string ValidTypeDescription { 
			get { return ""; }
		}

		protected virtual bool ValidatePacketContentType(InformationPacket ip) {
			return true;
		}

		void DispatchOffer(IpOffer offer) {
			switch (offer.Type) {
				case InformationPacket.PacketType.Data:
					if (Receive != null) {
						Receive(offer);
					}
					break;
				case InformationPacket.PacketType.StartSequence:
					if (SequenceStart != null) {
						SequenceStart(offer);
					}
					break;
				case InformationPacket.PacketType.EndSequence:
					if (SequenceEnd != null) {
						SequenceEnd(offer);
					}
					break;
				case InformationPacket.PacketType.Auto:
					if (Receive != null) {
						Receive(offer);
					}
					break;
			}
		}
	}

	public class StandardInputPort<T1> : StandardInputPort {
		public StandardInputPort(int connectionCapacity, Component process) : base(connectionCapacity, process){ }

		protected override bool ValidatePacketContentType(InformationPacket ip) {
			return typeof(T1).IsAssignableFrom(ip.Content.GetType());
		}

		protected override string ValidTypeDescription { 
			get { return typeof(T1).ToString(); }
		}
	}

	public class StandardInputPort<T1, T2> : StandardInputPort {
		public StandardInputPort(int connectionCapacity, Component process) : base(connectionCapacity, process){ }

		protected override bool ValidatePacketContentType(InformationPacket ip) {
			return 	typeof(T1).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T2).IsAssignableFrom(ip.Content.GetType());
		}

		protected override string ValidTypeDescription { 
			get { return string.Format("{0}, {1}", typeof(T1), typeof(T2)); }
		}
	}

	public class StandardInputPort<T1, T2, T3> : StandardInputPort {
		public StandardInputPort(int connectionCapacity, Component process) : base(connectionCapacity, process){ }

		protected override bool ValidatePacketContentType(InformationPacket ip) {
			return 	typeof(T1).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T2).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T3).IsAssignableFrom(ip.Content.GetType());
		}

		protected override string ValidTypeDescription { 
			get { return string.Format("{0}, {1}, {2}", typeof(T1), typeof(T2), typeof(T3)); }
		}
	}

	public class StandardInputPort<T1, T2, T3, T4> : StandardInputPort {
		public StandardInputPort(int connectionCapacity, Component process) : base(connectionCapacity, process){ }

		protected override bool ValidatePacketContentType(InformationPacket ip) {
			return 	typeof(T1).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T2).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T3).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T4).IsAssignableFrom(ip.Content.GetType());
		}

		protected override string ValidTypeDescription { 
			get { return string.Format("{0}, {1}, {2}, {3}", typeof(T1), typeof(T2), typeof(T3), typeof(T4)); }
		}
	}

	public class StandardInputPort<T1, T2, T3, T4, T5> : StandardInputPort {
		public StandardInputPort(int connectionCapacity, Component process) : base(connectionCapacity, process){ }

		protected override bool ValidatePacketContentType(InformationPacket ip) {
			return 	typeof(T1).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T2).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T3).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T4).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T5).IsAssignableFrom(ip.Content.GetType());
		}

		protected override string ValidTypeDescription { 
			get { return string.Format("{0}, {1}, {2}, {3}, {4}", typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)); }
		}
	}

	public class StandardInputPort<T1, T2, T3, T4, T5, T6> : StandardInputPort {
		public StandardInputPort(int connectionCapacity, Component process) : base(connectionCapacity, process){ }

		protected override bool ValidatePacketContentType(InformationPacket ip) {
			return 	typeof(T1).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T2).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T3).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T4).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T5).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T6).IsAssignableFrom(ip.Content.GetType());
		}

		protected override string ValidTypeDescription { 
			get { return string.Format("{0}, {1}, {2}, {3}, {4}, {5}", typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6)); }
		}
	}

	public class StandardInputPort<T1, T2, T3, T4, T5, T6, T7> : StandardInputPort {
		public StandardInputPort(int connectionCapacity, Component process) : base(connectionCapacity, process){ }

		protected override bool ValidatePacketContentType(InformationPacket ip) {
			return 	typeof(T1).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T2).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T3).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T4).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T5).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T6).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T7).IsAssignableFrom(ip.Content.GetType());
		}

		protected override string ValidTypeDescription { 
			get { return string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}", typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7)); }
		}
	}

	public class StandardInputPort<T1, T2, T3, T4, T5, T6, T7, T8> : StandardInputPort {
		public StandardInputPort(int connectionCapacity, Component process) : base(connectionCapacity, process){ }

		protected override bool ValidatePacketContentType(InformationPacket ip) {
			return 	typeof(T1).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T2).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T3).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T4).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T5).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T6).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T7).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T8).IsAssignableFrom(ip.Content.GetType());
		}

		protected override string ValidTypeDescription { 
			get { return string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}", typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8)); }
		}
	}

	public class StandardInputPort<T1, T2, T3, T4, T5, T6, T7, T8, T9> : StandardInputPort {
		public StandardInputPort(int connectionCapacity, Component process) : base(connectionCapacity, process){ }

		protected override bool ValidatePacketContentType(InformationPacket ip) {
			return 	typeof(T1).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T2).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T3).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T4).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T5).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T6).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T7).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T8).IsAssignableFrom(ip.Content.GetType()) ||
					typeof(T9).IsAssignableFrom(ip.Content.GetType());
		}

		protected override string ValidTypeDescription { 
			get { return string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}", typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9)); }
		}
	}
}