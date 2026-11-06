using UnityEngine;

// CoreCameraPlacement places menu cameras above and behind the core using a level horizon.
public static class CoreCameraPlacement
{
    // Place moves the camera behind the core's horizontal forward direction and aims it at the core.
    public static void Place(Camera camera, Transform core, float distance, float height)
    {
        if (camera == null || core == null) { return; }
        Vector3 forward = Vector3.ProjectOnPlane(core.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f) { forward = Vector3.forward; }
        camera.transform.position = core.position - forward.normalized * distance + Vector3.up * height;
        camera.transform.LookAt(core.position, Vector3.up);
    }
}
