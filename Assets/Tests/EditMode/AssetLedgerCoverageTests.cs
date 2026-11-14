using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>
    /// The asset controller in code. Every art pack under Assets/Art and every font under Assets/Fonts must have a row in
    /// Docs/ASSET_LEDGER.md and either a licence file beside it or an "own work" ledger row, and no pack may contain
    /// scripts, installers or editor-source files. These are the same rules the asset agents follow
    /// (Docs/ASSET_AGENT_RULES.md), enforced so nothing slips in by hand either.
    /// </summary>
    public class AssetLedgerCoverageTests
    {
        private static readonly string[] ForbiddenExtensions =
        {
            ".js", ".py", ".sh", ".bat", ".cmd", ".exe", ".dll", ".app", ".jar", ".url", ".psd", ".svg"
        };

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static string LedgerText()
        {
            string path = Path.Combine(ProjectRoot, "Docs", "ASSET_LEDGER.md");
            Assert.IsTrue(File.Exists(path), "Docs/ASSET_LEDGER.md is missing");
            return File.ReadAllText(path);
        }

        // A pack is a folder two levels under Assets/Art (Group/Pack) or one level under Assets/Fonts.
        private static List<string> PackFolders()
        {
            var packs = new List<string>();
            string art = Path.Combine(ProjectRoot, "Assets", "Art");
            if (Directory.Exists(art))
                foreach (var group in Directory.GetDirectories(art))
                    foreach (var pack in Directory.GetDirectories(group))
                        packs.Add("Assets/Art/" + Path.GetFileName(group) + "/" + Path.GetFileName(pack));

            string fonts = Path.Combine(ProjectRoot, "Assets", "Fonts");
            if (Directory.Exists(fonts))
                foreach (var pack in Directory.GetDirectories(fonts))
                    packs.Add("Assets/Fonts/" + Path.GetFileName(pack));
            return packs;
        }

        private static string[] LedgerRowsFor(string pack, string ledger)
        {
            var rows = new List<string>();
            foreach (var line in ledger.Split('\n'))
                if (line.StartsWith("|") && line.Contains(pack)) rows.Add(line);
            return rows.ToArray();
        }

        [Test]
        public void EveryPack_HasALedgerRow()
        {
            string ledger = LedgerText();
            var missing = new List<string>();
            foreach (var pack in PackFolders())
                if (LedgerRowsFor(pack, ledger).Length == 0) missing.Add(pack);

            Assert.IsEmpty(missing, "Packs with no row in Docs/ASSET_LEDGER.md:\n  " + string.Join("\n  ", missing));
        }

        [Test]
        public void EveryPack_HasLicenceEvidence_OrIsOwnWork()
        {
            string ledger = LedgerText();
            var problems = new List<string>();
            foreach (var pack in PackFolders())
            {
                string dir = Path.Combine(ProjectRoot, pack);
                bool hasLicenceFile = false;
                foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                {
                    string name = Path.GetFileName(file).ToLowerInvariant();
                    // "OFL.txt" is the standard licence file shipped with SIL Open Font License fonts.
                    if (name.Contains("licen") || name == "ofl.txt") { hasLicenceFile = true; break; }
                }

                bool ownWork = false;
                foreach (var row in LedgerRowsFor(pack, ledger))
                    if (row.ToLowerInvariant().Contains("own work") || row.ToLowerInvariant().Contains("in-house") || row.ToLowerInvariant().Contains("generated in-house"))
                        ownWork = true;

                if (!hasLicenceFile && !ownWork) problems.Add(pack);
            }
            Assert.IsEmpty(problems, "Packs with no licence file (use LICENCE_EVIDENCE.txt) and not marked own work:\n  " + string.Join("\n  ", problems));
        }

        [Test]
        public void NoPack_ContainsScriptsInstallersOrEditorSources()
        {
            var found = new List<string>();
            foreach (var pack in PackFolders())
                foreach (var file in Directory.GetFiles(Path.Combine(ProjectRoot, pack), "*", SearchOption.AllDirectories))
                    foreach (var ext in ForbiddenExtensions)
                        if (file.ToLowerInvariant().EndsWith(ext)) { found.Add(file.Replace(ProjectRoot + Path.DirectorySeparatorChar, "")); break; }

            Assert.IsEmpty(found, "Forbidden file types inside art packs:\n  " + string.Join("\n  ", found));
        }
    }
}
