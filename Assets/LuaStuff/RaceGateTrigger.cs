using System;
using UnityEngine;

// RaceGateTrigger forwards Unity trigger entries to its race controller.
public class RaceGateTrigger : MonoBehaviour
{
    public Action<Collider> Entered;

    // OnTriggerEnter forwards the entering collider for player and gate-order validation.
    void OnTriggerEnter(Collider other) => this.Entered?.Invoke(other);
}
