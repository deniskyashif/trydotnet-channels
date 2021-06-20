#nullable enable

namespace TryChannelsDemo {

	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using System.Threading.Channels;
	using System.Threading.Tasks;
	using static System.Console;

	public class Program {

		/// <summary>
		///     Returns the count of lines read from <paramref name="file" /> until the end of file, or until
		///     <paramref name="cancellationToken" /> has been cancelled.
		/// </summary>
		/// <param name="file"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		private static async Task<Int32> CountLines( FileSystemInfo file, CancellationToken cancellationToken ) {
			var lines = 0;

			file.Refresh();

			if ( file.Exists ) {
				using var sr = new StreamReader( file.FullName );

				while ( await sr.ReadLineAsync().ConfigureAwait( false ) != null ) {
					lines++;
					if ( cancellationToken.IsCancellationRequested ) {
						break;
					}
				}
			}

			return lines;
		}

		private static async Task<(ChannelReader<(FileInfo file, Int32 lines)> output, ChannelReader<String> errors)> CountLinesAndMerge(
			IEnumerable<ChannelReader<FileInfo>> inputs,
			CancellationToken cancellationToken
		) {
			var output = Channel.CreateUnbounded<(FileInfo file, Int32 lines)>();
			var errors = Channel.CreateUnbounded<String>();

			async Task Redirect( ChannelReader<FileInfo> input ) {
				await foreach ( var file in input.ReadAllAsync( cancellationToken ) ) {
					var lines = await CountLines( file, cancellationToken ).ConfigureAwait( false );
					if ( lines == 0 ) {
						await errors.Writer.WriteAsync( $"[Error] Empty file {file}", cancellationToken ).ConfigureAwait( false );
					}
					else {
						await output.Writer.WriteAsync( (file, lines), cancellationToken ).ConfigureAwait( false );
					}
				}
			}

			await Task.WhenAll( inputs.Select( Redirect ).ToArray() ).ConfigureAwait( false );
			output.Writer.Complete();
			errors.Writer.Complete();

			return (output, errors);
		}

		private static async Task<ChannelReader<String>> CreateMessenger( String msg, Int32 count ) {
			var ch = Channel.CreateUnbounded<String>();

			for ( var i = 0; i < count; i++ ) {
				await ch.Writer.WriteAsync( $"{msg} {i}" ).ConfigureAwait( false );
				await Task.Delay( TimeSpan.FromSeconds( Randem.Instance().Next( 3 ) ) ).ConfigureAwait( false );
			}

			ch.Writer.Complete();

			return ch.Reader;
		}

		private static async Task<ChannelReader<String>> CreateMessenger( String msg, Int32? count, CancellationToken token ) {
			count ??= 5;

			var ch = Channel.CreateUnbounded<String>();

			for ( var i = 0; i < count; i++ ) {
				if ( token.IsCancellationRequested ) {
					await ch.Writer.WriteAsync( $"{msg} says bye!", token ).ConfigureAwait( false );
					break;
				}

				await ch.Writer.WriteAsync( $"{msg} {i}", token ).ConfigureAwait( false );
				await Task.Delay( TimeSpan.FromSeconds( Randem.Instance().Next( 0, 3 ) ), token ).ConfigureAwait( false );
			}

			ch.Writer.Complete();

			return ch.Reader;
		}

		private static async Task<ChannelReader<FileInfo>> FilterByExtensionAsync( ChannelReader<String> input, IReadOnlySet<String> exts ) {
			var output = Channel.CreateUnbounded<FileInfo>();

			await foreach ( var file in input.ReadAllAsync() ) {
				var fileInfo = new FileInfo( file );
				if ( exts.Contains( fileInfo.Extension ) ) {
					await output.Writer.WriteAsync( fileInfo ).ConfigureAwait( false );
				}
			}

			output.Writer.Complete();

			return output;
		}

		private static async Task<ChannelReader<String>> GetFilesRecursivelyAsync( String root, CancellationToken token ) {
			var output = Channel.CreateUnbounded<String>();

			async Task WalkDir( String path ) {
				if ( token.IsCancellationRequested ) {
					throw new OperationCanceledException();
				}

				foreach ( var file in Directory.EnumerateFiles( path ) ) {
					await output.Writer.WriteAsync( file, token ).ConfigureAwait( false );
				}

				var tasks = Directory.EnumerateDirectories( path ).Select( WalkDir );
				await Task.WhenAll( tasks.ToArray() ).ConfigureAwait( false );
			}

			try {
				await WalkDir( root ).ConfigureAwait( false );
			}
			catch ( OperationCanceledException ) {
				WriteLine( "Cancelled." );
			}
			finally {
				output.Writer.Complete();
			}

			return output;
		}

		private static async Task<(ChannelReader<(FileInfo file, Int32 lines)> output, ChannelReader<String> errors)> GetLineCountAsync(
			ChannelReader<FileInfo> input,
			CancellationToken cancellationToken
		) {
			var output = Channel.CreateUnbounded<(FileInfo, Int32)>();
			var errors = Channel.CreateUnbounded<String>();

			await foreach ( var file in input.ReadAllAsync( cancellationToken ) ) {
				var lines = await CountLines( file, cancellationToken ).ConfigureAwait( false );
				if ( lines == 0 ) {
					await errors.Writer.WriteAsync( $"[Error] Empty file {file}", cancellationToken ).ConfigureAwait( false );
				}
				else {
					await output.Writer.WriteAsync( (file, lines), cancellationToken ).ConfigureAwait( false );
				}
			}

			output.Writer.Complete();
			errors.Writer.Complete();

			return (output, errors);
		}

		private static async Task<ChannelReader<T>> Merge<T>( params ChannelReader<T>[] inputs ) {
			var output = Channel.CreateUnbounded<T>();

			async Task Redirect( ChannelReader<T> input ) {
				await foreach ( var item in input.ReadAllAsync() ) {
					await output.Writer.WriteAsync( item ).ConfigureAwait( false );
				}
			}

			await Task.WhenAll( inputs.Select( Redirect ).ToArray() ).ConfigureAwait( false );
			output.Writer.Complete();

			return output;
		}

		private static async Task<IList<ChannelReader<T>>> Split<T>( ChannelReader<T> channelReader, Int32 n ) {
			var outputs = new Channel<T>[n];

			for ( var i = 0; i < n; i++ ) {
				outputs[i] = Channel.CreateUnbounded<T>();
			}

			var index = 0;
			await foreach ( var item in channelReader.ReadAllAsync() ) {
				await outputs[index].Writer.WriteAsync( item ).ConfigureAwait( false );
				index = ( index + 1 ) % n;
			}

			foreach ( var channel in outputs ) {
				channel.Writer.Complete();
			}

			return outputs.Select( ch => ch.Reader ).ToArray();
		}

		private static void WriteLineWithTime( String s ) => WriteLine( $"[{DateTime.UtcNow.ToLongTimeString()}] {s}" );

		public static async Task Main( String[] args ) {
			var choice = Randem.Instance().Next( 8 );
			switch ( choice ) {
				case 0:
					await Runner( null, "run_pipeline" ).ConfigureAwait( false );
					break;
				case 1:
					await Runner( null, "run_generator" ).ConfigureAwait( false );
					break;
				case 2:
					await Runner( null, "run_multiplexing" ).ConfigureAwait( false );
					break;
				case 3:
					await Runner( null, "run_demultiplexing" ).ConfigureAwait( false );
					break;
				case 4:
					await Runner( null, "run_timeout" ).ConfigureAwait( false );
					break;
				case 5:
					await Runner( null, "run_quit_channel" ).ConfigureAwait( false );
					break;
				case 6:
					await Runner( "run_basic_channel_usage" ).ConfigureAwait( false );
					break;
				case 7:
					await Runner( "web_search" ).ConfigureAwait( false );
					break;
			}
		}

		public static async Task RunBasicChannelUsage() {
			var ch = Channel.CreateUnbounded<String>();

			async Task func_consumer() {
				while ( await ch.Reader.WaitToReadAsync().ConfigureAwait( false ) ) {
					WriteLineWithTime( await ch.Reader.ReadAsync().ConfigureAwait( false ) );
				}
			}

			async Task func_producer() {
				for ( var i = 0; i < 5; i++ ) {
					await Task.Delay( TimeSpan.FromSeconds( Randem.Instance().Next( 3 ) ) ).ConfigureAwait( false );
					await ch.Writer.WriteAsync( $"Message {i}" ).ConfigureAwait( false );
				}

				ch.Writer.Complete();
			}

			await Task.WhenAll( func_consumer(), func_producer() ).ConfigureAwait( false );
		}

		public static async Task RunDemultiplexing() {
			var joe = await CreateMessenger( "Joe", 10 ).ConfigureAwait( false );
			var readers = Split( joe, 3 );
			var tasks = new List<Task>();

			for ( var i = 0; i < ( await readers.ConfigureAwait( false ) ).Count; i++ ) {
				var reader = ( await readers.ConfigureAwait( false ) )[i];
				var index = i;
				tasks.Add( Task.Run( async () => {
					await foreach ( var item in reader.ReadAllAsync() ) {
						WriteLineWithTime( $"Reader {index}: {item}" );
					}
				} ) );
			}

			await Task.WhenAll( tasks ).ConfigureAwait( false );
		}

		public static async Task RunGenerator( CancellationToken cancellationToken ) {
			var joe = await CreateMessenger( "Joe", 5 ).ConfigureAwait( false );

			await foreach ( var item in joe.ReadAllAsync( cancellationToken ) ) {
				WriteLineWithTime( item );
			}
		}

		public static async Task RunMultiplexing( CancellationToken cancellationToken ) {
			var ch = await Merge( await CreateMessenger( "Joe", 3 ).ConfigureAwait( false ), await CreateMessenger( "Ann", 5 ).ConfigureAwait( false ) ).ConfigureAwait( false );

			await foreach ( var item in ch.ReadAllAsync( cancellationToken ) ) {
				WriteLineWithTime( item );
			}
		}

		public static async Task Runner( String? region = null, String? session = null, String? package = null, String? project = null, String[]? args = null ) {
			var cancellationTokenSource = new CancellationTokenSource( TimeSpan.FromMinutes( 1 ) );
			var cancellationToken = cancellationTokenSource.Token;

			if ( !String.IsNullOrWhiteSpace( session ) ) {
				switch ( session ) {
					case "run_generator": {
							await RunGenerator( cancellationToken ).ConfigureAwait( false );
							break;
						}
					case "run_multiplexing": {
							await RunMultiplexing( cancellationToken ).ConfigureAwait( false );
							break;
						}
					case "run_demultiplexing": {
							await RunDemultiplexing().ConfigureAwait( false );
							break;
						}
					case "run_timeout": {
							await RunTimeout().ConfigureAwait( false );
							break;
						}
					case "run_quit_channel": {
							await RunQuitChannel().ConfigureAwait( false );
							break;
						}
					case "run_pipeline": {
							var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 5 ) );
							await RunPipeline( cts.Token ).ConfigureAwait( false );
							break;
						}
					default: {
							WriteLine( "Unrecognized session" );
							break;
						}
				}
			}
			else {
				switch ( region ) {
					case "run_basic_channel_usage":
						await RunBasicChannelUsage().ConfigureAwait( false );
						break;

					case "web_search":
						await RunWebSearch().ConfigureAwait( false );
						break;

					default:
						WriteLine( "Unrecognized region" );
						break;
				}
			}
		}

		public static async Task RunPipeline( CancellationToken cancellationToken ) {
			var sw = new Stopwatch();
			sw.Start();

			// Try with a large folder e.g. node_modules
			var fileSource = GetFilesRecursivelyAsync( ".", cancellationToken );
			var sourceCodeFiles = FilterByExtensionAsync( await fileSource.ConfigureAwait( false ), new HashSet<String> {
				".cs", ".json", ".xml", ".List"
			} );
			(var counter, var errors) = await GetLineCountAsync( await sourceCodeFiles.ConfigureAwait( false ), cancellationToken ).ConfigureAwait( false );

			// Distribute the file reading stage amongst several workers
			// var (counter, errors) = CountLinesAndMerge(Split(sourceCodeFiles, 5));

			var totalLines = 0;
			await foreach ( (var file, var lines) in counter.ReadAllAsync( cancellationToken ) ) {
				WriteLineWithTime( $"{file.FullName} {lines}" );
				totalLines += lines;
			}

			WriteLine( $"Total lines: {totalLines}" );

			await foreach ( var errMessage in errors.ReadAllAsync( cancellationToken ) ) {
				WriteLine( errMessage );
			}

			sw.Stop();
			WriteLine( sw.Elapsed.Simpler() );
		}

		public static async Task RunQuitChannel() {
			var cts = new CancellationTokenSource();
			var joe = await CreateMessenger( "Joe", 10, cts.Token ).ConfigureAwait( false );
			cts.CancelAfter( TimeSpan.FromSeconds( 5 ) );

			await foreach ( var item in joe.ReadAllAsync( cts.Token ) ) {
				WriteLineWithTime( item );
			}
		}

		public static async Task RunTimeout() {
			var joe = await CreateMessenger( "Joe", 10 ).ConfigureAwait( false );
			var cts = new CancellationTokenSource();
			cts.CancelAfter( TimeSpan.FromSeconds( 5 ) );

			try {
				await foreach ( var item in joe.ReadAllAsync( cts.Token ) ) {
					WriteLine( item );
				}

				WriteLine( "Joe sent all of his messages." );
			}
			catch ( OperationCanceledException ) {
				WriteLine( "Joe, you are too slow!" );
			}
		}

		public static async Task RunWebSearch() {
			var ch = Channel.CreateUnbounded<String>();

			async Task Search( String source, String terms, CancellationToken token ) {
				await Task.Delay( TimeSpan.FromSeconds( Randem.Instance().Next( 5 ) ), token ).ConfigureAwait( false );
				await ch.Writer.WriteAsync( $"Result from {source} for {terms}", token ).ConfigureAwait( false );
			}

			const String? term = "Jupyter";
			var token = new CancellationTokenSource( TimeSpan.FromSeconds( 10 ) ).Token;

			_ = Search( "Wikipedia", term, token );

			_ = Search( "Quora", term, token );

			_ = Search( "Everything2", term, token );

			try {
				for ( var i = 0; i < 3; i++ ) {
					WriteLine( await ch.Reader.ReadAsync( token ).ConfigureAwait( false ) );
				}

				WriteLine( "All searches have completed." );
			}
			catch ( OperationCanceledException ) {
				WriteLine( "Timeout." );
			}

			ch.Writer.Complete();
		}

	}

	public static class TimeExtensions {
		/// <summary>
		///     Display a <see cref="TimeSpan" /> in simpler terms. ie "2 hours 4 minutes 33 seconds".
		/// </summary>
		/// <param name="timeSpan"></param>
		public static String Simpler( this TimeSpan timeSpan ) {
			var sb = new StringBuilder();

			if ( timeSpan.Days > 365 * 2 ) {
				sb.AppendFormat( " {0:n0} years", timeSpan.Days / 365 );
			}

			else if ( timeSpan.Days is >= 365 and <= 366 ) {
				sb.Append( " 1 year" );
			}

			switch ( timeSpan.Days ) {
				//else if ( timeSpan.Days > 14 ) {
				//    sb.AppendFormat( " {0:n0} weeks", timeSpan.Days / 7 );
				//}
				//else if ( timeSpan.Days > 7 ) {
				//    sb.AppendFormat( " {0} week", timeSpan.Days / 7 );
				//}
				//else
				case > 1:
					sb.Append( $" {timeSpan.Days:R} days" );
					break;
				case 1:
					sb.Append( $" {timeSpan.Days:R} day" );
					break;
			}

			switch ( timeSpan.Hours ) {
				case > 1:
					sb.Append( $" {timeSpan.Hours:n0} hours" );
					break;
				case 1:
					sb.Append( $" {timeSpan.Hours} hour" );
					break;
			}

			switch ( timeSpan.Minutes ) {
				case > 1:
					sb.Append( $" {timeSpan.Minutes:n0} minutes" );
					break;
				case 1:
					sb.Append( $" {timeSpan.Minutes} minute" );
					break;
			}

			switch ( timeSpan.Seconds ) {
				case > 1:
					sb.Append( $" {timeSpan.Seconds:n0} seconds" );
					break;
				case 1:
					sb.Append( $" {timeSpan.Seconds} second" );
					break;
			}

			switch ( timeSpan.Milliseconds ) {
				case > 1:
					sb.Append( $" {timeSpan.Milliseconds:n0} milliseconds" );
					break;
				case 1:
					sb.Append( $" {timeSpan.Milliseconds} millisecond" );
					break;
			}

			if ( String.IsNullOrEmpty( sb.ToString().Trim() ) ) {
				sb.Append( " 0 milliseconds " );
			}

			return sb.ToString().Trim();
		}
	}

}