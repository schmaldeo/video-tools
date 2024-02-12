using System.Diagnostics;
using System.Text;
using Spectre.Console;

namespace VideoTools;

internal static class Program
{
	// TODO download ffmpeg if not present
	// TODO add more formats
	private static readonly string[] VideoFormats = ["mp4", "mov", "avi", "mkv", "webm"];

	public static async Task Main()
	{
		// initial prompt
		var selection = AnsiConsole.Prompt(
			new SelectionPrompt<Options>()
				.Title("What do you want to do?")
				.AddChoices([Options.Concatenate, Options.Reformat])
		);

		switch (selection)
		{
			case Options.Concatenate:
				await HandleConcatenate();
				break;
			case Options.Reformat:
				await HandleChangeFormat();
				break;
		}
	}

	private static async Task HandleConcatenate()
	{
		List<FileInfo> paths = [];
		// prompt repeatedly asking for files to concatenate until there's at least 2 of them
		Console.WriteLine("Please enter a path to the file you want to add or press enter if you want to stop adding.");
		while (true)
		{
			var prompt = AnsiConsole.Prompt(
				new TextPrompt<string>("File path:")
					.ValidationErrorMessage("[red]Invalid path[/]")
					.Validate(path =>
					{
						if (string.IsNullOrWhiteSpace(path)) return ValidationResult.Success();

						return Path.Exists(path) && Path.HasExtension(path) ? ValidationResult.Success() : ValidationResult.Error();
					})
					.AllowEmpty()
			);

			// if nothing was entered end the prompt unless there's less than 2 files
			if (string.IsNullOrWhiteSpace(prompt))
			{
				if (paths.Count < 2)
				{
					Console.WriteLine("You need to enter at least 2 files.");
					continue;
				}

				break;
			}

			paths.Add(new FileInfo(prompt));
		}

		var outputFileName = AnsiConsole.Prompt(
			new TextPrompt<string>("Output file name:"));

		await Concatenate(paths, outputFileName);
	}

	private static async Task HandleChangeFormat()
	{
		var fileName = AnsiConsole.Prompt(
			new TextPrompt<string>("File path:")
				.ValidationErrorMessage("[red]Invalid path[/]")
				.Validate(path => Path.Exists(path) && Path.HasExtension(path)
					? ValidationResult.Success()
					: ValidationResult.Error())
		);

		var extension = AnsiConsole.Prompt(
			new TextPrompt<string>("File format:")
				.ValidationErrorMessage("[red]Invalid file format[/]")
				.Validate(input => VideoFormats.Any(format => format == input))
		);

		await ChangeFormat(new FileInfo(fileName), extension);
	}

	private static async Task Concatenate(IEnumerable<FileInfo> files, string output)
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

	private enum Options
	{
		Concatenate,
		Reformat
	}
}