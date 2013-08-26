using System;
using Transit;
using System.Collections;

namespace TransitTest
{
	public class MockComponent : Component
	{
		public override IEnumerator Execute () 
		{
			yield break;
		}
	}
}