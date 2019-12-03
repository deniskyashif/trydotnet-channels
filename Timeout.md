## Timeout

We want to read from a channel for a certain amount of time.

``` cs --region run_timeout --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_timeout
```

In this example, Joe was set to send 10 messages but over 5 seconds, we received less than that and then canceled the reading operation. If we reduce the number of messages Joe sends, or sufficiently increase the timeout duration, we'll read everything and thus avoid ending up in the `catch` block.

``` cs --region generator --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_timeout
```

#### Next: [Quit Channel &raquo;](../QuitChannel.md) Previous: [Demultiplexer &laquo;](../Demultiplexer.md) 
