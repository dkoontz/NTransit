using System;

namespace NTransit {
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class OutputPortAttribute : PortAttribute {
		public OutputPortAttribute(string name) : base(name) {}
	}
}