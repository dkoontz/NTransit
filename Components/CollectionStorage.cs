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
		}

		public override void Setup() {
			InPorts["ICollection"].Receive = data => {
				var collection = data.Accept().ContentAs<IEnumerable<object>>();
				values = new List<object>();
				
				foreach (var value in collection) {
					values.Add(value);
				}
				
				readOnlyWrapper = new ReadOnlyWrapper<object>(values);
			};
			
			InPorts["Add"].Receive = data => {
				valuesToAdd.Enqueue(data.Accept().Content);
			};
			
			InPorts["Remove"].Receive = data => {
				valuesToRemove.Enqueue(data.Accept().Content);
			};
			
			InPorts["Send"].Receive = data =>  {
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