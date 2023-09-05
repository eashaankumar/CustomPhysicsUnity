using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class World
{
    public static readonly float minBodySize = 0.01f * 0.01f; // area
    public static readonly float maxBodySize = 64f * 64f;

    public static readonly float MinDensity = 0.5f; // g/cm^3 (water is 1)
    public static readonly float MaxDensity = 21.4f;

    HashSet<Shape> shapes;
    private Vector2 gravity;

    public World()
    {
        gravity = Vector2.down * 9.81f;
        shapes = new HashSet<Shape>();
    }

    public void AddBody(Shape body)
    {
        shapes.Add(body);
    }

    public bool RemoveBody(Shape body)
    {
        return shapes.Remove(body);
    }

    public void Step(float time)
    {
        ResolveNullShapes();
        ResolveCollisions();
        foreach (Shape shape in shapes)
        {

        }
    }

    void ResolveNullShapes()
    {
        shapes.RemoveWhere(s => s == null);
        /*for (int i = shapes.Count - 1; i >= 0; i--)
        {
            Shape shape = shapes[i];
            if (shape == null) shapes.RemoveAt(i);

        }*/
    }

    void ResolveCollisions()
    {
        Shape[] shapesTemp = new Shape[shapes.Count];
        shapes.CopyTo(shapesTemp);

        for (int i = 0; i < shapesTemp.Length - 1; i++)
        {
            Shape a = shapesTemp[i];
            for (int j = i + 1; j < shapesTemp.Length; j++)
            {
                Shape b = shapesTemp[j];
                if (a.body.type == ShapeType.Circle && b.body.type == ShapeType.Circle)
                {
                    Vector2 normal;
                    float depth;
                    if (Collisions.IntersetCircles(a.body.position, a.body.radius, b.body.position, b.body.radius, out normal, out depth))
                    {
                        a.body.Move(-normal * depth * 0.5f);
                        b.body.Move(normal * depth * 0.5f);
                        a.OnCollision(b);
                        b.OnCollision(a);
                    }
                }
                else if (IsPolygon(a.body.type) && IsPolygon(b.body.type))
                {
                    Vector2 normal;
                    float depth;
                    BoxVertices PolyGonA = new BoxVertices(a.body.position, a.body.size, a.body.rotation);
                    BoxVertices PolyGonB = new BoxVertices(b.body.position, b.body.size, b.body.rotation);
                    Vector2[] verticesA = GetCounterClockwiseVertices(PolyGonA);
                    Vector2[] verticesB = GetCounterClockwiseVertices(PolyGonB);
                    Vector2[] normalsA = Collisions.GetNormals(verticesA);
                    Vector2[] normalsB = Collisions.GetNormals(verticesB);

                    Vector2[] z = new Vector2[normalsA.Length + normalsB.Length];
                    normalsA.CopyTo(z, 0);
                    normalsB.CopyTo(z, normalsA.Length);

                    if (Collisions.IntersectPolygons(verticesA, verticesB, z, out normal, out depth))
                    {
                        a.body.Move(-normal * depth * 0.5f);
                        b.body.Move(normal * depth * 0.5f);
                        a.OnCollision(b);
                        b.OnCollision(a);
                    }
                }
                else if (IsPolygon(a.body.type) && b.body.type == ShapeType.Circle)
                {
                    Vector2 normal;
                    float depth;
                    BoxVertices PolyGonA = new BoxVertices(a.body.position, a.body.size, a.body.rotation);
                    Vector2[] verticesA = GetCounterClockwiseVertices(PolyGonA);
                    Vector2[] normalsA = Collisions.GetNormals(verticesA);
                    if (Collisions.IntersectCirclePolygon(b.body.position, b.body.radius, verticesA, normalsA, out normal, out depth))
                    {
                        a.body.Move(-normal * depth * 0.5f);
                        b.body.Move(normal * depth * 0.5f);
                        a.OnCollision(b);
                        b.OnCollision(a);
                    }
                }
                else if (IsPolygon(b.body.type) && a.body.type == ShapeType.Circle)
                {
                    Vector2 normal;
                    float depth;
                    BoxVertices PolyGonB = new BoxVertices(b.body.position, b.body.size, b.body.rotation);
                    Vector2[] verticesB = GetCounterClockwiseVertices(PolyGonB);
                    Vector2[] normalsB = Collisions.GetNormals(verticesB);
                    if (Collisions.IntersectCirclePolygon(a.body.position, a.body.radius, verticesB, normalsB, out normal, out depth))
                    {
                        b.body.Move(-normal * depth * 0.5f);
                        a.body.Move(normal * depth * 0.5f);
                        b.OnCollision(a);
                        a.OnCollision(b);
                    }
                }
            }

        }
    }

    Vector2[] GetCounterClockwiseVertices(BoxVertices vertices)
    {
        return new Vector2[] { vertices.topLeft, vertices.topRight, vertices.bottomRight, vertices.bottomLeft };
    }

    bool IsPolygon(ShapeType type)
    {
        return type == ShapeType.Box;
    }
}
