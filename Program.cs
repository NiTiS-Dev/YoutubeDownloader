using CommandLine;
using NiTiS.IO;
using System;
using System.Linq;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using static System.Console;

namespace YoutubeDownloader;

public class Program
{
	public static readonly Version Version = new(1, 0, 0);
	public static void Main(string[] args)
	{
		Parser.Default.ParseArguments<Options>(args)
			.WithParsed(Main)
			;
	}
	public static void Main(Options args)
	{
		string executorFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
		File executorFile = new(string.IsNullOrWhiteSpace(executorFilePath) ? "ytdownloader" : executorFilePath);

		if (string.IsNullOrWhiteSpace(args.Link))
		{
			WriteError($"Required video link\n{executorFile.Name} -l [youtube video link]");
			Environment.Exit(-101);
		}
#if DEBUG
		WriteLine(executorFile);
		WriteLine(args.Link);
		WriteLine(args.OutputPath);
#endif

		WriteLine("Access to youtube video...");
		YoutubeClient youTube = new();

		Video video = youTube.Videos.GetAsync(args.Link).Result;
		WriteGreen("Founded video:\n" + video.Title);

		WriteLine("Getting video stream...");
		StreamManifest streamManifest = youTube.Videos.Streams.GetManifestAsync(args.Link).Result;

		try
		{
			IProgress<double> progress = new ConsoleProgress();

			IStreamInfo audioStreamInfo = streamManifest.GetAudioStreams().GetWithHighestBitrate();
			IVideoStreamInfo videoStreamInfo = string.IsNullOrWhiteSpace(args.Resolution)
				? streamManifest.GetVideoStreams().GetWithHighestVideoQuality()
				: streamManifest.GetVideoStreams().FirstOrDefault(s => s.VideoQuality.Label == args.Resolution);

			IStreamInfo[] streamInfos = new IStreamInfo[] { audioStreamInfo, videoStreamInfo };

			File outputFile = string.IsNullOrWhiteSpace(args.OutputPath)
				? new File($"./video.{videoStreamInfo.Container}")
				: new File(args.OutputPath.Replace("{{EXT}}", videoStreamInfo.Container.ToString()));

			youTube.Videos.DownloadAsync(video.Id, outputFile.Path, (o) =>
			{
				o.SetContainer(Container.WebM)
				 .SetPreset(ConversionPreset.Fast);

				if (!string.IsNullOrWhiteSpace(args.FFMPEG))
					o.SetFFmpegPath(args.FFMPEG);

			}, progress).GetAwaiter().GetResult();
		}
		catch (Exception ex)
		{
			if (ex.Message.ToLower().Contains("ffmpeg"))
			{
				WriteError("\rffmpeg required to be install >_<\nIf you allready installed ffmpeg try to restart youre system");
				Environment.Exit(-102);
			}
			else
			{
				WriteError("\rException: " + ex.GetType().FullName + "\n" + ex.Message);
				Environment.Exit(-100);
			}
		}
		
		WriteGreen("\rVideo Downloaded              ");
	}
	private class ConsoleProgress : IProgress<double>
	{
		public void Report(double value)
		{
			const int WIDE = 30;
			int full = (int)(WIDE * value);
			int blank = WIDE - full;

			Write("\r" + string.Concat(Enumerable.Repeat("█", full)) + string.Concat(Enumerable.Repeat("▒", blank)));

			if (value == 1f)
			{
				Write("\r" + string.Concat(Enumerable.Repeat(" ", WIDE)) + "\n");
			}
		}
	}
	public static void WriteGreen(string text)
	{
		ConsoleColor _temp2 = ForegroundColor;
		ForegroundColor = ConsoleColor.Green;
		WriteLine(text);
		ForegroundColor = _temp2;
	}
	public static void WriteError(string error)
	{
		ConsoleColor _temp2 = ForegroundColor;
		ForegroundColor = ConsoleColor.Red;
		WriteLine(error);
		ForegroundColor = _temp2;
	}
}
public class Options
{
	[Option('o', "output", Required = false, HelpText = "Path to output directory")]
	public string OutputPath { get; set; } = string.Empty;
	[Option('l', "link", Required = false, HelpText = "Link to youtube video")]
	public string Link { get; set; } = string.Empty;
	[Option('r', "resolution", Required = false, HelpText = "Video resolution")]
	public string Resolution { get; set; } = string.Empty;
	[Option("ffmpeg", Required = false, HelpText = "Path to ffmpeg")]
	public string FFMPEG { get; set; } = string.Empty;
}