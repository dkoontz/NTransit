using System;

namespace NTransit {
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class ArrayOutputPortAttribute : PortAttribute {
		public Type Type { get; set; }
		public ArrayOutputPortAttribute(string name) : base(name) {}
	}
}