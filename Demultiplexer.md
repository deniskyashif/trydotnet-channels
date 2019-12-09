## Demultiplexer

We want to distribute the work amongst several consumers. Let's define `Split<T>()`:

<img src="http://localhost:1313/images/posts/2019-12-08-csharp-channels-part1/split-sketch.png" width="600" />

``` cs --region split --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_demultiplexing
```

`Split<T>` takes a channel and redirects its messages to `n` number of newly created channels in a round-robin fashion. It returns these channels as read-only. Here's how to use it:


``` cs --region run_demultiplexing --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_demultiplexing
```

Joe sends 10 messages which we distribute amongst 3 channels. Some channels may take longer to process a message, therefore, we have no guarantee that the order of emission is going to be preserved. Our code is structured so that we process (in this case log) a message as soon as it arrives.

``` cs --region generator --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_demultiplexing
```

#### Next: [Timeout &raquo;](../Timeout.md) Previous: [Multiplexer &laquo;](../Multiplexer.md)
