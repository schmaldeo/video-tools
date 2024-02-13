using System.Diagnostics;
using System.Text;
using System.IO.Compression;

namespace VideoTools;

public class Ffmpeg
{
	private static readonly string ExePath = Path.GetDirectoryName(Environment.ProcessPath) ?? ".";
	
	public static async Task Concatenate(IEnumerable<FileInfo> files, string output)
	{
		StringBuilder commandBuilder = new();
		StringBuilder filterBuilder = new("-filter_complex \"");

		var index = 0;
		// want to get a string like this:
		// ffmpeg -i input1.mp4 -i input2.webm -i input3.mov \
		// -filter_complex "[0:v:0][0:a:0][1:v:0][1:a:0][2:v:0][2:a:0]concat=n=3:v=1:a=1[outv][outa]" \
		// -map "[outv]" -map "[outa]" output.mkv
		// with variables being amount of -i's and the filter
		foreach (var file in files)
		{
			commandBuilder.Append($"-i {file} ");
			filterBuilder.Append($"[{index}:v:0][{index}:a:0]");
			index++;
		}

		filterBuilder.Append($"concat=n={index}:v=1:a=1[outv][outa]\" ");
		commandBuilder.Append(filterBuilder);
		commandBuilder.Append($"-map \"[outv]\" -map \"[outa]\" {output}");

		var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "./ffmpeg.exe",
				Arguments = commandBuilder.ToString()
			}
		};
		Console.WriteLine(commandBuilder.ToString());
		process.Start();
		await process.WaitForExitAsync();
	}

	public static async Task ChangeFormat(FileInfo file, string outputFormat)
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

	public static bool IsInstalled()
	{
		return File.Exists($"{ExePath}/ffmpeg.exe") && File.Exists($"{ExePath}/ffprobe.exe");
	}

	public static async Task Download()
	{
		HttpClient httpClient = new();
		var uri = new Uri(
			"https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip");
		
		var response = await httpClient.GetAsync(uri);

		// downloading zipped ffmpeg
		var zipPath = $"{ExePath}/ffmpeg.zip";
		var fs = new FileStream(zipPath, FileMode.CreateNew);
		await Task.Run(() => response.Content.CopyToAsync(fs))
			.ContinueWith(_ =>
			{
				response.Dispose();
				fs.Dispose();
				ZipFile.ExtractToDirectory(zipPath, ExePath);
			});
		
		// moving ffmpeg executables to the root of the application
		var unzippedPath = $"{ExePath}/ffmpeg-master-latest-win64-gpl";
		var files = Directory.GetFiles($"{unzippedPath}/bin");
		foreach (var file in files)
		{
			var fileName = Path.GetFileName(file);
			File.Move(file, $"{ExePath}/{fileName}");
		}
		
		// clean up
		Directory.Delete(unzippedPath, true);
		File.Delete(zipPath);
	}

	
	public static async Task<List<string>> GetSupportedFormats()
	{
		var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "./ffmpeg.exe",
				Arguments = $"-formats",
				RedirectStandardOutput = true,
				CreateNoWindow = true,
				UseShellExecute = false
			}
		};
		process.Start();
		// skip first few lines as they are just explaining what means what
		for (int i = 0; i < 4; i++)
		{
			await process.StandardOutput.ReadLineAsync();
		}

		List<string> formats = [];
		while (!process.StandardOutput.EndOfStream)
		{
			var line = await process.StandardOutput.ReadLineAsync();
			var split = line!.Split();
			formats.Add(split[3]);
		}
		await process.WaitForExitAsync();
		return formats;
	}
}