using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LocalDirection
{
    // As is defined in euler rotation
    North, West, South, East
}

public class ColourHighlighter : MonoBehaviour
{
    public LocalDirection localDirection;
}
