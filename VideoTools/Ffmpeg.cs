using System.Diagnostics;
using System.Text;

namespace VideoTools;

public class Ffmpeg
{
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
}