using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class Collisions
{
    struct MinMax
    {
        public float min, max;
    }

    public static bool IntersectAABB(AABB a, AABB b)
    {
        if (a.max.x <= b.min.x || b.max.x <= a.min.x ||
            a.max.y <= b.min.y || b.max.y <= a.min.y)
        {
            return false;
        }
        return true;
    }

    public static bool Collide(Body a, Body b, out float3 normal, out float depth)
    {
        depth = 0;
        normal = float3.zero;
        if (a.type == BodyType.SPHERE && b.type == BodyType.SPHERE)
        {
            return Collisions.IntersetCircles(a.position, a.size.x, b.position, b.size.x, out normal, out depth);
        }
        else if (a.type == BodyType.BOX && b.type == BodyType.BOX)
        {
            BoxVertices PolyGonA = new BoxVertices(a.position, a.size, a.rotation);
            BoxVertices PolyGonB = new BoxVertices(b.position, b.size, b.rotation);
            float3[] verticesA = GetVertices(PolyGonA);
            float3[] verticesB = GetVertices(PolyGonB);
            float3[] aAxes = GetAxis(PolyGonA); // 3
            float3[] bAxes = GetAxis(PolyGonB); // 3

            // https://github.com/irixapps/Unity-Separating-Axis-SAT/blob/master/Assets/SeparatingAxisTest.cs
            float3[] axises = new float3[]
            {
                aAxes[0],
                aAxes[1],
                aAxes[2],
                bAxes[0],
                bAxes[1],
                bAxes[2],
                math.cross(aAxes[0], bAxes[0]),
                math.cross(aAxes[0], bAxes[1]),
                math.cross(aAxes[0], bAxes[2]),
                math.cross(aAxes[1], bAxes[0]),
                math.cross(aAxes[1], bAxes[1]),
                math.cross(aAxes[1], bAxes[2]),
                math.cross(aAxes[2], bAxes[0]),
                math.cross(aAxes[2], bAxes[1]),
                math.cross(aAxes[2], bAxes[2])
            };

            if(axises.Length != 15)
            {
                throw new System.Exception("Invalid number of axis for Box: " + axises.Length);
            }

            if ( Collisions.IntersectPolygons(verticesA, verticesB, a.position, b.position, axises, out normal, out depth))
            {
                return true;
            }
            else if (Collisions.IntersectPolygons(verticesB, verticesA, b.position, a.position, axises, out normal, out depth))
            {
                return true;
            }
        }
        return false;
    }
    /*public static bool IntersectPolygons2(float3[] verticesA, float3[] verticesB, float3 centerA, float3 centerB, float3[] allNormals, out float3 normal, out float depth)
    {
        bool hasOverlap = true;
        normal = float3.zero;
        depth = float.PositiveInfinity;
        for (int i = 0; i < allNormals.Length; i++)
        {
            float bProjMin = float.MaxValue, aProjMin = float.MaxValue;
            float bProjMax = float.MinValue, aProjMax = float.MinValue;
            float3 n = allNormals[i];
            if (Utils.CloseEnough(n, float3.zero)) return true;

            for (int j = 0; j < verticesB.Length; j++)
            {
                float val = FindScalarProjection((verticesB[j]), n);

                if (val < bProjMin)
                {
                    bProjMin = val;
                }

                if (val > bProjMax)
                {
                    bProjMax = val;
                }
            }

            for (int j = 0; j < verticesA.Length; j++)
            {
                float val = FindScalarProjection((verticesA[j]), n);

                if (val < aProjMin)
                {
                    aProjMin = val;
                }

                if (val > aProjMax)
                {
                    aProjMax = val;
                }
            }

            float overlap = FindOverlap(aProjMin, aProjMax, bProjMin, bProjMax);

            if (overlap < minOverlap)
            {
                minOverlap = overlap;
                minOverlapAxis = axis;

                penetrationAxes.Add(axis);
                penetrationAxesDistance.Add(overlap);

            }

            if (overlap <= 0)
            {
                // Separating Axis Found Early Out
                return false;
            }
        }
        return true; // A penetration has been found
    }*/
    public static bool IntersectPolygons(float3[] verticesA, float3[] verticesB, float3 centerA, float3 centerB, float3[] allNormals, out float3 normal, out float depth)
    {
        normal = float3.zero;
        depth = float.MaxValue;
        for (int i = 0; i < allNormals.Length; i++)
        {
            float3 n = allNormals[i];
            // Handles the cross product = {0,0,0} case
            if (Utils.CloseEnough(n, float3.zero)) return true;
            n = math.normalize(n);

            MinMax minMaxA = Collisions.ProjectVerticesMinMax(n, verticesA);
            MinMax minMaxB = Collisions.ProjectVerticesMinMax(n, verticesB);
            if (minMaxA.min >= minMaxB.max || minMaxB.min > minMaxA.max)
            {
                // serparation
                return false;
            }
            float axisDepth = math.min(minMaxB.max - minMaxA.min, minMaxA.max - minMaxB.min);
            if (axisDepth < depth)
            {
                depth = axisDepth;
                normal = n;
            }
        }

        float3 direction = centerB - centerA;
        if (math.dot(direction, normal) < 0)
        {
            normal = -normal;
        }

        return true;
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

    public static void FindContactPoints(Body a, Body b, out NativeList<float3> contacts)
    {
        contacts = new NativeList<float3>(Allocator.Temp);
        if (a.type == BodyType.SPHERE && b.type == BodyType.SPHERE)
        {
            float3 contact;
            Collisions.FindContactPoint(a.position, a.size.x, b.position, out contact);
            contacts.Add(contact);
        }
        else if (a.type == BodyType.BOX && b.type == BodyType.BOX)
        {

        }
    }

    public static void FindContactPoint(float3 centerA, float radiusA, float3 centerB, out float3 cp)
    {
        float3 ab = math.normalize(centerB - centerA);
        cp = centerA + ab * radiusA;
    }

    private static MinMax ProjectVerticesMinMax(float3 normal, float3[] vertices)
    {
        MinMax minMax = new MinMax { min = float.MaxValue, max = float.MinValue };
        foreach (float3 p in vertices)
        {
            float proj = math.dot(p, normal);
            if (proj < minMax.min) minMax.min = proj;
            if (proj > minMax.max) minMax.max = proj;
        }
        return minMax;
    }

    public static float3[] GetVertices(BoxVertices vertices)
    {
        return new float3[] { vertices.bottomLeftBack, vertices.bottomLeftFront, vertices.bottomRightBack, vertices.bottomRightFront,
                              vertices.topLeftBack, vertices.topLeftFront, vertices.topRightBack, vertices.topRightFront};
    }

    public static float3[] GetAxis(BoxVertices vertices)
    {
        return new float3[]
        {
            vertices.rightN, vertices.topN, vertices.frontN
        };
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

public struct BoxVertices
{
    public readonly float3 center;

    public readonly float3 bottomLeftBack, bottomLeftFront, bottomRightBack, bottomRightFront,
        topLeftBack, topLeftFront, topRightBack, topRightFront;

    public readonly float3 topN, rightN, frontN;

    public BoxVertices(float3 _center, float3 _size, quaternion _rot)
    {
        this.center = _center;
        this.bottomLeftBack = math.mul(_rot, (Utils.Down + Utils.Left + Utils.Back) * _size/2);
        this.bottomLeftFront = math.mul(_rot, (Utils.Down + Utils.Left + Utils.Forward) * _size/2);
        this.bottomRightBack = math.mul(_rot, (Utils.Down + Utils.Right + Utils.Back) * _size/2);
        this.bottomRightFront = math.mul(_rot, (Utils.Down + Utils.Right + Utils.Forward) * _size/2);

        this.topLeftBack = math.mul(_rot, (Utils.Up + Utils.Left + Utils.Back) * _size/2);
        this.topLeftFront = math.mul(_rot, (Utils.Up + Utils.Left + Utils.Forward) * _size/2);
        this.topRightBack = math.mul(_rot, (Utils.Up + Utils.Right + Utils.Back) * _size/2);
        this.topRightFront = math.mul(_rot, (Utils.Up + Utils.Right + Utils.Forward) * _size/2);

        this.topN = math.mul(_rot, Utils.Up);
        this.rightN = math.mul(_rot, Utils.Right);
        this.frontN = math.mul(_rot, Utils.Forward);

        this.bottomLeftBack += _center;
        this.bottomLeftFront += _center;
        this.bottomRightBack += _center;
        this.bottomRightFront += _center;

        this.topLeftBack += _center;
        this.topLeftFront += _center;
        this.topRightBack += _center;
        this.topRightFront += _center;

    }
}