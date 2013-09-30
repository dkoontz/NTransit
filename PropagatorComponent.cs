using System;

namespace NTransit {
	[InputPort("In")]
	[OutputPort("Out")]
	public abstract class PropagatorComponent : Component {
		protected PropagatorComponent(string name) : base(name) { }

		public override void Setup() {
			InPorts["In"].SequenceStart = data => Send("Out", data.Accept());
			InPorts["In"].SequenceEnd = data => Send("Out", data.Accept());
		}
	}
}