using System;

namespace NTransit {
	[AttributeUsage(AttributeTargets.Class)]
	public class InputPortAttribute : PortAttribute {

		public InputPortAttribute(string name) : base(name) {}
	}
}