using System;

namespace NTransit {
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class InputPortAttribute : PortAttribute {
		public Type Type { get; set; }
		public int Capacity { get; set; }

		public InputPortAttribute(string name) : base(name) {
			Capacity = 1;
		}
	}
}