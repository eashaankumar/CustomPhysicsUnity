using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class Collisions
{
    public static bool IntersectAABB(AABB a, AABB b)
    {
        if (a.max.x <= b.min.x || b.max.x <= a.min.x ||
            a.max.y <= b.min.y || b.max.y <= a.min.y)
        {
            return false;
        }
        return true;
    }

    public static bool Collide(SphereBody a, SphereBody b, out float3 normal, out float depth)
    {
        depth = 0;
        normal = float3.zero;
        return Collisions.IntersetCircles(a.position, a.radius, b.position, b.radius, out normal, out depth);
    }


    public static bool IntersetCircles(float3 centerA, float radiusA, float3 centerB, float radiusB, out float3 normal, out float depth)
    {
        normal = float3.zero;
        depth = 0;

        float distance = math.distance(centerA, centerB);
        float radii = radiusA + radiusB;

        if (distance >= radii)
        {
            return false;
        }

        normal = math.normalize(centerB - centerA);
        depth = radii - distance;

        return true;
    }

    public static void FindContactPoints(SphereBody a, SphereBody b, out NativeList<float3> contacts)
    {
        contacts = new NativeList<float3>(Allocator.Temp);
        float3 contact;
        Collisions.FindContactPoint(a.position, a.radius, b.position, out contact);
        contacts.Add(contact);
    }

    public static void FindContactPoint(float3 centerA, float radiusA, float3 centerB, out float3 cp)
    {
        float3 ab = math.normalize(centerB - centerA);
        cp = centerA + ab * radiusA;
    }
}

public readonly struct CollisionManifold : System.IEquatable<CollisionManifold>
{
    public readonly int a;
    public readonly int b;
    public readonly float3 normal;
    public readonly float depth;

    public CollisionManifold(int _a, int _b, float3 _n, float _d)
    {
        this.a = _a;
        this.b = _b;
        this.normal = _n;
        this.depth = _d;
    }

    public bool Equals(CollisionManifold other)
    {
        return a == b;
    }
}
