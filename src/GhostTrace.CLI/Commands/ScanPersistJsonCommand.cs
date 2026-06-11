using System.CommandLine;
using System.CommandLine.Invocation;
using System.Runtime.Versioning;
using GhostTrace.Modules.Persistence;
using GhostTrace.CLI.Runtime;

namespace GhostTrace.CLI.Commands;

/// <summary>
/// A CLI command to execute the PersistenceScanModule and output a JSON report.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ScanPersistJsonCommand : Command
{
    public ScanPersistJsonCommand()
        : base("scan-persist-json", "Runs a basic persistence scan (Run keys, Startup folders) and outputs a JSON report.")
    {
        var outputArgument = new Argument<FileInfo>(
            name: "outputPath",
            description: "The path to the output JSON file.");

        AddArgument(outputArgument);

        this.SetHandler(async (InvocationContext context) =>
        {
            var outputInfo = context.ParseResult.GetValueForArgument(outputArgument)!;
            context.ExitCode = await ExecuteAsync(outputInfo);
        });
    }

    private async Task<int> ExecuteAsync(FileInfo outputInfo)
    {
        Console.WriteLine("[INFO] Starting Persistence scan...");
        Console.WriteLine("[INFO] Targets:     HKCU & HKLM Run/RunOnce keys, User & Common Startup folders");
        Console.WriteLine($"[INFO] Output File: {outputInfo.FullName}");

        bool ok = await SingleModuleJsonRunner.RunAsync(
            new PersistenceScanModule(), "Persistence Scan Report", outputInfo);
        return ok ? 0 : 1;
    }
}
