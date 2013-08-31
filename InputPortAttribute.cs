using System;

namespace NTransit {
	[AttributeUsage(AttributeTargets.Property)]
	public class InputPortAttribute : PortAttribute {

		public InputPortAttribute(string name) : base(name) {}
	}
}