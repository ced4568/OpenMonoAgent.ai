using System.Text.Json;
using FluentAssertions;
using OpenMono.Acp;
using OpenMono.Permissions;
using OpenMono.Tools;
using Xunit;

namespace OpenMono.Tests.Acp;

public sealed class AcpToolSchemaIntersectionTests
{
    private static readonly string[] VsCodeClientTools =
    {
        "FileRead", "FileWrite", "FileEdit",
        "Bash", "Glob", "Grep", "ListDirectory",
        "Lsp", "ApplyPatch", "AskUser", "Todo",
        "EnterPlanMode", "ExitPlanMode",
        "WebFetch", "WebSearch",
    };

    [Fact]
    public void VsCode_profile_excludes_Roslyn_and_includes_TUI_internal()
    {
        var registry = MakeRegistryWith("FileRead", "Bash", "Roslyn", "Playbook", "MemorySave");

        var schema = AcpToolSchema.ForSession(registry, VsCodeClientTools);
        var names = schema.Select(t => t.Name).ToArray();

        names.Should().Contain("FileRead", because: "client declared it");
        names.Should().Contain("Bash", because: "client declared it");
        names.Should().NotContain("Roslyn", because: "not declared by VS Code profile");
        names.Should().Contain("Playbook", because: "TUI-internal always exposed");
        names.Should().Contain("MemorySave", because: "TUI-internal always exposed");
    }

    [Fact]
    public void Empty_clientTools_exposes_only_TUI_internal_tools()
    {
        var registry = MakeRegistryWith("FileRead", "Bash", "Roslyn", "Playbook", "MemorySave");

        var schema = AcpToolSchema.ForSession(registry, Array.Empty<string>());
        var names = schema.Select(t => t.Name).ToArray();

        names.Should().BeEquivalentTo("Playbook", "MemorySave");
    }

    [Fact]
    public void Unknown_clientTool_name_is_silently_ignored()
    {
        var registry = MakeRegistryWith("FileRead", "Bash", "Playbook");

        var schema = AcpToolSchema.ForSession(registry, new[] { "FileRead", "NotARealTool" });
        var names = schema.Select(t => t.Name).ToArray();

        names.Should().BeEquivalentTo("FileRead", "Playbook");
    }

    [Fact]
    public void Duplicate_clientTool_names_yield_a_single_entry_in_schema()
    {
        var registry = MakeRegistryWith("FileRead", "Bash", "Playbook");

        var schema = AcpToolSchema.ForSession(registry, new[] { "FileRead", "FileRead", "Bash", "Bash" });
        var names = schema.Select(t => t.Name).ToArray();

        names.Should().BeEquivalentTo("FileRead", "Bash", "Playbook");
        names.Length.Should().Be(names.Distinct().Count(), because: "results must be unique by tool name");
    }

    private static ToolRegistry MakeRegistryWith(params string[] names)
    {
        var r = new ToolRegistry();
        foreach (var n in names) r.Register(new StubTool(n));
        return r;
    }

    private sealed class StubTool(string name) : ITool
    {
        public string Name => name;
        public string Description => "stub";
        public bool IsConcurrencySafe => true;
        public bool IsReadOnly => true;
        public JsonElement InputSchema { get; } = JsonDocument.Parse("""{"type":"object"}""").RootElement.Clone();
        public PermissionLevel RequiredPermission(JsonElement input) => PermissionLevel.AutoAllow;
        public Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
            => Task.FromResult(ToolResult.Success("stub"));
    }
}
