using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TopUI : MonoBehaviour, IPointerExitHandler, IPointerMoveHandler
{
    private static bool OnUI = false;
    public static bool IsOnUI { get { return TopUI.OnUI; } }

    public RectTransform RT { get { return this.transform as RectTransform; } }
    protected Vector2Int left = Vector2Int.left;
    protected Vector2Int right = Vector2Int.right;
    protected Vector2Int up = Vector2Int.up;
    protected Vector2Int down = Vector2Int.down;
    protected Vector2Int zero = Vector2Int.zero;

    public void OnPointerExit(PointerEventData eventData)
    {
        TopUI.OnUI = false;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        TopUI.OnUI = true;
    }
    public static bool MouseMoving()
    {
        return Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0;
    }
    void OnDisable()
    {
        TopUI.OnUI = false;
    }
}
