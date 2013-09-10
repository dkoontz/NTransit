using System;

namespace NTransit {
	[AttributeUsage(AttributeTargets.Class)]
	public abstract class PortAttribute : Attribute {
		public string Name { get; private set; }

		public PortAttribute(string name) {
			Name = name;
		}
	}
}