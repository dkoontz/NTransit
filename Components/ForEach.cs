// The MIT License (MIT)
// 
// Copyright (c) 2013 David Koontz
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;

namespace NTransit {
	[InputPort("In")]
	[OutputPort("Out")]
	[OutputPort("Original")]
	public class ForEach : Component {
		public ForEach(string name) : base(name) { }

		public override void Setup() {
			InPorts["In"].Receive = data => {
				var ip = data.Accept();
				if (typeof(IEnumerable).IsAssignableFrom(ip.Content.GetType())) {
					foreach (var value in ip.ContentAs<IEnumerable>()) {
						SendNew("Out", value);
					}
				}
				else {
					throw new ArgumentException(string.Format("IP content was {0}, but must be an IEnumerable", ip.Content.GetType()));
				}

				if (OutPorts["Original"].Connected) {
					Send("Original", ip);
				}
			};
		}
	}
}