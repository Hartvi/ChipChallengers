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
                l.Add(MakePrintable(i));
            }
            return string.Join(", ", l) + "\n";
        } else
        {
            return o.ToString();
        }
    }
    public static void print(object o)
    {
        UnityEngine.Debug.Log(MakePrintable(o));
    }
    public static void print(string comment, object o)
    {
        UnityEngine.Debug.Log(MakePrintable(o));
    }
}
