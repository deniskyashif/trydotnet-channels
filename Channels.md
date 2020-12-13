## Introducing Channels

A channel is a data structure that allows one thread to communicate with another thread. By communicating, we mean sending and receiving data in a first in first out (FIFO) order. Currently, they are part of the `System.Threading.Channels` namespace.

<img src="https://deniskyashif.com/images/posts/2019-12-08-csharp-channels-part1/channel-sketch.png" width="600" />

That's how we create a channel:

```csharp
Channel<string> ch = Channel.CreateUnbounded<string>();
```

`Channel` is a static class that exposes several factory methods for creating channels. `Channel<T>` is a data structure that supports reading and writing. That's how we write asynchronously to a channel:

```csharp
await ch.Writer.WriteAsync("My first message");
await ch.Writer.WriteAsync("My second message");
ch.Writer.Complete();
```

And this is how we read from a channel.

```csharp
while (await ch.Reader.WaitToReadAsync()) 
    Console.WriteLine(await ch.Reader.ReadAsync());
```

The reader's `WaitToReadAsync()` will complete with `true` when data is available to read, or with `false` when no further data will ever be read, that is, after the writer invokes `Complete()`. The reader also provides an option to consume the data as an async stream by exposing a method that returns `IAsyncEnumerable<T>`:

```cs
await foreach (var item in ch.Reader.ReadAllAsync())
    Console.WriteLine(item);
```

## A basic pub/sub messaging scenario

Here's a basic example when we have a separate producer and consumer threads which communicate through a channel.

``` cs --region run_basic_channel_usage --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj
```

The consumer (reader) waits until there's an available message to read. On the other hand, the producer (writer) waits until it's able to send a message, hence, we say that **channels both communicate and synchronize**. Both operations are non-blocking, that is, while we wait, the thread is free to do some other work.  
Notice that we have created an **unbounded** channel, meaning that it accepts as many messages as it can with regards to the available memory. With **bounded** channels, however, we can limit the number of messages that can be processed at a time. 

#### Next: [Generator &raquo;](../Generator.md) Previous: [Home &laquo;](../Readme.md)
