using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace VideoTools;

public static class Ffmpeg
{
	private static readonly string ExePath = Path.GetDirectoryName(Environment.ProcessPath) ?? ".";

	/// <summary>
	///     Concatenates two or more videos of any supported format into one of any supported format.
	/// </summary>
	/// <param name="files">
	///     <see cref="IEnumerable{T}">IEnumerable</see> of <see cref="FileInfo">FileInfo</see>'s to be
	///     concatenated. It is recommended that you use a collection that guarantees order as the function just uses a
	///     <c>foreach</c> loop
	/// </param>
	/// <param name="output">Output filename (must include extension)</param>
	public static async Task Concat(IEnumerable<FileInfo> files, string output)
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

	/// <summary>
	///     Converts a file from one format to another.
	/// </summary>
	/// <param name="file"><see cref="FileInfo">FileInfo</see> of the file to be converted</param>
	/// <param name="outputFormat">Filename extension of the output</param>
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

	/// <summary>
	///     Extracts audio in MP3 format out of a video.
	/// </summary>
	/// <param name="file"><c>FileInfo</c> of the video</param>
	public static async Task ExtractAudio(FileInfo file)
	{
		var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "./ffmpeg.exe",
				Arguments = $"-i {file.FullName} {Path.GetFileNameWithoutExtension(file.FullName)}.mp3"
			}
		};
		process.Start();
		await process.WaitForExitAsync();
	}

	/// <summary>
	///     Removes audio from a video file and saves it in the same directory, with the same extension and <c>-nosound</c>
	///     added to its name.
	/// </summary>
	/// <param name="file"><see cref="FileInfo">FileInfo</see> of the file on which you want to perform the operation</param>
	public static async Task RemoveAudio(FileInfo file)
	{
		var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "./ffmpeg.exe",
				Arguments =
					$"-i {file.FullName} -an {Path.GetFileNameWithoutExtension(file.FullName)}-nosound{Path.GetExtension(file.FullName)}"
			}
		};
		process.Start();
		await process.WaitForExitAsync();
	}

	/// <summary>
	///     Checks if ffmpeg is present in the process' directory.
	/// </summary>
	/// <returns>A boolean indicating whether ffmpeg exists in process' directory</returns>
	public static bool IsInstalled()
	{
		return File.Exists($"{ExePath}/ffmpeg.exe") && File.Exists($"{ExePath}/ffprobe.exe");
	}

	/// <summary>
	///     Downloads ffmpeg from github, and unzips the executables into process' directory.
	/// </summary>
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

	/// <summary>
	///     Gets a list of ffmpeg's supported formats from <c>ffmpeg -formats</c> command
	/// </summary>
	/// <returns>A list of formats supported by ffmpeg</returns>
	public static async Task<List<string>> GetSupportedFormats()
	{
		var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "./ffmpeg.exe",
				Arguments = "-formats",
				RedirectStandardOutput = true,
				CreateNoWindow = true,
				UseShellExecute = false
			}
		};
		process.Start();
		// skip first few lines as they are just explaining what means what
		for (var i = 0; i < 4; i++) await process.StandardOutput.ReadLineAsync();

		List<string> formats = [];
		while (!process.StandardOutput.EndOfStream)
		{
			var line = await process.StandardOutput.ReadLineAsync();
			var split = line!.Split();
			// formatting this output is really funky, maybe there's a better way to do it
			formats.Add(split[2] == "E" ? split[3] : split[2]);
		}

		await process.WaitForExitAsync();
		return formats;
	}
}