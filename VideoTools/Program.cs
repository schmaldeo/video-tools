﻿using Spectre.Console;

namespace VideoTools;

internal static class Program
{
	// TODO add more formats
	private static readonly string[] VideoFormats = ["mp4", "mov", "avi", "mkv", "webm"];

	public static async Task Main()
	{
		// checks if ffmpeg is present in installation directory and if not, downloads it
		if (!Ffmpeg.IsInstalled())
		{
			await AnsiConsole.Status()
				.StartAsync("ffmpeg not found, installing...", ctx => 
				{
					ctx.Spinner(Spinner.Known.Aesthetic);
					ctx.SpinnerStyle(Style.Parse("green"));
					return Ffmpeg.Download();
				});
		}
		
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

		await Ffmpeg.Concatenate(paths, outputFileName);
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

		var formats = await Ffmpeg.GetSupportedFormats();
		var extension = AnsiConsole.Prompt(
			new TextPrompt<string>("File format:")
				.ValidationErrorMessage("[red]Invalid file format[/]")
				.Validate(input => formats.Any(x => x == input.Trim()))
		);

		await Ffmpeg.ChangeFormat(new FileInfo(fileName), extension);
	}

	private enum Options
	{
		Concatenate,
		Reformat
	}
}