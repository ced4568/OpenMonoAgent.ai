using OpenMono.Tools;

namespace OpenMono.Acp;

public static class AcpToolSchema
{
    private static readonly HashSet<string> TuiInternal = new(StringComparer.Ordinal)
    {
        "Playbook", "MemorySave",
    };

    public static IReadOnlyList<ITool> ForSession(ToolRegistry registry, IReadOnlyList<string> clientTools)
    {
        var declared = new HashSet<string>(clientTools, StringComparer.Ordinal);
        var result = new List<ITool>();
        foreach (var t in registry.All)
        {
            if (declared.Contains(t.Name) || TuiInternal.Contains(t.Name))
                result.Add(t);
        }
        return result;
    }
}
