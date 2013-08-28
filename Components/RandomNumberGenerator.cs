using System;
using System.Collections;

namespace NTransit {
	public class RandomNumberGenerator : Component {
		[OutputPort ("Number")]
		StandardOutputPort output;

		Random random;

		public RandomNumberGenerator(string name) : base (name) {
			random = new Random(name.GetHashCode());
		}

		public override IEnumerator Execute() {
			while (true) {
				while (!output.TrySend(random.Next())) yield return WaitForCapacityOn(output);
			}
		}
	}
}