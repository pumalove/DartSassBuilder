using DartSassHost.Helpers;

using JavaScriptEngineSwitcher.V8;

using Spectre.Console;

namespace DartSassBuilder;

public class Compiler
{
    public Compiler(ConsoleLogger? logger = null)
    {
        Logger = logger ?? new ConsoleLogger();
    }

    private ConsoleLogger Logger { get; }

    public async Task Compile(GenericOptions options)
    {
        var compileTask = options switch
        {
            DirectoryOptions dir => CompileDirectoriesAsync(dir),
            FilesOptions files => CompileFilesAsync(files),
            _ => throw new NotImplementedException("Invalid commandline option parsing"),
        };

        await compileTask;

        Logger.Default("Sass operation completed.");
    }

    private async Task CompileFilesAsync(IEnumerable<string> sassFiles, CompilationOptions compilationOptions)
    {
        Logger.Default(line: "Sass compile files");

        try
        {
            using var sassCompiler = new SassCompiler(() => new V8JsEngineFactory().CreateEngine());

            async Task CompileFile(string file, SassCompiler compiler)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.Name.StartsWith('_'))
                {
                    Logger.Debug($"Skipping: {fileInfo.FullName}");
                    return;
                }

                Logger.Debug($"Processing: {fileInfo.FullName}");

                var result = compiler.CompileFile(file, options: compilationOptions);

                var newFile = fileInfo.FullName.Replace(fileInfo.Extension, ".css");

                if (File.Exists(newFile)
                    && result.CompiledContent.ReplaceLineEndings() == (await File.ReadAllTextAsync(newFile)).ReplaceLineEndings())
                {
                    return;
                }

                await File.WriteAllTextAsync(newFile, result.CompiledContent);
            }

            await Parallel.ForEachAsync(sassFiles, async (file, _)
                => await CompileFile(file, sassCompiler));
        }
        catch (SassCompilerLoadException e)
        {
            Logger.Error(line: "During loading of Sass compiler an error occurred. See details:");
            Logger.Error();
            Logger.Error(line: SassErrorHelpers.GenerateErrorDetails(e));
        }
        catch (SassCompilationException e)
        {
            Logger.Error(line: "During compilation of SCSS code an error occurred. See details:");
            Logger.Error();
            Logger.Error(line: SassErrorHelpers.GenerateErrorDetails(e));
        }
        catch (SassException e)
        {
            Logger.Error(line: "During working of Sass compiler an unknown error occurred. See details:");
            Logger.Error();
            Logger.Error(line: SassErrorHelpers.GenerateErrorDetails(e));
        }
        catch (Exception e)
        {
            Logger.Error(line: "Unknown exception during compilation");
            Logger.Error();
            AnsiConsole.WriteException(e);
        }
    }

    private Task CompileFilesAsync(FilesOptions options)
        => CompileFilesAsync(options.Files,
                              options.SassCompilationOptions);

    private async Task CompileDirectoriesAsync(DirectoryOptions options, string? directory = null)
    {
        var isSubdirectory = directory is not null;

        directory ??= options.Directory;

        if ((isSubdirectory && options.IsExcluded(directory))
            || !ContainsSass(directory))
        {
            Logger.Debug($"Skipped directory {directory}");
            return;
        }

        Logger.Default(line: $"Sass compile directory: {directory}");

        var sassFiles =
            Directory.EnumerateFiles(directory)
                     .Where(file => Path.GetExtension(file).ToLower() is ".scss" or ".sass");

        await CompileFilesAsync(sassFiles, options.SassCompilationOptions);

        foreach (var subDirectory in Directory.EnumerateDirectories(directory)
                                                    .Where(ContainsSass))
        {
            await CompileDirectoriesAsync(options, subDirectory);
        }
    }

    private static bool ContainsSass(string directory)
        => Directory.EnumerateFiles(directory,
                                     "*.s*ss",
                                     SearchOption.AllDirectories)
                    .Any();
}