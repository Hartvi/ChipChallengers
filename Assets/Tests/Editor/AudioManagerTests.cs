using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioManagerTests
{
    [Test]
    public void SingletonIsCachedAndCanBeRecreatedAfterDestruction()
    {
        var manager = AudioManager.Instance;
        Assert.That(AudioManager.Instance, Is.SameAs(manager));
        Assert.That(manager.sounds, Is.Not.Null);
        UnityEngine.Object.DestroyImmediate(manager.gameObject);
        var replacement = AudioManager.Instance;
        try
        {
            Assert.That(replacement, Is.Not.Null);
            Assert.That(AudioManager.Instance, Is.SameAs(replacement));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(replacement.gameObject);
        }
    }

    [Test]
    public void ReservationsStayWithinEachSoundLimitAndReleaseTheirSources()
    {
        var manager = AudioManager.Instance;
        var root = manager.gameObject;
        manager.sounds = new[] {
            new Sound { name = "one", maxInstanceNumber = 1, pitch = 1f },
            new Sound { name = "two", maxInstanceNumber = 2, pitch = 1f },
            new Sound { name = "invalid", maxInstanceNumber = 0 }
        };
        try
        {
            var first = new HashSet<AudioSource>();
            var second = new HashSet<AudioSource>();
            for (int i = 0; i < 12; ++i)
            {
                manager.ReserveSound("one", (value, source) => first.Add(source))(0f);
                manager.ReserveSound("two", (value, source) => second.Add(source))(0f);
            }
            Assert.That(first.Count, Is.EqualTo(1));
            Assert.That(second.Count, Is.EqualTo(2));
            Assert.That(manager.allSounds.Count, Is.EqualTo(3));
            Assert.Throws<ArgumentException>(() => manager.ReserveSound("missing", (value, source) => { }));
            Assert.Throws<InvalidOperationException>(() => manager.ReserveSound("invalid", (value, source) => { }));
            UnityEngine.Object.DestroyImmediate(manager);
            Assert.That(root.GetComponents<AudioSource>(), Is.Empty);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
