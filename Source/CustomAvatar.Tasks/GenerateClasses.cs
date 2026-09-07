using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace CustomAvatar.Tasks;

public class GenerateClasses : Task
{
    [Required]
    public string AssemblyFile { get; set; }

    [Required]
    public ITaskItem[] Types { get; set; }

    [Required]
    public ITaskItem[] SearchDirectories { get; set; }

    [Required]
    public string OutputDirectory { get; set; }

    public override bool Execute()
    {
        try
        {
            new ClassGenerator(AssemblyFile, SearchDirectories.Select(sd => sd.ItemSpec))
                .Generate(Types.Select(t => new ClassGenerator.TargetType(t.ItemSpec, t.GetMetadata("AssemblyName"))), OutputDirectory);
        }
        catch (Exception ex)
        {
            Log.LogError(ex.ToString());
            return false;
        }
        return true;
    }
}