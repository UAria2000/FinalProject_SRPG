using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct HexCoord : IEquatable<HexCoord>
{
    public int q;
    public int r;

    public HexCoord(int q, int r)
    {
        this.q = q;
        this.r = r;
    }

    public int S => -q - r;

    public static readonly HexCoord[] NeighborDirections =
    {
        new HexCoord(1, 0),
        new HexCoord(1, -1),
        new HexCoord(0, -1),
        new HexCoord(-1, 0),
        new HexCoord(-1, 1),
        new HexCoord(0, 1),
    };

    public List<HexCoord> GetNeighbors()
    {
        List<HexCoord> result = new List<HexCoord>(6);
        for (int i = 0; i < NeighborDirections.Length; i++)
            result.Add(this + NeighborDirections[i]);
        return result;
    }

    public static int Distance(HexCoord a, HexCoord b)
    {
        return (Mathf.Abs(a.q - b.q) + Mathf.Abs(a.r - b.r) + Mathf.Abs(a.S - b.S)) / 2;
    }

    public static HexCoord operator +(HexCoord a, HexCoord b)
    {
        return new HexCoord(a.q + b.q, a.r + b.r);
    }

    public static HexCoord operator -(HexCoord a, HexCoord b)
    {
        return new HexCoord(a.q - b.q, a.r - b.r);
    }

    public bool Equals(HexCoord other)
    {
        return q == other.q && r == other.r;
    }

    public override bool Equals(object obj)
    {
        return obj is HexCoord other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (q * 397) ^ r;
        }
    }

    public override string ToString()
    {
        return $"({q}, {r})";
    }
}
