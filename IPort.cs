using System;

namespace nTransit {
	public interface IPort {
		string Name { get; set; }

		bool Closed { get; }

		Connection Connection { get; set; }

		bool HasConnection { get; }

		Component Component { get; set; }

		bool HasComponent { get; }

		void Close();
	}
}