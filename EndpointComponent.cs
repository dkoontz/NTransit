using System;

namespace NTransit {
	[InputPort("In")]
	public abstract class EndpointComponent : Component {
		protected EndpointComponent(string name) : base(name) {}
	}
}