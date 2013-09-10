using System;

namespace NTransit {
	[AttributeUsage(AttributeTargets.Class)]
	public class OutputPortAttribute : PortAttribute {

		public OutputPortAttribute(string name) : base(name) {}
	}
}