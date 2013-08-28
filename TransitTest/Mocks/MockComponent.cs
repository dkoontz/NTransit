using System;
using NTransit;
using System.Collections;

namespace NTransit {
	public class MockComponent : Component {
		public MockComponent() : base ("Mock Component") {}

		public override IEnumerator Execute() {
			yield break;
		}
	}
}