using System;
using System.Collections;
using System.Collections.Generic;

namespace Transit
{
	public class WaitForPacketOn
	{
		public IInputPort[] Ports { get; private set; }

		public WaitForPacketOn (IInputPort[] ports)
		{
			Ports = ports;
		}
	}

	public class WaitForCapacityOn
	{
		public IOutputPort[] Ports { get; private set; }

		public WaitForCapacityOn (IOutputPort[] ports)
		{
			Ports = ports;
		}
	}

	public class WaitForTime
	{
		public int Milliseconds { get; private set; }
		public int ElapsedTime { get; set; }

		public WaitForTime(int milliseconds)
		{
			Milliseconds = milliseconds;
		}
	}

	public abstract class Component
	{
		public string Name { get; set; }

		[OutputPort("Errors")]
		protected StandardOutputPort Errors;

		protected Component ()
		{
			ownedIps = new LinkedList<InformationPacket> ();
		}

		LinkedList<InformationPacket> ownedIps;

		public abstract IEnumerator Execute ();

		public void ClaimIp (InformationPacket ip)
		{
			ip.Owner = this;
			ownedIps.AddLast (ip);
		}

		public void ReleaseIp (InformationPacket ip)
		{
			ip.Owner = null;
			ownedIps.Remove (ip);
		}

		protected WaitForPacketOn WaitForPacketOn (params IInputPort[] ports)
		{
			return new WaitForPacketOn (ports);
		}

		protected WaitForCapacityOn WaitForCapacityOn (params IOutputPort[] ports)
		{
			return new WaitForCapacityOn (ports);
		}
	}
}