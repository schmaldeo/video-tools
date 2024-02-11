using System.Diagnostics;
using System.Text;
using Spectre.Console;

namespace VideoTools;

static class Program
{
	private enum Options
	{
		Concatenate,
		Reformat,
	}
	public static async Task Main(string[] args)
	{
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
		while (paths.Count < 2)
		{
			var prompt = AnsiConsole.Prompt(
				new TextPrompt<string>("File path (empty if you want to stop adding):")
					.ValidationErrorMessage("[red]Invalid path[/]")
					.Validate(path =>
					{
						if (string.IsNullOrWhiteSpace(path))
						{
							return ValidationResult.Success();
						}

						return Path.Exists(path) && Path.HasExtension(path) ? ValidationResult.Success() : ValidationResult.Error();
					})
					.AllowEmpty()
			);
				
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

	// TODO add more formats
	private static readonly string[] VideoFormats = ["mp4", "mov", "avi", "mkv", "webm"];

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
		
		foreach (var (file, index) in files.Select((val, i) => (val, i)))
		{
			commandBuilder.Append($"-i {file} ");
			filterBuilder.Append($"[{index}:v:0][{index}:a:0]");
		}

		filterBuilder.Append($"concat=n={files.Count()}:v=1:a=1[outv][outa]\" ");
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
}