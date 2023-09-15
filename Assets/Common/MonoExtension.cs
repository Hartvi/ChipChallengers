using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    public static void SetTextSizesOf(this MonoBehaviour m, MonoBehaviour[] behaviours, float fontSize)
    {
        if(behaviours is null)
        {
            throw new ArgumentNullException($"SetTextSize: behaviours array is null.");
        }
        foreach(var b in behaviours)
        {
            TMP_Text[] ts = b.GetComponentsInChildren<TMP_Text>();
            if(ts is null)
            {
                continue;
            }
            foreach(var t in ts)
            {
                t.fontSize = fontSize;
            }
        }
    }

    public static void SetTransparent(this Material m)
    {
        m.SetFloat("_Mode", 2);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
    }

}
