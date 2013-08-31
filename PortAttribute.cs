using System;

namespace NTransit {
	[AttributeUsage(AttributeTargets.Property)]
	public abstract class PortAttribute : Attribute {
		public string Name { get; private set; }

		public PortAttribute(string name) {
			Name = name;
		}
	}
}