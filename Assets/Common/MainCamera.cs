using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour
{
    //int OccasionCounter = 0;
    //float OccasianPeriodSeconds = 10f;
    //int OccasionPeriod;

    //float ObjectFactor = 2f;
    //float TimeStepReductionFactor = 0.8f;
    //int MinNumberOfObjects;
    //int OldObjectCount;

    bool aboveY = false;
    GameObject water;

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
        //this.OccasionPeriod = (int)(this.OccasianPeriodSeconds * 1f / Time.deltaTime);
        //this.OldObjectCount = (int)(42f * Time.fixedDeltaTime);
        this.water = this.transform.GetChild(0).gameObject;
        this.water.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        float camY = this.transform.position.y;
        if (camY < 0f)
        {
            float colorRatio = Mathf.Min(1f, -camY * 1e-3f);
            Camera.main.backgroundColor = ((1f - colorRatio) * (0.2f * Color.blue + 0.6f * Color.white) + colorRatio * Color.black);
            if (aboveY)
            {
                this.water.SetActive(true);
                aboveY = false;
            }
        }
        else if (!aboveY)
        {
            aboveY = true;
            this.water.SetActive(false);
            Camera.main.backgroundColor = 0.1f * Color.blue + 0.8f * Color.white;
        }


        //if (this.OccasionCounter++ == this.OccasionPeriod) {
        //    Rigidbody[] allObjects = GameObject.FindObjectsOfType<Rigidbody>();
        //    //print($"Checking number of objects: {allObjects.Length}");

        //    this.OccasionCounter = 0;

        //    if (allObjects.Length > this.ObjectFactor * this.OldObjectCount)
        //    {
        //        Time.fixedDeltaTime = this.TimeStepReductionFactor * Time.fixedDeltaTime;
        //        this.OldObjectCount = allObjects.Length;
        //    }
        //}
    }
}
