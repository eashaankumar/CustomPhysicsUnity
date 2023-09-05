using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Collisions 
{

    struct MinMax
    {
        public float min, max;
    }

    public static bool IntersectCirclePolygon(Vector2 center, float radius, Vector2[] vertices, Vector2[] normals, out Vector2 normal, out float depth)
    {
        Debug.Assert(vertices.Length == normals.Length);
        normal = Vector2.zero;
        depth = float.MaxValue;

        int cpIndex = FindClosestPointOnPolygon(center, vertices);
        Vector2 cp = vertices[cpIndex];
        Vector2 cpAxis = (cp - center).normalized;

        for (int i = 0; i < normals.Length + 1; i++) // +1 for cp axis
        {
            Vector2 n = Vector2.zero;
            if (i < normals.Length) n = normals[i];
            else n = cpAxis;

            n.Normalize();
            MinMax minMaxA = Collisions.ProjectVerticesMinMax(n, vertices);
            MinMax minMaxB = Collisions.ProjectCircle(center, radius, n);

            if (minMaxA.min >= minMaxB.max || minMaxB.min > minMaxA.max)
            {
                // serparation
                return false;
            }
            float axisDepth = Mathf.Min(minMaxB.max - minMaxA.min, minMaxA.max - minMaxB.min);
            if (axisDepth < depth)
            {
                depth = axisDepth;
                normal = n;
            }
        }

        Vector2 centerA = GeometricCenter(vertices);
        Vector2 centerB = center;

        Vector2 direction = centerB - centerA;
        if (Vector2.Dot(direction, normal) < 0)
        {
            normal = -normal;
        }

        return true;
    }

    public static bool IntersetCircles(Vector2 centerA, float radiusA, Vector2 centerB, float radiusB, out Vector2 normal, out float depth)
    {
        normal = Vector2.zero;
        depth = 0;

        float distance = Vector2.Distance(centerA, centerB);
        float radii = radiusA + radiusB;

        if (distance >= radii)
        {
            return false;
        }

        normal = (centerB - centerA).normalized;
        depth = radii - distance;

        return true;
    }

    public static bool IntersectPolygons(Vector2[] verticesA, Vector2[] verticesB, Vector2[] normalsAB, out Vector2 normal, out float depth)
    {
        Debug.Assert(verticesA.Length + verticesB.Length == normalsAB.Length);
        normal = Vector2.zero;
        depth = float.MaxValue;
        foreach (Vector2 n in normalsAB)
        {
            n.Normalize(); 
            MinMax minMaxA = Collisions.ProjectVerticesMinMax(n, verticesA);
            MinMax minMaxB = Collisions.ProjectVerticesMinMax(n, verticesB);
            if (minMaxA.min >= minMaxB.max || minMaxB.min > minMaxA.max)
            {
                // serparation
                return false;
            }
            float axisDepth = Mathf.Min(minMaxB.max - minMaxA.min, minMaxA.max - minMaxB.min);
            if (axisDepth < depth)
            {
                depth = axisDepth;
                normal = n;
            }
        }

        Vector2 centerA = GeometricCenter(verticesA);
        Vector2 centerB = GeometricCenter(verticesB);

        Vector2 direction = centerB - centerA;
        if (Vector2.Dot(direction, normal) < 0)
        {
            normal = -normal;
        }

        return true;
    }

    public static Vector2 GeometricCenter(Vector2[] vertices)
    {
        Vector2 mean = Vector2.zero;
        foreach(Vector2 v in vertices)
        {
            mean += v;
        }    

        return mean / vertices.Length;
    }

    public static Vector2[] GetNormals(Vector2[] vertices)
    {
        Vector2[] normals = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector2 va = vertices[i];
            Vector2 vb = vertices[(i + 1)%vertices.Length];
            Vector2 edge = vb - va;
            Vector2 axis = Quaternion.AngleAxis(90, Vector3.forward) * edge;
            normals[i] = axis.normalized;
        }
        return normals;
    }

    private static MinMax ProjectCircle(Vector2 center, float radius, Vector2 normal)
    {
        MinMax ans = new MinMax();
        normal.Normalize();
        Vector2 dirAndRad = normal * radius;
        Vector2 p1 = center + dirAndRad;
        Vector2 p2 = center - dirAndRad;

        ans.min = Vector2.Dot(p1, normal);
        ans.max = Vector2.Dot(p2, normal);

        if (ans.min > ans.max)
        {
            float t = ans.min;
            ans.min = ans.max;
            ans.max = t;
        }

        return ans;
    }

    private static MinMax ProjectVerticesMinMax(Vector2 normal, Vector2[] vertices)
    {
        MinMax minMax = new MinMax { min = float.MaxValue, max = float.MinValue };
        foreach (Vector2 p in vertices)
        {
            float proj = Vector2.Dot(p, normal);
            if (proj < minMax.min) minMax.min = proj;
            if (proj > minMax.max) minMax.max = proj;
        }
        return minMax;
    }

    private static int FindClosestPointOnPolygon(Vector2 center, Vector2[] vertices)
    {
        int result = -1;
        float minSqDistance = float.MaxValue;

        for(int i = 0; i < vertices.Length; i++)
        {
            Vector2 v = vertices[i];
            float sqDistance = Vector2.SqrMagnitude(v - center);
            if (sqDistance < minSqDistance)
            {
                minSqDistance = sqDistance;
                result = i;
            }
        }

        return result;
    }
}

public struct BoxVertices
{
    public readonly Vector2 center;
    public readonly Vector2 topLeft;
    public readonly Vector2 bottomRight;
    public readonly Vector2 topRight;
    public readonly Vector2 bottomLeft;
    public BoxVertices(Vector2 _center, Vector2 _size, Quaternion _rot)
    {
        this.center = _center;
        this.topLeft = _rot * (new Vector2(-_size.x / 2, _size.y / 2));
        this.topRight = _rot * (new Vector2(_size.x / 2, _size.y / 2));
        this.bottomLeft = _rot * (new Vector2(-_size.x / 2, -_size.y / 2));
        this.bottomRight = _rot * (new Vector2(_size.x / 2, -_size.y / 2));

        this.topLeft += _center;
        this.topRight += _center;
        this.bottomLeft += _center;
        this.bottomRight += _center;

    }
}

public readonly struct CollisionManifold
{
    public readonly Shape a;
    public readonly Shape b;
    public readonly Vector2 normal;
    public readonly float depth;
    public readonly Vector2[] contacts;

    public CollisionManifold(Shape _a, Shape _b, Vector2 _n, float _d, Vector2[] _cs)
    {
        this.a = _a;
        this.b = _b;
        this.normal = _n;
        this.depth = _d;
        this.contacts = _cs;
    }
}