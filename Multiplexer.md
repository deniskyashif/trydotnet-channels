## Multiplexer

We have two channels, want to read from both and process whichever's message arrives first. We're going to solve this by consolidating their messages into a single channel. Let's define the `Merge<T>()` method.

``` cs --region merge --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_multiplexing
```

`Merge<T>()` takes two channels and starts reading from them simultaneously. It creates and immediately returns a new channel which consolidates the outputs from the input channels. The reading procedures are run asynchronously on separate threads. Think of it like this:

<img src="http://localhost:1313/images/posts/2019-12-08-csharp-channels-part1/merge-sketch.png" width="600" />

`Merge<T>()` also works with an arbitrary number of inputs.  
We've created the local asynchronous `Redirect()` function which takes a channel as an input writes its messages to the consolidated output. It returns a `Task` so we can use `WhenAll()` to wait for the input channels to complete. This allows us to also capture potential exceptions. In the end, we know that there's nothing left to be read, so we can safely close the writer.

``` cs --region run_multiplexing --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_multiplexing
```

Our code is concurrent and non-blocking. The messages are being processed at the time of arrival and there's no need to use locks or any kind of conditional logic. While we're waiting, the thread is free to perform other work. We also don't have to handle the case when one of the writers complete (as you can see Ann has sent all of her messages before Joe).

``` cs --region generator --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_multiplexing
```

#### Next: [Timeout &raquo;](../Demultiplexer.md) Previous: [Generator &laquo;](../Generator.md)

