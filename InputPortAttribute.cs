using System;

namespace nTransit {
	[AttributeUsage(AttributeTargets.Field)]
	public class InputPortAttribute : Attribute {
		public string Name { get; private set; }

		public InputPortAttribute(string name) {
			Name = name;
		}
	}
}