using System.Diagnostics;
using System.Xml.Linq;
using Xunit;

namespace AutoCutBlight.Tests;

public class ModFolderTests
{
    static readonly string Root = ModRoot.Path;

    static XElement Load(string relative) => XDocument.Load(Path.Combine(Root, relative)).Root;

    static bool InBuildOutput(string path) {
        var sep = Path.DirectorySeparatorChar;
        return path.Contains($"{sep}.git{sep}") || path.Contains($"{sep}bin{sep}") || path.Contains($"{sep}obj{sep}");
    }

    static string Git(string arguments) {
        var git = Process.Start(new ProcessStartInfo("git", arguments) {
            WorkingDirectory = Root, RedirectStandardOutput = true, RedirectStandardError = true,
        });
        var output = git.StandardOutput.ReadToEnd();
        var error = git.StandardError.ReadToEnd();
        git.WaitForExit();
        Assert.True(git.ExitCode == 0, "git " + arguments + " failed: " + error);
        return output.Trim();
    }

    [Fact]
    public void About_supports_exactly_1_6_and_1_7() {
        var about = Load("About/About.xml");
        Assert.Equal(new[] { "1.6", "1.7" }, about.Element("supportedVersions").Elements("li").Select(l => l.Value));
    }

    [Fact]
    public void About_has_its_own_package_id() {
        Assert.Equal("rimhunter.autocutblight", Load("About/About.xml").Element("packageId").Value);
    }

    [Fact]
    public void About_declares_no_dependency() {
        var about = Load("About/About.xml");
        Assert.Null(about.Element("modDependencies"));
        Assert.Null(about.Element("modDependenciesByVersion"));
    }

    [Fact]
    public void Load_folders_pick_the_matching_build_for_each_game_version() {
        var folders = Load("LoadFolders.xml");
        Assert.Equal(new[] { "/", "1.6" }, folders.Element("v1.6").Elements("li").Select(l => l.Value));
        Assert.Equal(new[] { "/", "1.7" }, folders.Element("v1.7").Elements("li").Select(l => l.Value));
    }

    [Fact]
    public void No_prebuilt_binary_is_committed() {
        Assert.Equal("", Git("ls-files -- \"*.dll\" \"*.pdb\" \"*.exe\""));
    }

    [Fact]
    public void The_mod_project_references_only_the_game() {
        var csproj = Load("Source/ModAutoCutBlight.csproj");
        Assert.Empty(csproj.Descendants("Reference"));
        var references = csproj.Descendants("PackageReference").Select(r => r.Attribute("Include").Value);
        Assert.Equal(new[] { "Krafs.Rimworld.Ref" }, references);
    }

    [Fact]
    public void Nothing_references_Harmony() {
        var offenders = Directory.GetFiles(Path.Combine(Root, "Source"), "*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".cs") || p.EndsWith(".csproj"))
            .Where(p => !InBuildOutput(p))
            .Where(p => File.ReadAllText(p).Contains("Harmony"))
            .ToList();
        Assert.Empty(offenders);
    }

    [Fact]
    public void About_lists_the_three_original_package_ids_as_incompatible() {
        var ids = Load("About/About.xml").Element("incompatibleWith").Elements("li").Select(l => l.Value.ToLowerInvariant()).OrderBy(v => v);
        Assert.Equal(new[] { "defi.autocutblight", "razor2.3.anotherrimworldmod.autocutblight", "tr.autocutblight" }, ids);
    }

    [Fact]
    public void No_Workshop_id_file_is_committed() {
        Assert.Equal("", Git("ls-files -- \"*PublishedFileId.txt\""));
    }

    [Fact]
    public void The_mod_has_no_settings_class() {
        var offenders = Directory.GetFiles(Path.Combine(Root, "Source"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !InBuildOutput(p))
            .Where(p => System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(p), @"ModSettings|SettingsCategory|DoSettingsWindowContents"))
            .ToList();
        Assert.Empty(offenders);
    }

    [Fact]
    public void The_work_giver_is_an_emergency_giver_in_plant_cutting_served_by_our_class() {
        var giver = Load("Defs/WorkGiverDef/WorkGivers.xml").Elements("WorkGiverDef").Single();
        Assert.Equal("AcbCutBlight", giver.Element("defName").Value);
        Assert.Equal("PlantCutting", giver.Element("workType").Value);
        Assert.Equal("true", giver.Element("emergency").Value);
        Assert.Equal("Rimhunter.AutoCutBlight.WorkGiver_CutBlight", giver.Element("giverClass").Value);
        Assert.Equal("false", giver.Element("directOrderable").Value);
        Assert.Equal("10", giver.Element("priorityInType").Value);
    }
}
