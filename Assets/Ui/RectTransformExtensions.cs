using UnityEngine;
public static class RectExtension
{
    public static void SetLeft(this RectTransform rt, float left)
    {
        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
    }

    public static void SetRight(this RectTransform rt, float right)
    {
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }

    public static void SetTB(this RectTransform rt, float value)
    {
        rt.SetTop(value);
        rt.SetBottom(value);
    }
    public static void SetTop(this RectTransform rt, float top)
    {
        rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
    }

    public static void SetBottom(this RectTransform rt, float bottom)
    {
        rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
    }
    public static void SetLRTB(this RectTransform rt, float gap)
    {
        SetLeft(rt, gap);
        SetRight(rt, gap);
        SetTop(rt, gap);
        SetBottom(rt, gap);
    }
    public static float rectHeight(RectTransform rt) {
        return rt.sizeDelta.y;
    }
    public static float rectWidth(RectTransform rt) {
        return rt.sizeDelta.x;
    }
    public static RectTransform RT(this GameObject g) {
        return (g.transform as RectTransform);
    }
    // public static float rectHeight(this RectTransform rt) {
    //     return rt.sizeDelta.y;
    // }
    // public static float rectWidth(this RectTransform rt) {
    //     return rt.sizeDelta.x;
    // }
}