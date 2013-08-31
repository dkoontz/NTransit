using System;

namespace NTransit {
	[AttributeUsage(AttributeTargets.Property)]
	public class OutputPortAttribute : PortAttribute {

		public OutputPortAttribute(string name) : base(name) {}
	}
}