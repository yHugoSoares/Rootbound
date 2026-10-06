using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Duatborn.Tests
{
    public class FusionInstallerPatchPresenceTests
    {
        [Test]
        public void FusionInstallerHasMppmPatchMarker()
        {
            string path = Path.Combine(Application.dataPath, "Photon/Fusion/Editor/Fusion.Unity.Editor.cs");
            Assert.That(File.Exists(path), Is.True, "Fusion editor source not found at " + path);

            string text = File.ReadAllText(path);
            Assert.That(text.Contains("ROOTBOUND_PATCH(FusionInstaller-MPPM)"), Is.True,
                "Fusion MPPM installer patch is missing; reapply docs/patches/fusion-installer-mppm.patch after SDK updates.");
        }
    }
}
