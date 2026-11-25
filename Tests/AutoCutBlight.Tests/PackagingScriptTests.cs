using System.Diagnostics;
using Xunit;

namespace AutoCutBlight.Tests;

public class PackagingScriptTests : IDisposable
{
    readonly string dir = Directory.CreateTempSubdirectory("acb-scripts").FullName;

    const string AboutXml = "<ModMetaData><packageId>rimhunter.autocutblight</packageId></ModMetaData>";

    public void Dispose() => Directory.Delete(dir, true);

    void Write(string relative, string text) {
        var path = Path.Combine(dir, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    void Copy(string script) {
        Directory.CreateDirectory(Path.Combine(dir, "scripts"));
        File.Copy(Path.Combine(ModRoot.Path, "scripts", script), Path.Combine(dir, "scripts", script));
    }

    (int Code, string Output) Run(string script) {
        var p = Process.Start(new ProcessStartInfo("bash", "scripts/" + script) {
            WorkingDirectory = dir, RedirectStandardOutput = true, RedirectStandardError = true,
        })!;
        var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, output);
    }

    void IdentityRepo(params string[] files) {
        Copy("test-identity");
        Write(".gitignore", "*.dll\n");
        Write("1.6/Assemblies/ModAutoCutBlight.dll", "");
        Write("1.7/Assemblies/ModAutoCutBlight.dll", "");
        foreach (var f in files) Write(f, "x");
        var init = Process.Start(new ProcessStartInfo("git", "init -q") { WorkingDirectory = dir })!;
        init.WaitForExit();
        Assert.Equal(0, init.ExitCode);
    }

    static readonly string[] OriginalIds = {
        string.Concat("188518", "6534"), string.Concat("352016", "7264"), string.Concat("322534", "0303"),
    };

    public static TheoryData<string> EachOriginalId() => new(OriginalIds);

    const string IdentityLine = "nothing of the original: no Workshop id, preview, art or binary";

    void AssertIdentityPasses() {
        var (_, output) = Run("test-identity");
        Assert.Contains("PASS " + IdentityLine, output);
    }

    void AssertIdentityFails(string reason) {
        var (code, output) = Run("test-identity");
        Assert.NotEqual(0, code);
        Assert.Contains("FAIL " + IdentityLine, output);
        Assert.Contains(reason, output);
    }

    [Fact]
    public void A_png_under_docs_verification_is_allowed() {
        IdentityRepo("docs/verification/x.png");
        AssertIdentityPasses();
    }

    [Fact]
    public void A_png_in_a_folder_that_only_starts_like_docs_verification_fails() {
        IdentityRepo("docs/verification-x/a.png");
        AssertIdentityFails("docs/verification-x/a.png");
    }

    [Fact]
    public void A_png_elsewhere_in_docs_fails() {
        IdentityRepo("docs/c.png");
        AssertIdentityFails("docs/c.png");
    }

    [Fact]
    public void A_jpg_under_docs_verification_fails() {
        IdentityRepo("docs/verification/x.jpg");
        AssertIdentityFails("docs/verification/x.jpg");
    }

    [Fact]
    public void The_preview_png_in_About_is_allowed() {
        IdentityRepo("About/Preview.png");
        AssertIdentityPasses();
    }

    [Fact]
    public void Another_png_in_About_fails() {
        IdentityRepo("About/Preview.png", "About/Other.png");
        AssertIdentityFails("About/Other.png");
    }

    [Fact]
    public void A_preview_png_outside_About_fails() {
        IdentityRepo("Preview.png");
        AssertIdentityFails("Preview.png");
    }

    [Fact]
    public void A_preview_png_in_a_nested_About_folder_fails() {
        IdentityRepo("1.7/About/Preview.png");
        AssertIdentityFails("1.7/About/Preview.png");
    }

    [Fact]
    public void A_preview_jpg_in_About_fails() {
        IdentityRepo("About/Preview.jpg");
        AssertIdentityFails("About/Preview.jpg");
    }

    [Theory]
    [MemberData(nameof(EachOriginalId))]
    public void Each_original_id_is_allowed_in_WORKSHOP_md(string id) {
        IdentityRepo();
        Write("WORKSHOP.md", id);
        AssertIdentityPasses();
    }

    [Fact]
    public void All_three_original_ids_together_are_allowed_in_WORKSHOP_md() {
        IdentityRepo();
        Write("WORKSHOP.md", string.Join(" ", OriginalIds));
        AssertIdentityPasses();
    }

    [Theory]
    [MemberData(nameof(EachOriginalId))]
    public void Each_original_id_is_not_allowed_in_the_readme(string id) {
        IdentityRepo();
        Write("Readme.md", id);
        AssertIdentityFails("Readme.md");
    }

    void PackageRepo(bool withDocs, bool withPreview = true, string? extraFile = null) {
        Copy("test-package");
        Write("scripts/package", "#!/usr/bin/env bash\nset -euo pipefail\nhere=\"$(cd \"$(dirname \"${BASH_SOURCE[0]}\")/..\" && pwd)\"\ncp -r \"$here/fixture\" \"$2\"\n");
        File.SetUnixFileMode(Path.Combine(dir, "scripts", "package"), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Write("fixture/About/About.xml", AboutXml);
        foreach (var p in new[] {
            "Defs/WorkGiverDef/WorkGivers.xml", "Languages/English/DefInjected/WorkGiverDef/WorkGivers.xml",
            "LoadFolders.xml", "LICENSE", "CHANGELOG.md", "Source/ModAutoCutBlight.csproj",
            "1.6/Assemblies/ModAutoCutBlight.dll", "1.7/Assemblies/ModAutoCutBlight.dll" })
            Write("fixture/" + p, "x");
        if (withDocs) Write("fixture/docs/verification/README.md", "x");
        if (withPreview) Write("fixture/About/Preview.png", "x");
        if (extraFile != null) Write("fixture/" + extraFile, "x");
    }

    [Fact]
    public void A_package_without_docs_passes() {
        PackageRepo(false);
        var (code, output) = Run("test-package");
        Assert.True(code == 0, output);
    }

    [Fact]
    public void A_package_that_holds_docs_fails() {
        PackageRepo(true);
        var (code, output) = Run("test-package");
        Assert.NotEqual(0, code);
        Assert.Contains("FAIL unexpected docs", output);
    }

    [Fact]
    public void A_package_without_the_preview_fails() {
        PackageRepo(false, withPreview: false);
        var (code, output) = Run("test-package");
        Assert.NotEqual(0, code);
        Assert.Contains("FAIL missing About/Preview.png", output);
    }

    [Fact]
    public void A_package_with_another_png_fails() {
        PackageRepo(false, extraFile: "Textures/a.png");
        var (code, output) = Run("test-package");
        Assert.NotEqual(0, code);
        Assert.Contains("Textures/a.png", output);
    }
}
