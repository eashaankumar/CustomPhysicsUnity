using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct AABB
{
    public readonly float3 min, max;

    public AABB(float3 _min, float3 _max)
    {
        min = _min;
        max = _max;
    }
}