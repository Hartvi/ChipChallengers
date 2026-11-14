using NUnit.Framework;
using UnityEngine;

public class AudioManagerTests
{
    [Test]
    public void SingletonIsCachedAndCanBeRecreatedAfterDestruction()
    {
        var manager = AudioManager.Instance;
        Assert.That(AudioManager.Instance, Is.SameAs(manager));
        Assert.That(manager.sounds, Is.Not.Null);
        Object.DestroyImmediate(manager.gameObject);
        var replacement = AudioManager.Instance;
        try
        {
            Assert.That(replacement, Is.Not.Null);
            Assert.That(AudioManager.Instance, Is.SameAs(replacement));
        }
        finally
        {
            Object.DestroyImmediate(replacement.gameObject);
        }
    }
}
