using System.Collections.Generic;
using System.Linq;
using GhostTrace.Core.Models;

namespace GhostTrace.Analysis;

public sealed class ScheduledTasksCorrelationEngine
{
    private const string TaskCachePrefix = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\";

    public IReadOnlyList<ScheduledTaskCorrelationFinding> Correlate(
        IEnumerable<ScanFinding> comFindings,
        IEnumerable<ScanFinding> registryFindings)
        => Correlate(comFindings, registryFindings, comCollectionDegraded: false, regCollectionDegraded: false);

    /// <summary>
    /// Correlates COM and TaskCache findings. When a source's collection was partial
    /// or failed (<paramref name="comCollectionDegraded"/> / <paramref name="regCollectionDegraded"/>),
    /// conclusions that depend on absence from that source are downgraded to warnings:
    /// a task missing from a partially-enumerated COM view may simply be missing
    /// coverage, not evidence of evasion.
    /// </summary>
    public IReadOnlyList<ScheduledTaskCorrelationFinding> Correlate(
        IEnumerable<ScanFinding> comFindings,
        IEnumerable<ScanFinding> registryFindings,
        bool comCollectionDegraded,
        bool regCollectionDegraded)
    {
        var comList = comFindings.ToList();
        var regList = registryFindings.ToList();
        
        var results = new List<ScheduledTaskCorrelationFinding>();

        // Fail-safe / Degraded trust: Se a coleta COM falhou miseravelmente, 
        // não podemos assumir que todas as chaves do registro são "GhostTasks".
        if (comList.Count == 0 && regList.Count > 0)
        {
            results.Add(new ScheduledTaskCorrelationFinding(
                LogicalPath: "<GLOBAL>",
                Label: "Inconclusive",
                Severity: CorrelationSeverity.Info,
                Reason: "COM findings are completely empty. Cannot safely correlate Ghost tasks.",
                ComSource: null,
                RegistrySource: null));
            return results;
        }

        // A partially-read registry tree means TaskCache entries may be missing
        // entirely — the correlation can still flag what it sees, but the overall
        // picture is incomplete and must be marked as such.
        if (regCollectionDegraded)
        {
            results.Add(new ScheduledTaskCorrelationFinding(
                LogicalPath: "<GLOBAL>",
                Label: "DegradedRegistryCoverage",
                Severity: CorrelationSeverity.Info,
                Reason: "Registry collection was partial/failed. TaskCache coverage is incomplete; absence of a finding does not prove absence of a task.",
                ComSource: null,
                RegistrySource: null));
        }

        var comMap = new Dictionary<string, ScanFinding>(StringComparer.OrdinalIgnoreCase);
        var comDuplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var finding in comList)
        {
            var path = NormalizeComPath(finding.Source);
            if (!comMap.TryAdd(path, finding))
            {
                comDuplicates.Add(path);
            }
        }

        var regMap = new Dictionary<string, ScanFinding>(StringComparer.OrdinalIgnoreCase);
        var regDuplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var finding in regList)
        {
            var path = NormalizeRegistryPath(finding.Source);
            if (!regMap.TryAdd(path, finding))
            {
                regDuplicates.Add(path);
            }
        }

        foreach (var kvp in regMap)
        {
            var logicalPath = kvp.Key;
            var regFinding = kvp.Value;

            if (regDuplicates.Contains(logicalPath) || comDuplicates.Contains(logicalPath))
            {
                results.Add(new ScheduledTaskCorrelationFinding(
                    LogicalPath: logicalPath,
                    Label: "Indeterminate",
                    Severity: CorrelationSeverity.Info,
                    Reason: "Duplicate paths found in either COM or Registry sources. Cannot safely correlate.",
                    ComSource: comMap.TryGetValue(logicalPath, out var dupCom) ? dupCom.RawValue : null,
                    RegistrySource: regFinding.RawValue));
                continue;
            }

            bool hasCom = comMap.TryGetValue(logicalPath, out var comFinding);
            var rawValue = regFinding.RawValue ?? "";
            
            bool missingSd = rawValue.Contains("MISSING_SD");
            bool zeroIndex = rawValue.Contains("MISSING_OR_ZERO_INDEX");
            bool anyAnomaly = rawValue.Contains("ANOMALIES:");

            // Conclusions built on "absent in COM" are only trustworthy when the COM
            // enumeration actually completed. With partial coverage they become warnings.
            const string degradedComNote = " WARNING: COM collection was partial/failed — absence from COM may be missing coverage, not evasion.";

            if (missingSd && !hasCom)
            {
                results.Add(new ScheduledTaskCorrelationFinding(
                    logicalPath,
                    comCollectionDegraded ? "GhostCandidate-DegradedEvidence" : "GhostCandidate",
                    comCollectionDegraded ? CorrelationSeverity.Low : CorrelationSeverity.High,
                    "Task is present in Registry with missing Security Descriptor, but absent in COM API." + (comCollectionDegraded ? degradedComNote : ""),
                    null,
                    regFinding.RawValue));
            }
            else if (zeroIndex && !hasCom)
            {
                results.Add(new ScheduledTaskCorrelationFinding(
                    logicalPath,
                    comCollectionDegraded ? "GhostCandidate-DegradedEvidence" : "GhostCandidate",
                    comCollectionDegraded ? CorrelationSeverity.Low : CorrelationSeverity.High,
                    "Task is present in Registry with missing or zero index, but absent in COM API." + (comCollectionDegraded ? degradedComNote : ""),
                    null,
                    regFinding.RawValue));
            }
            else if (anyAnomaly && hasCom)
            {
                // Direct registry observation, not an absence inference — only the
                // registry side's own degradation matters here, and a partially-read
                // tree cannot fabricate anomalies, so the conclusion stands.
                results.Add(new ScheduledTaskCorrelationFinding(
                    logicalPath,
                    "StructuralAnomaly",
                    CorrelationSeverity.Medium,
                    "Task has explicit anomalies in Registry, but COM API surprisingly still sees it.",
                    comFinding!.RawValue,
                    regFinding.RawValue));
            }
            else if (!hasCom && !anyAnomaly)
            {
                // Task exists in registry but not in COM, with no explicit corruption flag.
                // It might be orphaned, a leftover, or a cache desync. Not as severe as missing SD.
                results.Add(new ScheduledTaskCorrelationFinding(
                    logicalPath,
                    comCollectionDegraded ? "StructuralOnly-DegradedEvidence" : "StructuralOnly",
                    comCollectionDegraded ? CorrelationSeverity.Info : CorrelationSeverity.Medium,
                    "Task exists structurally in Registry but is completely absent from COM API. No explicit evasion anomalies found." + (comCollectionDegraded ? degradedComNote : ""),
                    null,
                    regFinding.RawValue));
            }
            // Tasks perfectly matching both sides without anomalies are ignored (implicit 'OK').
        }

        return results;
    }

    private static string NormalizeComPath(string source)
    {
        var path = source.Trim();
        if (!path.StartsWith("\\")) path = "\\" + path;
        return path.ToLowerInvariant();
    }

    private static string NormalizeRegistryPath(string source)
    {
        var path = source.Trim();
        if (path.StartsWith(TaskCachePrefix, StringComparison.OrdinalIgnoreCase))
        {
            path = path.Substring(TaskCachePrefix.Length);
        }
        if (!path.StartsWith("\\")) path = "\\" + path;
        return path.ToLowerInvariant();
    }
}
