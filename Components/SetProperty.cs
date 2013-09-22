using System;

namespace NTransit {

	[InputPort("Object")]
	[InputPort("Property")]
	[InputPort("Value")]
	[OutputPort("Out")]
	public class SetProperty : Component {
		object value;
		string propertyName;

		public SetProperty(string name) : base(name) {
			Receive["Value"] = data => value = data.Accept().Content;
			Receive["Property"] = data => propertyName = data.Accept().ContentAs<string>();
			Receive["Object"] = data => {
				var ip = data.Accept();
				var target = ip.Content;

				var propertyAccessor = PropertyAccessor.CreateAccessor(target.GetType().GetProperty(propertyName));
				propertyAccessor.SetValue(target, value);
			};
		}
	}
}