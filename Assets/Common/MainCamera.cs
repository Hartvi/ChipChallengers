using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour
{
    int OccasionCounter = 0;
    float OccasianPeriodSeconds = 10f;
    int OccasionPeriod;

    float ObjectFactor = 2f;
    float TimeStepReductionFactor = 0.8f;
    int MinNumberOfObjects;
    int OldObjectCount;

    CoreChip core;
    void Awake()
    {
        core = Instantiate(Resources.Load<GameObject>("Chips/Core")).AddComponent<CoreChip>();
        core.gameObject.layer = 6;
        core.gameObject.AddComponent<Rigidbody>();
        core.gameObject.AddComponent<BoxCollider>();
        core.name = UIStrings.Core;
        //print($"Core: {core}");
    }

    void Start()
    {
        this.OccasionPeriod = (int)(this.OccasianPeriodSeconds * 1f / Time.deltaTime);
        this.OldObjectCount = (int)(42f * Time.fixedDeltaTime);
    }

    // Update is called once per frame
    void Update()
    {
        if (this.OccasionCounter++ == this.OccasionPeriod) {
            Rigidbody[] allObjects = GameObject.FindObjectsOfType<Rigidbody>();
            //print($"Checking number of objects: {allObjects.Length}");

            this.OccasionCounter = 0;

            if (allObjects.Length > this.ObjectFactor * this.OldObjectCount)
            {
                Time.fixedDeltaTime = this.TimeStepReductionFactor * Time.fixedDeltaTime;
                this.OldObjectCount = allObjects.Length;
            }
        }
    }
}
