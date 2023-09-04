using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Collisions 
{
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
