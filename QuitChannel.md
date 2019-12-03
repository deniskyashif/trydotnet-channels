## Quit Channel

Unlike the Timeout, where we cancel reading, this time we want to cancel the writing procedure. We need to modify our `CreateMessenger()` generator.

``` cs --region generator_with_cancellation --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_quit_channel
```

Now we need to pass our cancellation token to the generator which gives us control over the channel's longevity.

``` cs --region run_quit_channel --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_quit_channel
```

Joe had 10 messages to send, but we gave him only 5 seconds, for which he managed to send less than that. We can also manually send a cancellation request, for example, after reading `N` number of messages.

#### Next: [Web Search &raquo;](../WebSearch.md) Previous: [Timeout &laquo;](../Timeout.md) 
