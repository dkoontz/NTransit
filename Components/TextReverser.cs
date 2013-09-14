using System;
using System.Text;

namespace NTransit {
	public class TextReverser : PropagatorComponent {
		public TextReverser(string name) : base(name) {
			Receive["In"] = data => {
				var ip = data.Accept();
				var content = ip.ContentAs<string>();
				var builder = new StringBuilder(content.Length);
				foreach (var c in content) {
					builder.Insert(0, c);
				}
				ip.Content = builder.ToString();

				Send("Out", ip);
			};
		}
	}
}