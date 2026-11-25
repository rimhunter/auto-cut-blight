using System.Diagnostics;
using Xunit;

namespace AutoCutBlight.Tests;

public class TranslationScriptTests : IDisposable
{
    readonly string dir = Directory.CreateTempSubdirectory("acb-translations").FullName;

    const string DefXml = "<Defs><WorkGiverDef><defName>Giver</defName><label>cut it</label><verb>cut</verb></WorkGiverDef></Defs>";
    const string EnglishXml = "<LanguageData><Giver.label>cut it</Giver.label><Giver.verb>cut</Giver.verb></LanguageData>";

    public void Dispose() => Directory.Delete(dir, true);

    void Write(string relative, string text) {
        var path = Path.Combine(dir, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    void ValidRepo() {
        Directory.CreateDirectory(Path.Combine(dir, "scripts"));
        File.Copy(Path.Combine(ModRoot.Path, "scripts", "test-translations"), Path.Combine(dir, "scripts", "test-translations"));
        Write("Defs/WorkGiverDef/Giver.xml", DefXml);
        Write("Languages/English/DefInjected/WorkGiverDef/Giver.xml", EnglishXml);
    }

    (int Code, string Output) Run() {
        var p = Process.Start(new ProcessStartInfo("bash", "scripts/test-translations") {
            WorkingDirectory = dir, RedirectStandardOutput = true, RedirectStandardError = true,
        })!;
        var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, output);
    }

    void AssertFails(string reason) {
        var (code, output) = Run();
        Assert.NotEqual(0, code);
        Assert.Contains(reason, output);
    }

    [Fact]
    public void A_complete_English_folder_passes() {
        ValidRepo();
        var (code, output) = Run();
        Assert.True(code == 0, output);
    }

    [Fact]
    public void A_missing_English_folder_fails() {
        ValidRepo();
        Directory.Delete(Path.Combine(dir, "Languages"), true);
        AssertFails("Languages/English is missing");
    }

    [Fact]
    public void A_def_text_with_no_English_entry_fails() {
        ValidRepo();
        Write("Languages/English/DefInjected/WorkGiverDef/Giver.xml", "<LanguageData><Giver.label>cut it</Giver.label></LanguageData>");
        AssertFails("Giver.verb");
    }

    [Fact]
    public void An_English_entry_that_differs_from_the_def_fails() {
        ValidRepo();
        Write("Languages/English/DefInjected/WorkGiverDef/Giver.xml", "<LanguageData><Giver.label>chop it</Giver.label><Giver.verb>cut</Giver.verb></LanguageData>");
        AssertFails("Giver.label");
    }

    [Fact]
    public void An_English_entry_for_a_def_that_does_not_exist_fails() {
        ValidRepo();
        Write("Languages/English/DefInjected/WorkGiverDef/Giver.xml", "<LanguageData><Giver.label>cut it</Giver.label><Giver.verb>cut</Giver.verb><Gone.label>x</Gone.label></LanguageData>");
        AssertFails("Gone.label");
    }

    [Fact]
    public void An_empty_English_text_fails() {
        ValidRepo();
        Write("Languages/English/DefInjected/WorkGiverDef/Giver.xml", "<LanguageData><Giver.label> </Giver.label><Giver.verb>cut</Giver.verb></LanguageData>");
        AssertFails("empty");
    }

    [Fact]
    public void A_duplicate_key_fails() {
        ValidRepo();
        Write("Languages/English/DefInjected/WorkGiverDef/Giver.xml", "<LanguageData><Giver.label>cut it</Giver.label><Giver.verb>cut</Giver.verb><Giver.verb>cut</Giver.verb></LanguageData>");
        AssertFails("duplicate key");
    }

    [Fact]
    public void Another_language_folder_fails() {
        ValidRepo();
        Directory.CreateDirectory(Path.Combine(dir, "Languages", "French"));
        AssertFails("only English ships");
    }

    [Fact]
    public void A_repo_with_no_def_text_fails_instead_of_passing_on_nothing() {
        ValidRepo();
        Write("Defs/WorkGiverDef/Giver.xml", "<Defs/>");
        Write("Languages/English/DefInjected/WorkGiverDef/Giver.xml", "<LanguageData/>");
        AssertFails("no def text");
    }

    [Fact]
    public void A_Keyed_key_used_in_code_with_no_English_text_fails() {
        ValidRepo();
        Write("Source/Thing.cs", "class T { string s = \"AcbMissingKey\".Translate(); }");
        AssertFails("AcbMissingKey");
    }

    [Fact]
    public void A_Keyed_key_that_is_present_and_used_passes() {
        ValidRepo();
        Write("Source/Thing.cs", "class T { string s = \"AcbKnownKey\".Translate(); }");
        Write("Languages/English/Keyed/Keys.xml", "<LanguageData><AcbKnownKey>known</AcbKnownKey></LanguageData>");
        var (code, output) = Run();
        Assert.True(code == 0, output);
        Assert.Contains("1 Keyed keys", output);
    }

    [Fact]
    public void A_Keyed_key_read_with_a_backup_and_missing_fails() {
        ValidRepo();
        Write("Source/Thing.cs", "class T { string s = \"AcbBackupKey\".TranslateWithBackup(null); }");
        AssertFails("AcbBackupKey");
    }
}
