using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthAspect : BaseAspect
{
    float myHealth;
    bool hasHealthSet = false;

    public void SetHealth(float health)
    {
        if (this.hasHealthSet)
        {
            throw new InvalidOperationException($"Cannot set health on chip {this.myChip.equivalentVirtualChip.ChipType}. It has already been set.");
        }
        this.myHealth = health;
        this.hasHealthSet = true;
    }

    public void ApplyDamage(float power)
    {
        if (!this.hasHealthSet)
        {
            throw new InvalidOperationException($"Cannot deal damage to chip whose health has not been set: {this.myChip.equivalentVirtualChip.ChipType}");
        }
        print($"Health: {this.myHealth}, damage: {power}");
        this.myHealth = this.myHealth - power;
        if (this.myHealth <= 0f)
        {
            if (this.myChip is not null)
            {
                this.myChip.Die();
            }
        }
    }
}
