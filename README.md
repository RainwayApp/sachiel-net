# sachiel-net

## What is it?

Sachiel is a messaging framework built on top of [Google's Protocol Buffers](https://developers.google.com/protocol-buffers/). The goal of this framework is to let developers easily setup and deploy APIs inside their applications (regardless of language) without any headache. Use cases for Sachiel include setting up APIs for your IPC or network messaging.


## Progress

### Supported Langauges 

- C#
- [Typescript](https://github.com/RainwayApp/sachiel-ts)

### TODO Langauges 

- Php (in progress)
- Java (not started)
- C++ (not started)
- Ruby (not started)
- Python (not started)

If you wish to help port Sachiel to another langauge feel free, we're happy to link from this repo. 

## Getting Started 

### Introduction

This library makes use of [protobuf-net](https://github.com/mgravell/protobuf-net); you should take some time to read over it.


### Creating A Consumer 

Consumers are a way to handle individual messages for a particular connection or user. Here is an example of what a Consumer can look like:

```csharp
internal class ExampleConsumer : Consumer
{
        public override object SyncObject { get; set; }

        public override void Reply(PacketCallback packet)
        {
            var session = (IWebSocketSession) SyncObject;
            if (session != null && session.State == WebSocketState.Open)
            {
                Console.WriteLine("Replied to endpoint: " + packet.Endpoint);
                session.Context.WebSocket.Send(packet.Response);
            }
        }
}
```

Consumers contain a SyncObject; this object is set at initialization can is usually something that allows you to reply to the calling user/connection. In this example its a WebSocketSession, so a message can be sent back to the calling WebSocketSession. The PacketCallback is explained in "Creating A Handler" below.


### Creating An Endpoint

For this example let's say we need an endpoint for requesting a file tree from a remote computer based on the path. Creating your endpoint model is this simple:

```csharp
    [ProtoContract]
    [SachielEndpoint(Name = "RequestFileTree", Handler = typeof(FilePacketHandler))]
    public class RequestFileTree : Message
    {
        [ProtoMember(1)]
        public string Path { get; set; }
    }
```

Each endpoint is designated a name and handler via the ```SachielEndpoint``` attribute. The handler is executed when the message is successfully deserialized, this is where custom handling logic and replying will take place.

You'll need to mark your models with the ```ProtoContract``` attribute and sort your ```ProtoMember``` members accordingly. Only models that implement ```Message``` can be serialized.


### Creating A Handler

You'll likely want to perform actions on incoming messages, to do this you can create a handler and assign it to your endpoint. Here is an example of a handler:

```csharp
internal class FilePacketHandler : PacketHandler
    {
        private PacketCallback _callback;
        private Consumer _consumer;
        private Message _message;
        private Packet _packet;

        public override void HandlePacket(Consumer consumer, Packet packet)
        {
            _consumer = consumer;
            _packet = packet;
            _message = _packet.Message;
            _callback = new PacketCallback {Endpoint = _message.Header.Endpoint };
            switch (_message.Header.Endpoint)
            {
                case "RequestFileTree":
                    HandleFileTree();
                    break;
            }
        }


        private void HandleFileTree()
        {
            //cast our deserialized source to its original model
            var fileRequest = (RequestFileTree) _message.Source;
            Console.WriteLine($"Request for {fileRequest.Path} received");
            //create a response
            var response = new TreeResponse {Tree = new FileTree(fileRequest.Path)};
            //serialize the response
            _callback.Response = response.Serialize();
            //reply with our file tree
            _consumer.Reply(_callback);
        }
    }
```

Each handler can contain logic for multiple endpoints, so it's recommended you make use of the message headers. 


### Creating A Message

Messages contain headers, these headers dictate which endpoint the message is destined for and a sync key to associate a response with a message. 

You have a different options when creating messages, and sync keys are completely optional. 

You can create a serialized message in a static fashion with an existing model:

```csharp
Message.Serialized("MyEndPoint", "MySyncKey", MyModel);
```

Or as a model:

```csharp
[ProtoContract]
    [SachielHeader(Endpoint = "TreeResponse")]
    internal class TreeResponse : Message
    {
        [ProtoMember(1)]
        public FileTree Tree { get; set; }
    }
```

At which point you can call ```TreeResponse.Serialize()```.

You can find more information by reading the documentation of the ```Message``` class. 

### Packet Loading

For handlers and endpoints to be known, you'll need to make use of the ```PacketLoader```. When calling ```PacketLoader.SavePackets``` it will loop over classes marked with ```SachielEndpoint``` and serialize them to a Message. You can then save this to a stream of your choice. At run time you will call ```PacketLoader.LoadPackets``` which will load your serialize endpoints from the raw buffer.


### Deserializing Packets and Handling Packets

When receiving a packet, you'll want to pass the buffer to ```Packet.GetPacket```, simple as that.

```csharp
var packet = Packet.GetPacket(data);
packet?.HandlePacket(_consumer);
```

### Notes

This documentation and framework is a WIP. If you'd like to contribute we'll be happy to accept pull request. You can find a full example application in the repository.


## Links

[Website](https://rainway.io/)

[Twitter](https://twitter.com/rainwayapp)
