using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SoundLifetimeTests
{
    [Test]
    public void ReplacingSoundAspectsReleasesTheirOwnedSources()
    {
        var root = new GameObject("Sound ownership test");
        root.SetActive(false);
        try
        {
            var core = NetworkTestObjects.Add<CoreChip>(root);
            core.myCore = core;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            for (int rebuild = 0; rebuild < 5; rebuild++)
            {
                var aspect = root.AddComponent<BaseSoundAspect>();
                aspect.myChip = core;
                typeof(BaseSoundAspect).GetMethod("Start", flags).Invoke(aspect, null);
                Assert.That(root.GetComponents<AudioSource>().Length, Is.EqualTo(4));
                typeof(BaseSoundAspect).GetMethod("OnDestroy", flags).Invoke(aspect, null);
                Object.DestroyImmediate(aspect);
                Assert.That(root.GetComponents<AudioSource>(), Is.Empty);
                Assert.That(core.RuntimeFunctions, Is.Empty);
            }
        }
        finally { Object.DestroyImmediate(root); }
    }
}
