using System;

namespace NTransit {

	[InputPort("Object")]
	[InputPort("Property")]
	[InputPort("Value")]
	[OutputPort("Out")]
	public class SetProperty : Component {
		object value;
		string propertyName;

		public SetProperty(string name) : base(name) { }

		public override void Setup() {
			InPorts["Value"].Receive = data => value = data.Accept().Content;
			InPorts["Property"].Receive = data => propertyName = data.Accept().ContentAs<string>();
			InPorts["Object"].Receive = data => {
				var ip = data.Accept();
				var target = ip.Content;

				var propertyAccessor = PropertyAccessor.CreateAccessor(target.GetType().GetProperty(propertyName));
				propertyAccessor.SetValue(target, value);
			};
		}
	}
}