using System;

namespace NTransit {
	[InputPort("Object")]
	[InputPort("Field")]
	[InputPort("Value")]
	[OutputPort("Out")]
	public class SetField : Component {
		object value;
		string fieldName;

		public SetField(string name) : base(name) { }

		public override void Setup() {
			InPorts["Value"].Receive = data => value = data.Accept().Content;
			InPorts["Field"].Receive = data => fieldName = data.Accept().ContentAs<string>();
			InPorts["Object"].Receive = data => {
				var ip = data.Accept();
				var target = ip.Content;
				target.GetType().GetField(fieldName).SetValue(target, value);
			};
		}
	}
}