## Generator

A generator is a method that returns a channel. The one below creates a channel and writes a given number of messages asynchronously from a separate thread.

``` cs --region generator --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_generator
```

By returning a `ChannelReader` we ensure that our consumers won't be able to attempt writing to it.

That's how we use the generator.

``` cs --region run_generator --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_generator
```

#### Next: [Multiplexer &raquo;](../Multiplexer.md) Previous: [Channels &laquo;](../Channels.md) 
