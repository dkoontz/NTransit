using System;
using System.Collections;
using System.Collections.Generic;

namespace NTransit {
	public class ReadOnlyWrapper<T> : IReadOnlyCollection<T> {
		ICollection<T> collection;

		public ReadOnlyWrapper(ICollection<T> collection) {
			this.collection = collection;
		}

		public int Count {
			get { return collection.Count; }
		}

		public IEnumerator<T> GetEnumerator() {
			return collection.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return collection.GetEnumerator();
		}
	}
}