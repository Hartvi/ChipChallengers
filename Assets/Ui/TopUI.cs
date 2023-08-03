using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopUI : MonoBehaviour
{
    public RectTransform RT { get { return this.transform as RectTransform; } }
    protected Vector2Int left = Vector2Int.left;
    protected Vector2Int right = Vector2Int.right;
    protected Vector2Int up = Vector2Int.up;
    protected Vector2Int down = Vector2Int.down;
    protected Vector2Int zero = Vector2Int.zero;
}
