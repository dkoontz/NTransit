using System;
using System.Collections;
using System.Collections.Generic;

namespace NTransit {
	[InputPort("IEnumerable")]
	public class ConvertIEnumerableToInformationPacketStream : SourceComponent {
		public ConvertIEnumerableToInformationPacketStream(string name) : base(name) {}

		IEnumerator iterator;

		public override void Init() {
			Receive["IEnumerable"] = data => {
				iterator = data.Accept().ContentAs<IEnumerable>().GetEnumerator();
				HasCompleted = !iterator.MoveNext();
			};

			Update = () => {
				while (HasCapacity("Out") && !HasCompleted) {
					SendNew("Out", iterator.Current);
					HasCompleted = !iterator.MoveNext();
				}
			};
		}
	}
}