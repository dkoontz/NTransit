# NTransit

### What is Flow Based Programming (FBP)?
Briefly put, Flow Based Programming is an architectural pattern by which you think of your program as being comprised of independently executing processes that communicate with each other over well defined connections between output pots and input ports.  FBP networks are generally drawn out in an editor or described textually as the connections between the various components' ports.  When the network is executed, the components that are declared are instantiated and become the running processes of the system.  A process is an instance of a component. FBP is different from many "dataflow" style systems in that the only thing that moves between processes is an "Information Packet" or IP.  This simple abstraction allows for the authoring of components that know nothing about the actual data of an IP but can nonetheless operate on the packet in some fashion (batching up several packets to be sent at once, routing it based on metadata attached to the packet, etc.).  For those wanting to know more I highly suggest reading the book ["Flow Based Programming"](http://www.amazon.com/Flow-Based-Programming-2nd-Edition-ebook/dp/B004PLO66O/ref=sr_1_1?ie=UTF8&qid=1379994068&sr=8-1&keywords=flow+based+programming) by J Paul Morrison the originator of the technique.  Listed below is a free version of the 1st edition and the second edition, updated in 2011 and available on Amazon.

* [The original FBP book by J Paul Morrison](http://www.jpaulmorrison.com/fbp/)
* [Second edition of Pauls' FBP book](http://www.amazon.com/Flow-Based-Programming-2nd-Edition-ebook/dp/B004PLO66O/ref=sr_1_1?ie=UTF8&qid=1379994068&sr=8-1&keywords=flow+based+programming)
* [FBP Google Group](https://groups.google.com/forum/#!forum/flow-based-programming)
* [FBP subreddit](http://www.reddit.com/r/FlowBasedProgramming)

### NTransit at 30,000 feet

**NTransit is still very early on and I would not recommend anyone using it for anything serious.  A few [test applications](https://github.com/dkoontz/ntransit-spaceinvaders) are being built with it to shake out an major problems and get the various schedulers built out but outside of the problem domains those applications are exploring the component library and to a degree the capabilities are not fully built out.**

NTransit is a FBP runtime for C# 3.5 (.NET and Mono equivalents) and above.  NTransit aims to be usable in a variety of situations such as in desktop applications, server-side, and on mobile devices. To this end, NTransit does not make any assumptions about how your processes are scheduled, instead leaving that up to a pluggable scheduler which can be multi-threaded, single threaded, or some combination of the two, (for example using a thread pool).

NTransit uses an event style model for notifying a component of incoming packets.  Ports are declared as attributes on the class and then you can declare event handlers for that port.  The events for an input port are Receive, SequenceStart, SequenceEnd.  There are also three events for the component as a whole: 

```C#
public Action Start;
public Func<bool> Update;
public Action End;
```

The component can choose to accept the packet or decline it which leaves the packet in the incoming port's connection.  Here is an example component that receives an IP on the In port, gets its content as a string, adds an &lt;h1&gt; around the string, and then forwards the IP to the Out port.

```C#
[InputPort("In")]
[OutputPort("Out")]
public class AddHeaderTag : Component {
    public MyComponent(string name) : base(name) {
        Receive["In"] = data => {
            ip = data.Accept();
            var value = ip.ContentAs<string>();
            ip.Content = string.Format("<h1>{0}</h1>", value);
        }
        Send("Out", ip);
    }
}
```

A slightly more complex version might be to allow the component to be told what tag to wrap around the content via another port.

```C#
[InputPort("Tag")]
[InputPort("In")]
[OutputPort("Out")]
public class AddHeaderTag : Component {
    string tag;

    public MyComponent(string name) : base(name) {
        Receive["Tag"] = data => tag = data.Accept().ContentAs<string>();
        Receive["In"] = data => {
            ip = data.Accept();
            var value = ip.ContentAs<string>();
            ip.Content = string.Format("<{0}>{1}</{0}>", tag, value);
        }
        Send("Out", ip);
    }
}
```

Networks can be created via code or by a string containing a compact representation of a network.  Currently this is a format unique to NTransit but hopefully soon there will be some standardization of a .fbp format (and also a JSON format) that can then be adopted.  The current format uses the following:

```
'My literal string data' -> NameOfProcess(TypeOfComponent).InputPortName
NameOfProcess.OutputPortName -> OtherProcess(DifferentComponentType).InputPortName
```

Types are only declared the first time they are encountered, so the second use of NameOfProcess does not need the parentheses and type declaration.  Literal types that are supported are
* int
* float
* string
* bool

### Example projects
* [Space Invaders FBP](https://github.com/dkoontz/ntransit-spaceinvaders) - an example game using the [Unity](http://unity3d.com) game engine]

### Related projects
* [NoFlo visual editor](https://github.com/noflo/noflo-ui)
* [NoFlo, a Javascript FBP implementation](http://noflojs.org/)
* [JavaFBP, a Java implementation by J Paul Morrison](http://www.jpaulmorrison.com/fbp/#JavaFBP)
* [C#FBP, a C# implementation ported from JavaFBP](http://www.jpaulmorrison.com/fbp/#CsharpFBP)
* [DrawFBP applications by J Paul Morrison](http://www.jpaulmorrison.com/graphicsstuff/)
* [GoFlow, a Golang FBP](https://github.com/trustmaster/goflow)
* [Ruby FBP](http://murfware.org/wiki/projects/rubyfbp/Ruby_Fbp.html)
