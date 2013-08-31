using System;

namespace NTransit {
	public interface IPort {
		string Name { get; set; }

		bool Closed { get; }

		IConnection Connection { get; set; }

		bool HasConnection { get; }

		Component Process { get; set; }

		bool HasComponent { get; }

		void Close();
	}
}