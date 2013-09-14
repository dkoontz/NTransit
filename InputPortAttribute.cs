using System;

namespace NTransit {
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class InputPortAttribute : PortAttribute {
		public int Capacity { get; private set; }

		public InputPortAttribute(string name) : base(name) {}
	}
}