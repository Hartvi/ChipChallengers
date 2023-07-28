using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class JointUtility
{

    public static ConfigurableJoint AttachWithConfigurableJoint(GameObject child, GameObject parent, Vector3 globalPoint, Vector3 globalAxis) {
        
        // NOT NECESSARY BUT GOOD TO HAVE SINCE IT'S SEARCHABLE
        ConfigurableJoint cj = child.AddComponent<ConfigurableJoint>();

        // Connect the joint to the child GameObject
        var parentRB = parent.GetComponent<Rigidbody>();
        // ALWAYS CHECK IF SOMETHING IS NULL BEFORE USING IT
        if(parentRB == null) {
            throw new NullReferenceException($"Rigidbody of parent {parent} of child {child} is null.");
        }
        cj.connectedBody = parentRB;

        // Convert global point and axis to the parent's local space
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

