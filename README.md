# C# Concurrency Patterns with Channels

[![Build Status](https://github.com/deniskyashif/trydotnet-channels/workflows/.NET%20Core/badge.svg)](https://github.com/deniskyashif/trydotnet-channels/actions?query=workflow%3A%22.NET+Core%22)

<img src="https://deniskyashif.com/images/posts/2019-12-08-csharp-channels-part1/channel-sketch.png" width="450" />

This project contains interactive examples for implementing concurrent workflows in C# using channels. Check my blog posts on the topic:  
* [C# Channels - Publish / Subscribe Workflows](https://deniskyashif.com/csharp-channels-part-1/)
* [C# Channels - Timeout and Cancellation](https://deniskyashif.com/csharp-channels-part-2/)
* [C# Channels - Async Data Pipelines](https://deniskyashif.com/csharp-channels-part-3/)

## Table of contents

- [Using Channels](Channels.md)
- [Generator](Generator.md)
- [Multiplexer](Multiplexer.md)
- [Demultiplexer](Demultiplexer.md)
- [Timeout](Timeout.md)
- [Quit Channel](QuitChannel.md)
- [Web Search](WebSearch.md)
- [Pipelines](Pipelines.md)

## How to run it

Install [dotnet try](https://github.com/dotnet/try/blob/master/DotNetTryLocal.md)

```
git clone https://github.com/deniskyashif/trydotnet-channels.git
cd trydotnet-channels
dotnet try
```
