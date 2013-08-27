using System;
using nTransit;
using System.Collections;

namespace nTransit {
	public class MockComponent : Component {
		public MockComponent() : base ("Mock Component") {}

		public override IEnumerator Execute() {
			yield break;
		}
	}
}