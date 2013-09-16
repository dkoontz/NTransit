using System;
using System.Collections;
using System.Collections.Generic;

namespace NTransit {
	public class ReadOnlyWrapper<T> : IEnumerable<T>, IEnumerable {
		ICollection<T> collection;

		public ReadOnlyWrapper(ICollection<T> collection) {
			this.collection = collection;
		}

		public IEnumerator<T> GetEnumerator() {
			return collection.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return collection.GetEnumerator();
		}
	}
}