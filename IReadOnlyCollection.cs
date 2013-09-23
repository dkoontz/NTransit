using System;
using System.Collections;
using System.Collections.Generic;

namespace NTransit {
	public interface IReadOnlyCollection<T> : IEnumerable<T>, IEnumerable {
		int Count { get; }
	}
}