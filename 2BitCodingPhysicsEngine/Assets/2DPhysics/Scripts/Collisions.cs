using System.Collections;
using System.Collections.Generic;
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

    public static bool Collide(Shape a, Shape b, out Vector2 normal, out float depth)
    {
        depth = 0;
        normal = Vector2.zero;
        if (a.body.type == ShapeType.Circle && b.body.type == ShapeType.Circle)
        {
            return Collisions.IntersetCircles(a.body.position, a.body.radius, b.body.position, b.body.radius, out normal, out depth);
        }
        else if (IsPolygon(a.body.type) && IsPolygon(b.body.type))
        {
            BoxVertices PolyGonA = new BoxVertices(a.body.position, a.body.size, a.body.rotation);
            BoxVertices PolyGonB = new BoxVertices(b.body.position, b.body.size, b.body.rotation);
            Vector2[] verticesA = GetCounterClockwiseVertices(PolyGonA);
            Vector2[] verticesB = GetCounterClockwiseVertices(PolyGonB);
            Vector2[] normalsA = Collisions.GetNormals(verticesA);
            Vector2[] normalsB = Collisions.GetNormals(verticesB);

            Vector2[] z = new Vector2[normalsA.Length + normalsB.Length];
            normalsA.CopyTo(z, 0);
            normalsB.CopyTo(z, normalsA.Length);

            return Collisions.IntersectPolygons(verticesA, verticesB, z, out normal, out depth);
        }
        else if (IsPolygon(a.body.type) && b.body.type == ShapeType.Circle)
        {
            BoxVertices PolyGonA = new BoxVertices(a.body.position, a.body.size, a.body.rotation);
            Vector2[] verticesA = GetCounterClockwiseVertices(PolyGonA);
            Vector2[] normalsA = Collisions.GetNormals(verticesA);
            return Collisions.IntersectCirclePolygon(b.body.position, b.body.radius, verticesA, normalsA, out normal, out depth);
        }
        else if (IsPolygon(b.body.type) && a.body.type == ShapeType.Circle)
        {
            BoxVertices PolyGonB = new BoxVertices(b.body.position, b.body.size, b.body.rotation);
            Vector2[] verticesB = GetCounterClockwiseVertices(PolyGonB);
            Vector2[] normalsB = Collisions.GetNormals(verticesB);
            if (Collisions.IntersectCirclePolygon(a.body.position, a.body.radius, verticesB, normalsB, out normal, out depth))
            {
                normal = -normal;
                return true;
            }
        }
        return false;
    }

    public static void FindContactPoints(Body a, Body b, out Vector2[] contacts)
    {
        contacts = new Vector2[0];
        if (a.type == ShapeType.Circle && b.type == ShapeType.Circle)
        {
            Vector2 contact;
            Collisions.FindContactPoint(a.position, a.radius, b.position, out contact);
            contacts = new Vector2[1] { contact };
        }
        else if (a.type == ShapeType.Box && b.type == ShapeType.Box)
        {
            BoxVertices verticesA = new BoxVertices(a.position, a.size, a.rotation);
            BoxVertices verticesB = new BoxVertices(b.position, b.size, b.rotation);
            Collisions.FindContactPoint(GetCounterClockwiseVertices(verticesA),
                                        GetCounterClockwiseVertices(verticesB), out Vector2 cp1, out Vector2 cp2, out int contactCount);
            contacts = new Vector2[contactCount];
            if (contactCount > 0)
                contacts[0] = cp1;
            if (contactCount > 1)
                contacts[1] = cp2;
        }
        else if(a.type == ShapeType.Circle && b.type == ShapeType.Box)
        {
            Vector2 contact;
            BoxVertices vertices = new BoxVertices(b.position, b.size, b.rotation);
            Collisions.FindContactPoint(a.position, a.radius, GetCounterClockwiseVertices(vertices), out contact);
            contacts = new Vector2[1] { contact };
        }
        else if (a.type == ShapeType.Box && b.type == ShapeType.Circle)
        {
            Vector2 contact;
            BoxVertices vertices = new BoxVertices(a.position, a.size, a.rotation);
            Collisions.FindContactPoint(b.position, b.radius, GetCounterClockwiseVertices(vertices), out contact);
            contacts = new Vector2[1] { contact };
        }
    }

    public static void PointSegmentDistance(Vector2 p, Vector2 a, Vector2 b, out float distanceSq, out Vector2 closestPoint)
    {
        Vector2 ab = b - a;
        Vector2 ap = p - a;

        float proj = Vector2.Dot(ap, ab);
        float abLenSq = ab.sqrMagnitude;
        float d = proj / abLenSq;

        if (d <= 0)
        {
            closestPoint = a;
        }
        else if (d >= 1f)
        {
            closestPoint = b;
        }
        else
        {
            closestPoint = a + ab * d;
        }
        distanceSq = Vector2.SqrMagnitude(p - closestPoint);
    }

    public static void FindContactPoint(Vector2[] verticesA, Vector2[] verticesB, out Vector2 cp1, out Vector2 cp2, out int contactCount)
    {
        cp1 = Vector2.zero;
        cp2 = Vector2.zero;
        contactCount = 0;

        float minDisSq = float.MaxValue;

        for (int i = 0; i < verticesA.Length; i++)
        {
            Vector2 p = verticesA[i];
            for(int j = 0; j < verticesB.Length; j++)
            {
                Vector2 va = verticesB[j];
                Vector2 vb = verticesB[(j + 1) % verticesB.Length];

                Collisions.PointSegmentDistance(p, va, vb, out float distSq, out Vector2 cp);

                if (Collisions.NearlyEqual(distSq, minDisSq))
                {
                    if (!Collisions.NearlyEqual(cp,cp1))
                    {
                        cp2 = cp;
                        contactCount = 2;
                    }
                }
                if (distSq < minDisSq)
                {
                    minDisSq = distSq;
                    contactCount = 1;
                    cp1 = cp;
                }
            }
        }

        for (int i = 0; i < verticesB.Length; i++)
        {
            Vector2 p = verticesB[i];
            for (int j = 0; j < verticesA.Length; j++)
            {
                Vector2 va = verticesA[j];
                Vector2 vb = verticesA[(j + 1) % verticesA.Length];

                Collisions.PointSegmentDistance(p, va, vb, out float distSq, out Vector2 cp);

                if (Collisions.NearlyEqual(distSq, minDisSq))
                {
                    if (!Collisions.NearlyEqual(cp, cp1))
                    {
                        cp2 = cp;
                        contactCount = 2;
                    }
                }
                if (distSq < minDisSq)
                {
                    minDisSq = distSq;
                    contactCount = 1;
                    cp1 = cp;
                }
            }
        }

    }

    static readonly float VerySmallAmount = 1e-3f;
    public static bool NearlyEqual(float a, float b)
    {
        return Mathf.Abs(a - b) < VerySmallAmount;
    }

    public static bool NearlyEqual(Vector2 a, Vector2 b)
    {
        return NearlyEqual(a.x, b.x) && NearlyEqual(a.y, b.y);
    }

    public static void FindContactPoint(Vector2 center, float radius, Vector2[] vertices, out Vector2 cp)
    {
        cp = Vector2.zero;
        Vector2 polygonCenter = GeometricCenter(vertices);

        float minDistSq = float.MaxValue;

        for(int i = 0; i < vertices.Length; i++)
        {
            Vector2 va = vertices[i];
            Vector2 vb = vertices[(i + 1) % vertices.Length];
            Collisions.PointSegmentDistance(center, va, vb, out float distanceSq, out Vector2 contact);
            if (distanceSq < minDistSq)
            {
                minDistSq = distanceSq;
                cp = contact;
            }
        }
    }

    public static void FindContactPoint(Vector2 centerA, float radiusA, Vector2 centerB, out Vector2 cp)
    {
        Vector2 ab = (centerB - centerA).normalized;
        cp = centerA + ab * radiusA;
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

    public static bool IsPolygon(ShapeType type)
    {
        return type == ShapeType.Box;
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

    public static Vector2[] GetCounterClockwiseVertices(BoxVertices vertices)
    {
        return new Vector2[] { vertices.topLeft, vertices.topRight, vertices.bottomRight, vertices.bottomLeft };
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