using System;
using System.Collections;
using System.Collections.Generic;

namespace NTransit {
	[InputPort("ICollection")]
	[InputPort("Add")]
	[InputPort("Remove")]
	[InputPort("Send")]
	[OutputPort("Out")]
	public class CollectionStorage : Component {
		Queue<object> valuesToRemove;
		Queue<object> valuesToAdd;
		ReadOnlyWrapper<object> readOnlyWrapper;
		List<object> values;

		public CollectionStorage(string name) : base(name) {
			valuesToRemove = new Queue<object>();
			valuesToAdd = new Queue<object>();

			Receive["ICollection"] = data => {
				var collection = data.Accept().ContentAs<IEnumerable<object>>();
				values = new List<object>();

				foreach (var value in collection) {
					values.Add(value);
				}

				readOnlyWrapper = new ReadOnlyWrapper<object>(values);
			};

			Receive["Add"] = data => {
				valuesToAdd.Enqueue(data.Accept().Content);
			};

			Receive["Remove"] = data => {
				valuesToRemove.Enqueue(data.Accept().Content);
			};

			Receive["Send"] = data =>  {
				if (values != null) {
					data.Accept();
					if (valuesToRemove.Count > 0 || valuesToAdd.Count > 0) {
						while (valuesToRemove.Count > 0) {
							values.Remove(valuesToRemove.Dequeue());
						}
						
						while (valuesToAdd.Count > 0) {
							values.Remove(valuesToAdd.Dequeue());
						}
						readOnlyWrapper = new ReadOnlyWrapper<object>(values);
					}
					
					SendNew("Out", readOnlyWrapper);
				}
			};
		}
	}
}