using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BulletTests
{
    readonly List<GameObject> objects = new List<GameObject>();
    readonly Vector3 origin = new Vector3(10000f, 10000f, 10000f);
    Bullet bullet;

    [SetUp]
    public void SetUp()
    {
        var projectile = this.Create("Test bullet", 0f);
        projectile.AddComponent<SphereCollider>();
        projectile.AddComponent<Rigidbody>().useGravity = false;
        this.bullet = projectile.AddComponent<Bullet>();
        this.bullet.SetupBullet(2f);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var item in this.objects) { Object.DestroyImmediate(item); }
        this.objects.Clear();
    }

    [Test]
    public void ThinStaticSurfaceConsumesBullet()
    {
        var wall = this.Surface("Thin wall", 3f);
        wall.SetActive(false);
        this.FireAndStep();
        Assert.IsTrue(this.bullet.gameObject.activeSelf);
        wall.SetActive(true);
        this.FireAndStep();
        Assert.IsFalse(this.bullet.gameObject.activeSelf, "A wall without health must consume the bullet");
    }

    [Test]
    public void SourceCollidersAndTriggersDoNotInterceptBullet()
    {
        var gun = this.Surface("Gun", 0f);
        var body = gun.AddComponent<Rigidbody>();
        body.useGravity = false;
        var barrel = this.Surface("Gun barrel", 1f);
        barrel.transform.SetParent(gun.transform, true);
        this.Surface("Race gate", 2f).GetComponent<Collider>().isTrigger = true;
        int targetHits = 0;
        var target = this.Surface("Wall", 4f).AddComponent<HealthAspect>();
        target.SetHealth(1f);
        target.SetDeathCallbacks(new System.Action[] { () => targetHits++ });
        this.FireAndStep(body);
        Assert.IsFalse(this.bullet.GetComponent<Collider>().enabled);
        Assert.IsFalse(this.bullet.GetComponent<Rigidbody>().detectCollisions,
            "Projectile contacts must not push the firing gun");
        Assert.AreEqual(1, targetHits, "Shooter or trigger intercepted the projectile");
        Assert.IsFalse(this.bullet.gameObject.activeSelf);
    }

    [Test]
    public void OnlyNearestTargetReceivesDamage()
    {
        int nearDeaths = 0, farDeaths = 0;
        // Create the farther collider first so correctness cannot depend on creation order.
        var far = this.Surface("Far target", 6f).AddComponent<HealthAspect>();
        far.SetHealth(1f);
        far.SetDeathCallbacks(new System.Action[] { () => farDeaths++ });
        var near = this.Surface("Near target", 3f).AddComponent<HealthAspect>();
        near.SetHealth(1f);
        near.SetDeathCallbacks(new System.Action[] { () => nearDeaths++ });
        this.FireAndStep();
        Assert.AreEqual(1, nearDeaths);
        Assert.AreEqual(0, farDeaths);
        Assert.IsFalse(this.bullet.gameObject.activeSelf);
    }

    [TestCase(0.5f)]
    [TestCase(2f)]
    public void ImpactTransfersProjectileMomentumToOtherObjectsOnly(float projectileMass)
    {
        var sourceCore = this.Create("Source core", -2f).AddComponent<CoreChip>();
        var gun = this.Surface("Source gun", 1f);
        var gunBody = gun.AddComponent<Rigidbody>();
        gunBody.useGravity = false;
        gun.AddComponent<CommonChip>().myCore = sourceCore;
        var sourceWheel = this.Surface("Another chip on the source model", 2f);
        var wheelBody = sourceWheel.AddComponent<Rigidbody>();
        wheelBody.useGravity = false;
        sourceWheel.AddComponent<CommonChip>().myCore = sourceCore;
        var target = this.Surface("Other object", 4f).AddComponent<Rigidbody>();
        target.useGravity = false;
        target.mass = 4f;
        var fartherTarget = this.Surface("Farther object", 6f).AddComponent<Rigidbody>();
        fartherTarget.useGravity = false;
        this.bullet.GetComponent<Rigidbody>().mass = projectileMass;
        Vector3 velocity = Vector3.forward * (10f / Time.fixedDeltaTime);

        bool autoSimulation = Physics.autoSimulation;
        try
        {
            Physics.autoSimulation = false;
            this.bullet.Fire(this.origin, velocity, gunBody, sourceCore);
            Physics.SyncTransforms();
            this.Step();
            Physics.Simulate(Time.fixedDeltaTime);

            Assert.That(target.velocity.z, Is.EqualTo(projectileMass * velocity.z / target.mass).Within(0.001f));
            Assert.That(gunBody.velocity.sqrMagnitude, Is.Zero);
            Assert.That(wheelBody.velocity.sqrMagnitude, Is.Zero);
            Assert.That(fartherTarget.velocity.sqrMagnitude, Is.Zero);
            Assert.That(this.bullet.gameObject.activeSelf, Is.False);
        }
        finally
        {
            Physics.autoSimulation = autoSimulation;
        }
    }

    [Test]
    public void ConsumedBulletCanBeReusedAndExpiresWithoutHitting()
    {
        var wall = this.Surface("Wall", 3f);
        this.FireAndStep();
        Assert.IsFalse(this.bullet.gameObject.activeSelf);
        wall.SetActive(false);
        this.FireAndStep();
        Assert.IsTrue(this.bullet.gameObject.activeSelf);
        for (int i = 0; i <= Mathf.CeilToInt(Bullet.LifeTime / Time.fixedDeltaTime); ++i)
        {
            if (!this.bullet.gameObject.activeSelf) { break; }
            this.Step();
        }
        Assert.IsFalse(this.bullet.gameObject.activeSelf);
    }

    GameObject Create(string name, float distance)
    {
        var item = new GameObject(name);
        item.transform.position = this.origin + Vector3.forward * distance;
        this.objects.Add(item);
        return item;
    }

    GameObject Surface(string name, float distance)
    {
        var item = this.Create(name, distance);
        item.AddComponent<BoxCollider>().size = new Vector3(2f, 2f, 0.02f);
        return item;
    }

    void FireAndStep(Rigidbody source = null)
    {
        this.bullet.Fire(this.origin, Vector3.forward * (10f / Time.fixedDeltaTime), source);
        Physics.SyncTransforms();
        this.Step();
    }

    void Step() => typeof(Bullet).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
        .Invoke(this.bullet, null);
}
