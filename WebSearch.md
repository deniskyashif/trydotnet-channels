## Web Search

We're given the task to query several data sources and mix the results. The queries should run concurrently and we should disregard the ones taking too long. Also, we should handle a query response at the time of arrival, instead of waiting for all of them to complete.

``` cs --region web_search --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj
```
Depending on the timeout interval we might end up receiving responses for all of the queries, or cut off the ones that are too slow.

#### Next: [Home &raquo;](../Readme.md) Previous: [Quit Channel &laquo;](../QuitChannel.md) 
