using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SIU
{
    s, m, kg, A, K, mol, cd
}

public struct Units
{
    public const int NUMUNITS = 7;

    int[] units;
    float val;

    public static Units operator +(Units a, Units b)
    {
        if (a.units.SequenceEqual(b.units))
        {
            return new Units(a.val + b.val, a.units);
        }
        throw new InvalidOperationException($"Cannot sum different units");
    }

    public static Units operator *(Units a, Units b)
    {
        int[] resultUnits = new int[Units.NUMUNITS];

        for (int i = 0; i < Units.NUMUNITS; i++)
        {
            resultUnits[i] = a.units[i] + b.units[i];
        }

        return new Units(a.val * b.val, resultUnits);
    }


    public static implicit operator float(Units mu) => mu.val;
    public static explicit operator Units(float val) => new Units(val);
    public override string ToString() => $"{this.val} {this.GetType()}";

    public Units(float val)
    {
        this.units = new int[Units.NUMUNITS];
        this.val = val;
    }
    public Units(float val, int[] units)
    {
        if(units.Length != Units.NUMUNITS)
        {
            throw new ArgumentException($"Units do not have length {Units.NUMUNITS}.");
        }
        this.units = units;
        this.val = val;
    }
}
