using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MonoExtension
{
    public static T[] Siblings<T>(this MonoBehaviour mb, bool takeInactive) where T : MonoBehaviour
    {
        List<T> ret = new List<T>();
        var parent = mb.transform.parent;
        var childCount = parent.childCount;
        for(int i = 0; i < childCount; ++i)
        {
            var potentialSibling = parent.GetChild(i).GetComponent<T>();
            if(potentialSibling != null)
            {
                if (takeInactive)
                {
                    ret.Add(potentialSibling);
                }
                else if (potentialSibling.gameObject.activeSelf)
                {
                    ret.Add(potentialSibling);
                }
            }
        }
        return ret.ToArray();
    }

    public static Vector2 V2(this Vector3 v3)
    {
        return new Vector2(v3.x, v3.y);
    }

}
