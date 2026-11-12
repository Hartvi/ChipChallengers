using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class JointUtility
{
    // Breaking strength is separate from the spring/damper that holds the target angle.
    // Finite limits let collisions tear assemblies apart instead of remaining unbreakable.
    public const float BreakForce = 100000f; // N
    public const float BreakTorque = 25000f; // N m

    public static ConfigurableJoint AttachWithConfigurableJoint(GameObject child, GameObject parent, Vector3 globalPoint, Vector3 globalAxis) {
        
        // NOT NECESSARY BUT GOOD TO HAVE SINCE IT'S SEARCHABLE
        foreach (var oldJoint in child.GetComponents<ConfigurableJoint>())
        {
            // Destroy is deferred: disconnect old constraints immediately so
            // only the rebuilt joint can act when simulation resumes.
            oldJoint.connectedBody = null;
            GameObject.Destroy(oldJoint);
        }
        ConfigurableJoint cj = child.AddComponent<ConfigurableJoint>();

        // Connect the joint to the child GameObject
        var parentRB = parent.GetComponent<Rigidbody>();
        // ALWAYS CHECK IF SOMETHING IS NULL BEFORE USING IT
        if(parentRB == null) {
            throw new NullReferenceException($"Rigidbody of Parent {parent} of child {child} is null.");
        }
        cj.connectedBody = parentRB;
        cj.breakForce = BreakForce;
        cj.breakTorque = BreakTorque;

        // Convert global point and axis to the Parent's local space
        Vector3 localAnchor = child.transform.InverseTransformPoint(globalPoint);
        Vector3 localAxis = child.transform.InverseTransformDirection(globalAxis);

        // Set the joint's anchor to the provided point
        cj.anchor = localAnchor;

        // Set the joint's axis to the provided axis
        cj.axis = localAxis;

        // Setup the rest of the ConfigurableJoint properties as needed...
        LockAllMotions(cj);
        return cj;
    }

    public static void LockAllMotions(ConfigurableJoint joint) {
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;
    }

}

