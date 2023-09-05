using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Collisions 
{

    struct MinMax
    {
        public float min, max;
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

    public static bool IntersectPolygons(Vector2[] verticesA, Vector2[] verticesB, Vector2[] normals, out Vector2 normal, out float depth)
    {
        Debug.Assert(verticesA.Length + verticesB.Length == normals.Length);
        normal = Vector2.zero;
        depth = 0;
        foreach(Vector2 n in normals)
        {
            n.Normalize();
            MinMax minMaxA = Collisions.ProjectVerticesMinMax(n, verticesA);
            MinMax minMaxB = Collisions.ProjectVerticesMinMax(n, verticesB);
            if (minMaxA.min >= minMaxB.max || minMaxB.min > minMaxA.max)
            {
                // serparation
                return false;
            }
        }
        return true;
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
            normals[i] = axis;
        }
        return normals;
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
