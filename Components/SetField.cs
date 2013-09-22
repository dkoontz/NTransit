using System;

namespace NTransit {
	[InputPort("Object")]
	[InputPort("Field")]
	[InputPort("Value")]
	[OutputPort("Out")]
	public class SetField : Component {
		object value;
		string fieldName;

		public SetField(string name) : base(name) {
			Receive["Value"] = data => value = data.Accept().Content;
			Receive["Field"] = data => fieldName = data.Accept().ContentAs<string>();
			Receive["Object"] = data => {
				var ip = data.Accept();
				var target = ip.Content;
				foreach (var field in target.GetType().GetFields()) {
					if (field.Name == fieldName) {
						field.SetValue(target, value);
					}
				}
			};
		}
	}
}