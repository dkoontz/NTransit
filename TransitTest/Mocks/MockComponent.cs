using System;
using NTransit;
using System.Collections;

namespace NTransit {
	public class MockComponent : Component {
		public MockComponent() : base ("Mock Component") {}
		[InputPort("In")]
		public StandardInputPort InPort { get; set; }

		[OutputPort("Out")]
		public StandardOutputPort OutPort { get; set; }

		public override IEnumerator Execute() {
			yield break;
		}

		public bool Send(object content) {
			return OutPort.TrySend(content);
		}

		public object Receive() {
			return InPort.Receive();
		}
	}
}