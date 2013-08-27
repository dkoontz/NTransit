using System;
using System.Collections;

namespace nTransit {
	public class RandomNumberGenerator : Component {
		[OutputPort ("Number")]
		StandardOutputPort numberOutput;

		Random random;

		public RandomNumberGenerator(string name) : base (name) {
			random = new Random(name.GetHashCode());
		}

		public override IEnumerator Execute() {
			while (true) {
				while (!numberOutput.Send (random.Next ())) {
					yield return WaitForCapacityOn(numberOutput);
				}
			}
		}
	}
}