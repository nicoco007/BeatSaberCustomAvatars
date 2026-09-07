using System.Collections.Generic;

namespace CustomAvatar.Tasks.Models;

internal class UnityAssemblyDefinition
{
    public string Name { get; set; }

    public string RootNamespace { get; set; } = string.Empty;

    public IEnumerable<string> References { get; set; } = [];

    public IEnumerable<string> IncludePlatforms { get; set; } = [];

    public IEnumerable<string> ExcludePlatforms { get; set; } = [];

    public bool AllowUnsafeCode { get; set; }

    public bool OverrideReferences { get; set; }

    public IEnumerable<string> PrecompiledReferences { get; set; } = [];

    public bool AutoReferenced { get; set; } = true;

    public IEnumerable<string> DefineConstraints { get; set; } = [];

    public IEnumerable<string> VersionDefines { get; set; } = [];

    public bool NoEngineReferences { get; set; }
}
