## Pipelines

A pipeline is a concurrency model which consists of a sequence of stages. Each stage performs a part of the full job and when it's done, it forwards it to the next stage. It also runs in its own thread and **shares no state** with the other stages.

<img src="https://deniskyashif.com/images/posts/csharp-channels-part3/pipeline-channels.png" />

### Generator

Each pipeline starts with a generator method, which initiates jobs by passing it to the stages. Each stage is also a method which runs on its own thread. The communication between the stages in a pipeline is performed using channels. A stage takes a channel as an input, reads from it asynchronously, performs some work and passes data to an output channel.

To see it in action, we're going to design a program that efficiently counts the lines of code in a project. Let's start with the generator.

``` cs --region get_files_recursively --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_pipeline
```

We perform a depth first traversal of the folder and its subfolders and write each file name we enounter to the output channel. When we're done with the traversal, we mark the channel as complete so the consumer (the next stage) knows when to stop reading from it.

### Stage 1 - Keep the Source Files

Stage 1 is going to determine whether the file contains source code or not. The ones that are not should be discarded.

``` cs --region filter_by_extension --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_pipeline
```

### Stage 2 - Get the Line Count

This stage is responsible for determining the number of lines in each file.

``` cs --region get_line_count --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_pipeline
```

``` cs --region count_lines --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_pipeline
```

## The Sink Stage

Now we've implemented the stages of our pipeline, we are ready to put them all together.

``` cs --region run_pipeline --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_pipeline
```

## Dealing with Bottlenecks

<img src="https://deniskyashif.com/images/posts/csharp-channels-part3/bottleneck.png" />

In the line counter example, the stage where we read the file and count its lines might cause a bottleneck when a file is sufficiently large. It makes sense to increase the capacity of this stage and that's where `Merge<T>` and `Split<T>` which we discussed as [Multiplexer](/Multiplexer.md) and [Demultiplexer](/Demultiplexer.md) come into use.

We're use `Split<T>` to distribute the source code files among 5 channels which will let us process up to 5 files simultaneously.

```cs
var fileSource = GetFilesRecursively("node_modules");
var sourceCodeFiles =
    FilterByExtension(fileSource, new HashSet<string> {".js", ".ts" });
var splitter = Split(sourceCodeFiles, 5);
```

During `Merge<T>`, we read concurrently from several channels and writes the messages to a single output. This is the stage which we need to tweak a little bit and perform the line counting.

We introduce, `CountLinesAndMerge` which doesn't only redirect, but also transforms.

``` cs --region count_lines_and_merge --source-file ./src/Program.cs --project ./src/TryChannelsDemo.csproj --session run_pipeline
```

You can update the code above for the sink stage and see how it performs for folders with lots of files. For more details on cancellation and error handling, check out the [blog post](https://deniskyashif.com/csharp-channels-part-3/)

#### Next: [Home &raquo;](../Readme.md) Previous: [Web Search &laquo;](../WebSearch.md)
