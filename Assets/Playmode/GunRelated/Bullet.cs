using UnityEngine;

public class Bullet : MonoBehaviour
{
    public const float LifeTime = 5f;

    float myPower;
    float bulletWeightPerTime;
    float remainingLife;
    Rigidbody rb;
    Rigidbody sourceBody;
    CoreChip sourceCore;
    Vector3 velocity;

    public void SetupBullet(float bulletWeightPerTime, float power)
    {
        this.bulletWeightPerTime = bulletWeightPerTime;
        this.myPower = power;
        this.rb = this.GetComponent<Rigidbody>();
        // Flight and impacts are handled below; a solid projectile would push its own gun on spawn.
        this.rb.isKinematic = true;
        this.rb.detectCollisions = false;
        foreach (var collider in this.GetComponentsInChildren<Collider>()) { collider.enabled = false; }
        this.gameObject.SetActive(false);
    }

    public void Fire(Vector3 initialPosition, Vector3 velocity, Rigidbody sourceBody = null, CoreChip sourceCore = null)
    {
        this.sourceBody = sourceBody;
        this.sourceCore = sourceCore;
        this.velocity = velocity;
        this.gameObject.SetActive(true);
        this.rb.position = initialPosition;
        this.remainingLife = LifeTime;
    }

    void FixedUpdate()
    {
        float seconds = Mathf.Min(Time.fixedDeltaTime, this.remainingLife);
        Vector3 gravity = this.rb.useGravity ? Physics.gravity : Vector3.zero;
        Vector3 displacement = this.velocity * seconds + gravity * (0.5f * seconds * seconds);
        Vector3 position = this.rb.position;
        this.velocity += gravity * seconds;

        // Sweep the whole physics step so fast bullets cannot skip thin surfaces between rendered frames.
        RaycastHit closest = default;
        bool hasHit = false;
        if (displacement.sqrMagnitude > 0f)
        {
            foreach (var hit in Physics.RaycastAll(position, displacement.normalized, displacement.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (this.IsSource(hit)) { continue; }
                if (!hasHit || hit.distance < closest.distance) { closest = hit; hasHit = true; }
            }
        }
        if (hasHit)
        {
            this.rb.position = closest.point;
            // Consume before damage callbacks, which may destroy the target or the firing gun's pool.
            this.gameObject.SetActive(false);
            if (closest.rigidbody != null)
            {
                closest.rigidbody.AddForceAtPosition(this.velocity * this.bulletWeightPerTime,
                    closest.point, ForceMode.Impulse);
            }
            var health = closest.collider.GetComponentInParent<HealthAspect>();
            if (health != null) { health.ApplyDamage(this.myPower); }
            return;
        }

        this.rb.position = position + displacement;
        this.remainingLife -= seconds;
        if (this.remainingLife <= 0f) { this.gameObject.SetActive(false); }
    }

    bool IsSource(RaycastHit hit)
    {
        if (this.sourceBody != null && hit.rigidbody == this.sourceBody) { return true; }
        if (this.sourceCore == null) { return false; }
        var chip = hit.collider.GetComponentInParent<GeometricChip>();
        return chip != null && (chip == this.sourceCore || chip.myCore == this.sourceCore);
    }
}
