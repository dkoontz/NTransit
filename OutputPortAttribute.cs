using System;

namespace NTransit {
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class OutputPortAttribute : PortAttribute {
		public Type Type { get; set; }

		public OutputPortAttribute(string name) : base(name) {}
	}
}