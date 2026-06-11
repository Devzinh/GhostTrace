using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GhostTrace.Core.Abstractions;
using GhostTrace.Core.Enums;

namespace GhostTrace.Analysis;

public sealed class ScheduledTasksCorrelationComposer
{
    private readonly ScheduledTasksCorrelationEngine _engine;

    public ScheduledTasksCorrelationComposer(ScheduledTasksCorrelationEngine? engine = null)
    {
        _engine = engine ?? new ScheduledTasksCorrelationEngine();
    }

    public ScheduledTasksCorrelationResult Compose(IScanResult comResult, IScanResult registryResult)
    {
        var warnings = new List<string>();

        // Inspect the collection health of both sources before correlating. A module
        // that failed or only partially enumerated cannot support absence-based
        // conclusions ("registry entry with no COM counterpart = ghost task").
        bool comFailed = comResult.Status == ScanStatus.Failure;
        bool regFailed = registryResult.Status == ScanStatus.Failure;
        bool comDegraded = comFailed || comResult.Status == ScanStatus.PartialSuccess
                           || comResult.Errors.Count > 0 || HasCollectionErrors(comResult, "TotalWithError");
        bool regDegraded = regFailed || registryResult.Status == ScanStatus.PartialSuccess
                           || registryResult.Errors.Count > 0;

        if (comDegraded)
        {
            warnings.Add($"COM collection is degraded (Status: {comResult.Status}, Errors: {comResult.Errors.Count}). Absence-based conclusions are downgraded to warnings.");
        }
        if (regDegraded)
        {
            warnings.Add($"Registry collection is degraded (Status: {registryResult.Status}, Errors: {registryResult.Errors.Count}). TaskCache coverage may be incomplete.");
        }

        // A fully failed source makes the correlation meaningless — report an overall
        // inconclusive result instead of labelling missing coverage as anomalies.
        if (comFailed || regFailed)
        {
            string failedSide = comFailed && regFailed ? "COM and Registry" : comFailed ? "COM" : "Registry";
            warnings.Add($"{failedSide} collection failed entirely. Correlation is inconclusive.");

            var inconclusive = new List<ScheduledTaskCorrelationFinding>
            {
                new(
                    LogicalPath: "<GLOBAL>",
                    Label: "Inconclusive",
                    Severity: CorrelationSeverity.Info,
                    Reason: $"{failedSide} collection failed entirely. Cannot safely correlate Ghost tasks.",
                    ComSource: null,
                    RegistrySource: null)
            };
            return BuildResult(inconclusive, warnings);
        }

        if (comResult.Findings.Count == 0)
        {
            warnings.Add("COM findings are empty. Correlation will be globally inconclusive or degraded.");
        }
        
        if (registryResult.Findings.Count == 0)
        {
            warnings.Add("Registry findings are empty. Correlation is degraded.");
        }

        // Foca explicitamente nas categorias esperadas para Scheduled Tasks
        var comFindings = comResult.Findings.Where(f => f.Category == "ScheduledTask").ToList();
        var regFindings = registryResult.Findings.Where(f => f.Category == "ScheduledTaskCacheEntry").ToList();

        if (comResult.Findings.Count > 0 && comFindings.Count == 0)
        {
            warnings.Add($"Filtered out {comResult.Findings.Count} findings from COM result because Category was not 'ScheduledTask'.");
        }

        if (registryResult.Findings.Count > 0 && regFindings.Count == 0)
        {
            warnings.Add($"Filtered out {registryResult.Findings.Count} findings from Registry result because Category was not 'ScheduledTaskCacheEntry'.");
        }

        var correlatedFindings = _engine.Correlate(comFindings, regFindings, comDegraded, regDegraded);

        return BuildResult(correlatedFindings, warnings);
    }

    /// <summary>True when a known per-item error counter in the module metadata is non-zero.</summary>
    private static bool HasCollectionErrors(IScanResult result, string counterKey)
    {
        return result.Metadata.TryGetValue(counterKey, out var raw)
               && int.TryParse(raw, out var count)
               && count > 0;
    }

    private static ScheduledTasksCorrelationResult BuildResult(
        IReadOnlyList<ScheduledTaskCorrelationFinding> correlatedFindings,
        List<string> warnings)
    {
        // Agregando metadados de inteligência
        var metadata = new Dictionary<string, string>();
        metadata["TotalCorrelations"] = correlatedFindings.Count.ToString();
        
        var bySeverity = correlatedFindings.GroupBy(x => x.Severity).ToDictionary(g => g.Key, g => g.Count());
        foreach (var sev in Enum.GetValues<CorrelationSeverity>())
        {
            metadata[$"Severity_{sev}"] = bySeverity.GetValueOrDefault(sev, 0).ToString();
        }

        var byLabel = correlatedFindings.GroupBy(x => x.Label).ToDictionary(g => g.Key, g => g.Count());
        foreach (var kvp in byLabel)
        {
            metadata[$"Label_{kvp.Key}"] = kvp.Value.ToString();
        }

        return new ScheduledTasksCorrelationResult(
            ComponentName: "ScheduledTasksCorrelationComposer",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            CorrelatedFindings: correlatedFindings,
            Metadata: new ReadOnlyDictionary<string, string>(metadata),
            Warnings: warnings.AsReadOnly()
        );
    }
}
