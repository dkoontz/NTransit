using System;
using System.Collections;
using System.Collections.Generic;

namespace NTransit {
	[InputPort("IEnumerable")]
	public class ConvertIEnumerableToInformationPacketStream : SourceComponent {
		IEnumerator iterator;

		public ConvertIEnumerableToInformationPacketStream(string name) : base(name) { }

		public override void Setup() {
			InPorts["IEnumerable"].Receive = data => {
				iterator = data.Accept().ContentAs<IEnumerable>().GetEnumerator();
				SendSequenceStart("Out");
				if (!iterator.MoveNext()) {
					Status = ProcessStatus.Terminated;
				}
			};
		}

		protected override bool Update() {
			base.Update();

			while (HasCapacity("Out") && Status == ProcessStatus.Active) {
				SendNew("Out", iterator.Current);
				if (!iterator.MoveNext()) {
					Status = ProcessStatus.Terminated;
				}
			}
			return false;
		}

		protected override void End() {
			SendSequenceEnd("Out");
		}			
	}
}