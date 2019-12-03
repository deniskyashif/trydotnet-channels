using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Console;

public class Program
{
    static Action<string> WriteLineWithTime =
        (str) => WriteLine($"[{DateTime.UtcNow.ToLongTimeString()}] {str}");

    static async Task Main(
        string region = null,
        string session = null,
        string package = null,
        string project = null,
        string[] args = null)
    {
        if (!string.IsNullOrWhiteSpace(session))
        {
            switch (session)
            {
                case "run_generator":
                    await RunGenerator(); break;
                case "run_multiplexing":
                    await RunMultiplexing(); break;
                case "run_demultiplexing":
                    await RunDemultiplexing(); break;
                case "run_timeout":
                    await RunTimeout(); break;
                case "run_quit_channel":
                    await RunQuitChannel(); break;
                default:
                    WriteLine("Unrecognized session"); break;
            }
            return;
        }

        switch (region)
        {
            case "run_basic_channel_usage":
                await RunBasicChannelUsage(); break;
            case "web_search":
                await RunWebSearch(); break;
            default:
                WriteLine("Unrecognized region"); break;
        }
    }

    public static async Task RunBasicChannelUsage()
    {
#region run_basic_channel_usage
        var ch = Channel.CreateUnbounded<string>();

        var consumer = Task.Run(async () =>
        {
            while (await ch.Reader.WaitToReadAsync())
                WriteLineWithTime(await ch.Reader.ReadAsync());                
        });

        var producer = Task.Run(async () =>
        {
            var rnd = new Random();
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(rnd.Next(3)));
                await ch.Writer.WriteAsync($"Message {i}");
            }
            ch.Writer.Complete();
        });

        await Task.WhenAll(consumer, producer);
#endregion
    }

    public static async Task RunGenerator()
    {
#region run_generator
        var joe = CreateMessenger("Joe", 5);
        
        await foreach (var item in joe.ReadAllAsync())
            WriteLineWithTime(item);
#endregion
    }

    public static async Task RunMultiplexing()
    {
#region run_multiplexing
        var ch = Merge(CreateMessenger("Joe", 3), CreateMessenger("Ann", 5));

        await foreach (var item in ch.ReadAllAsync())
            WriteLineWithTime(item);
#endregion
    }

    public static async Task RunDemultiplexing()
    {
#region run_demultiplexing
        var joe = CreateMessenger("Joe", 10);
        var readers = Split(joe, 3);
        var tasks = new List<Task>();

        for (int i = 0; i < readers.Count; i++)
        {
            var reader = readers[i];
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                await foreach (var item in reader.ReadAllAsync())
                {
                    WriteLineWithTime(string.Format("Reader {0}: {1}", index, item));
                }
            }));
        }

        await Task.WhenAll(tasks);
#endregion
    }

    public static async Task RunTimeout()
    {
#region run_timeout
        var joe = CreateMessenger("Joe", 10);
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            await foreach (var item in joe.ReadAllAsync(cts.Token))
                Console.WriteLine(item);

            Console.WriteLine("Joe sent all of his messages."); 
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Joe, you are too slow!");
        }
#endregion
    }

    public static async Task RunQuitChannel()
    {
#region run_quit_channel
        var cts = new CancellationTokenSource();
        var joe = CreateMessenger("Joe", 10, cts.Token);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        await foreach (var item in joe.ReadAllAsync())
            WriteLineWithTime(item);
#endregion
    }

#region generator
    static ChannelReader<string> CreateMessenger(string msg, int count)
    {
        var ch = Channel.CreateUnbounded<string>();
        var rnd = new Random();

        Task.Run(async () =>
        {
            for (int i = 0; i < count; i++)
            {
                await ch.Writer.WriteAsync($"{msg} {i}");
                await Task.Delay(TimeSpan.FromSeconds(rnd.Next(3)));
            }
            ch.Writer.Complete();
        });

        return ch.Reader;
    }
#endregion

#region generator_with_cancellation
    static ChannelReader<string> CreateMessenger(
        string msg,
        int count = 5,
        CancellationToken token = default(CancellationToken))
    {
        var ch = Channel.CreateUnbounded<string>();
        var rnd = new Random();

        Task.Run(async () =>
        {
            for (int i = 0; i < count; i++)
            {
                if (token.IsCancellationRequested)
                {
                    await ch.Writer.WriteAsync($"{msg} says bye!");
                    break;
                }
                await ch.Writer.WriteAsync($"{msg} {i}");
                await Task.Delay(TimeSpan.FromSeconds(rnd.Next(0, 3)));
            }
            ch.Writer.Complete();
        });

        return ch.Reader;
    }
#endregion
    
#region merge
    static ChannelReader<T> Merge<T>(params ChannelReader<T>[] inputs)
    {
        var output = Channel.CreateUnbounded<T>();

        Task.Run(async () =>
        {
            async Task Redirect(ChannelReader<T> input)
            {
                await foreach (var item in input.ReadAllAsync())
                    await output.Writer.WriteAsync(item);
            }
            
            await Task.WhenAll(inputs.Select(i => Redirect(i)).ToArray());
            output.Writer.Complete();
        });

        return output;
    }
#endregion

#region split
    static IList<ChannelReader<T>> Split<T>(ChannelReader<T> ch, int n)
    {
        var outputs = new List<Channel<T>>(n);

        for (int i = 0; i < n; i++)
            outputs.Add(Channel.CreateUnbounded<T>());

        Task.Run(async () =>
        {
            var index = 0;
            await foreach (var item in ch.ReadAllAsync())
            {
                await outputs[index].Writer.WriteAsync(item);
                index = (index + 1) % n;
            }

            foreach (var chan in outputs)
                chan.Writer.Complete();
        });

        return outputs.Select(c => c.Reader).ToList();
    }
#endregion

    public static async Task RunWebSearch()
    {
#region web_search
        var term = "Jupyter";
        var ch = Channel.CreateUnbounded<string>();

        async Task Search(string source, string term, CancellationToken token)
        {
            await Task.Delay(TimeSpan.FromSeconds(new Random().Next(5)), token);
            await ch.Writer.WriteAsync($"Result from {source} for {term}", token);
        }

        var token = new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token;
        var search1 = Search("Wikipedia", term, token);
        var search2 = Search("Quora", term, token);
        var search3 = Search("Everything2", term, token);

        try
        {
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine(await ch.Reader.ReadAsync(token));
            }
            Console.WriteLine("All searches have completed.");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Timeout.");
        }

        ch.Writer.Complete();
#endregion
    }
}
