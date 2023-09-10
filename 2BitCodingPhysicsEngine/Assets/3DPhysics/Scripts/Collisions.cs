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
            float3[] verticesA = PolyGonA.GetVertices();
            float3[] verticesB = PolyGonB.GetVertices();
            float3[] aAxes = PolyGonA.GetAxis(); // 3
            float3[] bAxes = PolyGonB.GetAxis(); // 3 

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
        else if (a.type == BodyType.SPHERE && b.type == BodyType.BOX)
        {
            return Collisions.IntersectSpherePolygon(a.position, a.size.x, b.position, b.size, b.rotation, out normal, out depth);
        }
        else if (a.type == BodyType.BOX && b.type == BodyType.SPHERE)
        {
            return Collisions.IntersectSpherePolygon(b.position, b.size.x, a.position, a.size, a.rotation, out normal, out depth);
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
    
    public static bool IntersectSpherePolygon(float3 sphereCenter, float sphereRadius, float3 boxCenter, float3 boxSize, quaternion boxRot, out float3 normal, out float depth)
    {
        normal = float3.zero;
        depth = 0;
        float3 sphereCenterProjOnBox = Collisions.ClosestPointOnBox(boxCenter, boxRot, boxSize, sphereCenter, out float3 localPoint);
        if (Utils.CloseEnough(sphereCenterProjOnBox, sphereCenter))
        {
            BoxVertices PolyGon = new BoxVertices(boxCenter, boxSize, boxRot);
            float3[] cubeAxises = PolyGon.GetAllAxis();
            Collisions.ClosestDirection(cubeAxises, sphereCenterProjOnBox - boxCenter, out int closestNormIndex);
            if (closestNormIndex < 0) throw new System.Exception("No valid box face found");
            normal = cubeAxises[closestNormIndex];
            depth = sphereRadius;
            return true;
        }
        float dis = math.length(sphereCenterProjOnBox - sphereCenter);
        if (dis < sphereRadius)
        {
            normal = math.normalize(sphereCenterProjOnBox - sphereCenter);
            depth = dis;
            Debug.Log(sphereCenterProjOnBox - sphereCenter + " " + normal + " " + depth);
            return true;
        }
        return false;
    }

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
            BoxVertices abox = new BoxVertices(a.position, a.size, a.rotation);
            BoxVertices bbox = new BoxVertices(b.position, b.size, b.rotation);
            List<float3> contactsOnA, contactsOnB;
            FindContactPoint(a.position, a.size, a.rotation, bbox.GetVertices(), out contactsOnA);
            FindContactPoint(b.position, b.size, b.rotation, abox.GetVertices(), out contactsOnB);
            for(int i = 0; i < contactsOnA.Count; i++)
            {
                contacts.Add(contactsOnA[i]);
            }
            for (int i = 0; i < contactsOnB.Count; i++)
            {
                contacts.Add(contactsOnB[i]);
            }
        }
    }

    public static void FindContactPoint(float3 cubeACenter, float3 cubeASize, quaternion cubeARot, float3[] verticesB, out List<float3> contacts)
    {
        contacts = new List<float3>();
        List<(float3, float)> potentialContactPoint = new List<(float3, float)>();
        //float minDisSq = float.MaxValue;
        foreach (float3 vertexB in verticesB)
        {
            float3 closestPointToA = ClosestPointOnBox(cubeACenter, cubeARot, cubeASize, vertexB, out float3 localPoint);
            float disSq = math.lengthsq(closestPointToA - vertexB);
            potentialContactPoint.Add((closestPointToA, disSq));
            /*if (disSq < minDisSq)
            {
                minDisSq = disSq;
                contacts.Add(closestPointToA);
                //if (contacts.Count > 4) contacts.RemoveAt(contacts.Count - 1);
            }*/
        }
        potentialContactPoint.Sort((x, y) => x.Item2.CompareTo(y.Item2));
        for(int i = 0; i < potentialContactPoint.Count; i++)
        {
            if (i > 4) break;
            contacts.Add(potentialContactPoint[i].Item1);
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

    public static void ClosestDirection(float3[] directions, float3 normal, out int closestPlaneIndex)
    {
        normal = math.normalize(normal);
        closestPlaneIndex = -1;
        float minDotProd = float.MaxValue;
        for (int i = 0; i < directions.Length; i++)
        {
            float dot = 1 - math.dot(math.normalize(directions[i]), normal);
            if (dot < minDotProd)
            {
                minDotProd = dot;
                closestPlaneIndex = i;
            }
        }
    }

    public static float ClosestPointOnPlane(Plane plane, float3 point)
    {
        // https://gdbooks.gitbooks.io/3dcollisions/content/Chapter1/closest_point_on_plane.html
        // This works assuming plane.Normal is normalized, which it should be
        float distance = math.dot(plane.normal, point) - math.length(plane.center);
        // If the plane normal wasn't normalized, we'd need this:
        // distance = distance / DOT(plane.Normal, plane.Normal);

        return distance;
    }

    public static float3 ClosestPointOnBox(float3 center, quaternion rot, float3 size, float3 point, out float3 localPoint)
    {
        localPoint = Utils.WorldToLocal(center, rot, point);
        localPoint.x = math.clamp(localPoint.x, -size.x / 2f, size.x / 2f);
        localPoint.y = math.clamp(localPoint.y, -size.y / 2f, size.y / 2f);
        localPoint.z = math.clamp(localPoint.z, -size.z / 2f, size.z / 2f);
        //localFace = new float3(math.sign(localPoint.x), math.sign(localPoint.y), math.sign(localPoint.z));
        
        return Utils.LocalToWorld(center, rot, localPoint);
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

    public readonly Plane topP, bottomP, leftP, rightP, frontP, backP;

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

        this.topP = new Plane(center + topN * _size.y / 2, _size.xz, topN);
        this.bottomP = new Plane(center - topN * _size.y / 2, _size.xz, -topN);
        this.rightP = new Plane(center + rightN * _size.x / 2, _size.yz, rightN);
        this.leftP = new Plane(center - rightN * _size.x / 2, _size.yz, -rightN);
        this.frontP = new Plane(center + frontN * _size.z / 2, _size.xy, frontN);
        this.backP = new Plane(center - frontN * _size.z / 2, _size.xy, -frontN);

    }

    public float3[] GetVertices()
    {
        return new float3[] { bottomLeftBack, bottomLeftFront, bottomRightBack, bottomRightFront,
                              topLeftBack, topLeftFront, topRightBack, topRightFront};
    }

    public float3[] GetAxis()
    {
        return new float3[]
        {
            rightN, topN, frontN
        };
    }

    public float3[] GetAllAxis()
    {
        return new float3[]
        {
            rightN, topN, frontN,
            -rightN, -topN, -frontN
        };
    }
}

public struct Triangle
{
    public readonly float3 a;
    public readonly float3 b;
    public readonly float3 c;
    public readonly float3 ab;
    public readonly float3 ac;

    public readonly float3 normal;
    public Triangle(float3 _a, float3 _b, float3 _c)
    {
        a = _a;
        b = _b;
        c = _c;

        ab = b - a;
        ac = c - a;
        normal = math.normalize(math.cross(math.normalize(ab), math.normalize(ac)));
    }
}

public struct Plane
{
    public readonly float3 center;
    public readonly float2 size;
    public readonly float3 normal;

    public Plane(float3 _center, float2 _size, float3 _normal)
    {
        center = _center;
        size = _size;
        normal = math.normalize(_normal);
    }
}