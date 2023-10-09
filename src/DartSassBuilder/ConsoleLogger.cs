﻿using Spectre.Console;

namespace DartSassBuilder;

public enum OutputLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical,
    None,
}

public class ConsoleLogger
{
    public ConsoleLogger(OutputLevel outputLevel = OutputLevel.Information)
    {
        OutputLevel = outputLevel;
    }

    private OutputLevel OutputLevel { get; }

    public void Trace(string line = "") => Log(OutputLevel.Trace, line);
    public void Debug(string line = "") => Log(OutputLevel.Debug, line);
    public void Information(string line = "") => Log(OutputLevel.Information, line);
    public void Warning(string line = "") => Log(OutputLevel.Warning, line);
    public void Error(string line = "") => Log(OutputLevel.Error, line);
    public void Critical(string line = "") => Log(OutputLevel.Critical, line);

    private void Log(OutputLevel level, string line = "")
    {
        var logColor = level switch
        {
            OutputLevel.Trace => "grey",
            OutputLevel.Debug => "blue",
            OutputLevel.Information => "green",
            OutputLevel.Warning => "yellow",
            OutputLevel.Error => "orange",
            OutputLevel.Critical => "red",
            OutputLevel.None => "white",
            _ => "green",
        };

        if (level >= OutputLevel)
        {
            AnsiConsole.MarkupLine($"[{logColor}]{level}[/]: {line}");
        }
    }

}