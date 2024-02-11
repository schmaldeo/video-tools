using System.Diagnostics;
using System.Text;

namespace VideoTools;

static class Program
{
	public static async Task Main(string[] args)
	{
		try
		{
			var files = ParseFileInput(args).ToArray();
			// await Concatenate(files, "loooo8.mp4");
			await ChangeFormat(new FileInfo("./vid1.mp4"), "mp5");
		}
		catch (ArgumentException e)
		{
			await Console.Error.WriteLineAsync(e.Message);
			Environment.Exit(1);
		}
	}

	private static async Task Concatenate(FileInfo[] files, string output)
	{
		StringBuilder commandBuilder = new();
		StringBuilder filterBuilder = new("-filter_complex \"");
		
		foreach (var (file, index) in files.Select((val, i) => (val, i)))
		{
			commandBuilder.Append($"-i {file} ");
			filterBuilder.Append($"[{index}:v:0][{index}:a:0]");
		}

		filterBuilder.Append($"concat=n={files.Length}:v=1:a=1[outv][outa]\" ");
		commandBuilder.Append(filterBuilder);
		commandBuilder.Append($"-map \"[outv]\" -map \"[outa]\" {output}");

		Console.WriteLine(commandBuilder.ToString());
		
		var process = new Process 
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "./ffmpeg.exe",
				Arguments = commandBuilder.ToString()
			}
		};
		process.Start();
		await process.WaitForExitAsync();
	}

	private static async Task ChangeFormat(FileInfo file, string outputFormat)
	{
		var process = new Process 
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "./ffmpeg.exe",
				Arguments = $"-i {file.FullName} {Path.GetFileNameWithoutExtension(file.FullName)}.{outputFormat}"
			}
		};
		process.Start();
		await process.WaitForExitAsync();
	}

	private static IEnumerable<FileInfo> ParseFileInput(IEnumerable<string> input)
	{
		return input.Select(file =>
		{
			if (Path.Exists(file))
			{
				return new FileInfo(file);
			}

			throw new ArgumentException($"File {file} not found");
		});
	}
}