using System;
using System.Collections;
using System.Collections.Generic;

namespace NTransit {
	[InputPort("IEnumerable")]
	public class ConvertIEnumerableToInformationPacketStream : SourceComponent {
		IEnumerator iterator;

		public ConvertIEnumerableToInformationPacketStream(string name) : base(name) {
			Receive["IEnumerable"] = data => {
				iterator = data.Accept().ContentAs<IEnumerable>().GetEnumerator();
				SendSequenceStart("Out");
				if (!iterator.MoveNext()) Status = ProcessStatus.Completed;
			};
			
			Update = () => {
				while (HasCapacity("Out") && Status == ProcessStatus.Active) {
					SendNew("Out", iterator.Current);
					if (!iterator.MoveNext()) Status = ProcessStatus.Completed;
				}
			};
			
			End = () => SendSequenceEnd("Out");
		}
	}
}