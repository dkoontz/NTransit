using System;

namespace Transit
{
	public interface IPort
	{
		string Name { get; set; }

		bool Closed { get; }

		Connection Connection { get; set; }

		bool HasConnection { get; }

		Component Component { set; }

		bool HasComponent { get; }

		void Close ();
	}
}