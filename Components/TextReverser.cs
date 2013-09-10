using System;
using System.Text;

namespace NTransit {
	[InputPort("In")]
	[OutputPort("Out")]
	public class TextReverser : Component {
		public TextReverser(string name) : base(name) {}

		public override void Init() {
			OnReceive("In", data => {
				var ip = data.Accept();
				var content = ip.ContentAs<string>();
				var builder = new StringBuilder(content.Length);
				foreach (var c in content) {
					builder.Insert(0, c);
				}
				ip.Content = builder.ToString();

				Send("Out", ip);
			});
		}
	}
}