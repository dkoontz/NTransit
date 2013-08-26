using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;

namespace Transit
{
	public class Connection
	{
		// TODO: Remove this and have a config object that can be configured at runtime
		static int DEFAULT_CONNECTION_CAPACITY = 1;

		public int Capacity { get; set; }

		public bool Full {
			get { return packets.Count == Capacity; }
		}

		public bool Empty {
			get { 
				if (initialIp != null) {
					return sentInitialData && packets.Count == 0;
				} else
					return packets.Count == 0;
			}
		}

		public Action<Component> NotifyWhenPacketReceived { get; set; }

		public bool IsInitialInformationPacket { get; private set; }

		Queue<InformationPacket> packets;
		IInputPort receiver;
		InformationPacket initialIp;
		bool sentInitialData;
		readonly object lockObject = new object ();

		public Connection () : this(DEFAULT_CONNECTION_CAPACITY)
		{
		}

		public Connection (int capacity)
		{
			Capacity = capacity;
			packets = new Queue<InformationPacket> (Capacity);
		}

		public bool SendPacketIfCapacityAllows (InformationPacket ip)
		{
			if (IsInitialInformationPacket) throw new InvalidOperationException (string.Format ("Cannot send packet from '{0}' to connection that is an IIP", ip.Owner));
			if (null == receiver) throw new InvalidOperationException (string.Format ("Cannot send packet from '{0}' to connection that has no reciever", ip.Owner));
			bool hasCapacity = false;
			lock (lockObject) {
				if (packets.Count < Capacity) {
					hasCapacity = true;
					ip.Owner = this;
					packets.Enqueue (ip);
					if (NotifyWhenPacketReceived != null) NotifyWhenPacketReceived (receiver.Component);
				}
			}
			return hasCapacity;
		}

		public InformationPacket Receieve ()
		{
			InformationPacket ip;
			lock (lockObject) {
				if (!sentInitialData && initialIp != null) {
					sentInitialData = true;
					ip = initialIp;
				} else
					ip = packets.Dequeue ();
			}
			return ip;
		}

		public void SetReceiver (IInputPort receiver)
		{
			this.receiver = receiver;
		}

		public void SetInitialData (InformationPacket ip)
		{
			initialIp = ip;
			IsInitialInformationPacket = true;
		}
	}
}