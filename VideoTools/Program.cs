﻿using System.ComponentModel;
using EnumsNET;
using Spectre.Console;

namespace VideoTools;

internal static class Program
{
	public static async Task Main()
	{
		// checks if ffmpeg is present in installation directory and if not, downloads it
		if (!Ffmpeg.IsInstalled())
			await AnsiConsole.Status()
				.StartAsync("ffmpeg not found, installing...", ctx =>
				{
					ctx.Spinner(Spinner.Known.Aesthetic);
					ctx.SpinnerStyle(Style.Parse("green"));
					return Ffmpeg.Download();
				});

		// initial prompt
		var selection = AnsiConsole.Prompt(
			new SelectionPrompt<Options>
				{
					// displays enum member's Description attribute or its name if description is not present
					Converter = option => option.AsString(EnumFormat.Description) ?? option.AsString()
				}
				.Title("What do you want to do?")
				.AddChoices([Options.Concatenate, Options.Reformat, Options.ExtractAudio, Options.RemoveAudio, Options.CreateGif])
		);

		switch (selection)
		{
			case Options.Concatenate:
				await HandleConcatenate();
				break;
			case Options.Reformat:
				await HandleChangeFormat();
				break;
			case Options.ExtractAudio:
				await HandleExtractAudio();
				break;
			case Options.RemoveAudio:
				await HandleRemoveAudio();
				break;
			case Options.CreateGif:
				await HandleCreateGif();
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

		await Ffmpeg.Concat(paths, outputFileName);
	}

	private static async Task HandleChangeFormat()
	{
		var file = GetFileFromConsole();

		var formats = await Ffmpeg.GetSupportedFormats();
		var extension = AnsiConsole.Prompt(
			new TextPrompt<string>("File format:")
				.ValidationErrorMessage("[red]Invalid file format[/]")
				.Validate(input => formats.Any(x => x == input.Trim()) && input.Trim() != string.Empty)
		);

		await Ffmpeg.ChangeFormat(file, extension);
	}

	private static async Task HandleExtractAudio()
	{
		var file = GetFileFromConsole();

		await Ffmpeg.ExtractAudio(file);
	}

	private static async Task HandleRemoveAudio()
	{
		var file = GetFileFromConsole();

		await Ffmpeg.RemoveAudio(file);
	}

	private static async Task HandleCreateGif()
	{
		var file = GetFileFromConsole();

		await Ffmpeg.ChangeFormat(file, "gif");
	}

	/// <summary>
	///     Prompts a user for filename with validation.
	/// </summary>
	/// <returns><see cref="FileInfo">FileInfo</see> of the file user entered</returns>
	private static FileInfo GetFileFromConsole()
	{
		var fileName = AnsiConsole.Prompt(
			new TextPrompt<string>("File path:")
				.ValidationErrorMessage("[red]Invalid path[/]")
				.Validate(path => Path.Exists(path) && Path.HasExtension(path)
					? ValidationResult.Success()
					: ValidationResult.Error())
		);
		return new FileInfo(fileName);
	}

	// it's best that each member has a description attribute, this way the option is displayed in a user-friendly manner
	private enum Options
	{
		[Description("Concatenate files")] Concatenate,
		[Description("Change format")] Reformat,
		[Description("Extract audio to mp3")] ExtractAudio,

		[Description("Remove audio from a video")]
		RemoveAudio,
		[Description("Create a GIF")]
		CreateGif
	}
}