using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct Utils
{
    public readonly static float3 Left = new float3(-1, 0, 0);
    public readonly static float3 Right = new float3(1, 0, 0);
    public readonly static float3 Up = new float3(0, 1, 0);
    public readonly static float3 Down = new float3(0, -1, 0);
    public readonly static float3 Forward = new float3(0, 0, 1);
    public readonly static float3 Back = new float3(0, 0, -1);

    public static bool CloseEnough(float3 a, float3 b)
    {
        return math.distancesq(a, b) < 1e-5f;
    }

    public static float3 WorldToLocal(float3 transformPos, quaternion transformRot, float3 worldPoint)
    {
        return math.mul(math.inverse(transformRot), (worldPoint - transformPos));
    }

    public static float3 LocalToWorld(float3 transformPos, quaternion transformRot, float3 localPoint)
    {
        return math.mul(transformRot, localPoint) + transformPos;
    }
}
