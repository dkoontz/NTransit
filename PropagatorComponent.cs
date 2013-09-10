using System;

namespace NTransit {
	[InputPort("In")]
	[OutputPort("Out")]
	public abstract class PropagatorComponent : Component {
		protected PropagatorComponent(string name) : base(name) {}

		public override void Init() {
			SequenceStart["In"] = id => SendSequenceStart("Out", id);
			SequenceEnd["In"] = id => SendSequenceEnd("Out", id);
		}
	}
}