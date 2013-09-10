using System;

namespace NTransit {
	[OutputPort("Out")]
	public abstract class SourceComponent : Component {
		protected SourceComponent(string name) : base(name) {}
	}
}