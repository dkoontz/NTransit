using System;

namespace NTransit {
	[InputPort("In")]
	[OutputPort("Out")]
	public abstract class PropagatorComponent : Component {
		protected PropagatorComponent(string name) : base(name) {
			SequenceStart["In"] = data => Send("Out", data.Accept());
			SequenceEnd["In"] = data => Send("Out", data.Accept());
		}
	}
}