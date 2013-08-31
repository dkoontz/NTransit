using System;
using System.Collections;

namespace NTransit {
	public class RandomNumberGenerator : Component {
		[OutputPort ("Number")]
		public StandardOutputPort Output { get; set; }

		Random random;

		public RandomNumberGenerator(string name) : base (name) {
			random = new Random(name.GetHashCode());
		}

		public override IEnumerator Execute() {
			while (true) {
				while (!Output.TrySend(random.Next())) yield return WaitForCapacityOn(Output);
			}
		}
	}
}