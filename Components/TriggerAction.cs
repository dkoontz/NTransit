using System;
using System.Reflection;

namespace NTransit {
	[InputPort("Action")]
	public class TriggerAction : PropagatorComponent {
		string actionName;

		public TriggerAction(string name) : base(name) {
			Receive["Action"] = data => actionName = data.Accept().ContentAs<string>();
			Receive["In"] = data => {
				var ip = data.Accept();
				var target = ip.Content;
				var action = target.GetType().GetMethod(actionName);
				if (action == null) {
					throw new ArgumentException(string.Format("The type '{0}' does not contain a method named '{1}'", target.GetType(), actionName));
				}

				try {
					var actionDelegate = (Action)Delegate.CreateDelegate(typeof(Action), target, action);
					actionDelegate();
				}
				catch(ArgumentException) {
					throw new ArgumentException(string.Format("The target action '{0}' is not a System.Action", actionName));
				}

				Send("Out", ip);
			};
		}
	}
}