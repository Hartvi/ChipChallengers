using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PRINT
{
    public static string MakePrintable(object o) {
        if (o is IEnumerable && (o is not string)) {
            List<string> l = new List<string>();
            IEnumerable eo = o as IEnumerable;
            foreach (var i in eo) {
                l.Add(PRINT.MakePrintable(i));
            }
            return string.Join(", ", l) + "\n";
        } else
        {
            return o.ToString();
        }
    }
    public static void IPrint(object o)
    {
        UnityEngine.Debug.Log(PRINT.MakePrintable(o));
    }
    public static void print(string comment, object o)
    {
        UnityEngine.Debug.Log(PRINT.MakePrintable(o));
    }
    public static void VisualizePosition(Vector3 position, Color colour)
    {
        var newObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        newObject.transform.position = position;
        newObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        PRINT.ChangeObjectColor(newObject, colour);
    }
    public static void ChangeObjectColor(GameObject targetObject, Color newColor)
    {
        Renderer objectRenderer = targetObject.GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            objectRenderer.material.color = newColor;
        }
        else
        {
            Debug.Log("No renderer on the target object.");
        }
    }
}
