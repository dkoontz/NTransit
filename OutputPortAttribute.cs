using System;

namespace nTransit {
	[AttributeUsage(AttributeTargets.Field)]
	public class OutputPortAttribute : Attribute {
		public string Name { get; private set; }

		public OutputPortAttribute(string name) {
			Name = name;
		}
	}
}