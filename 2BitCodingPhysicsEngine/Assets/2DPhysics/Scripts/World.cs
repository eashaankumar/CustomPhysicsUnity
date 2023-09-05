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
    private List<CollisionManifold> contactList;

    public World()
    {
        gravity = Vector2.down * 9.81f;
        shapes = new HashSet<Shape>();
        contactList = new List<CollisionManifold>();
    }

    public void AddBody(Shape body)
    {
        shapes.Add(body);
    }

    public bool RemoveBody(Shape body)
    {
        return shapes.Remove(body);
    }

    public void Step(float dt, int iterations)
    {
        ResolveNullShapes();
        float dtIter = dt / iterations;
        for (int i = 0; i < iterations; i++)
        {
            ResolveCollisions();
            StepBodies(dtIter);
        }
    }

    void StepBodies(float dt)
    {
        foreach(Shape body in shapes)
        {
            body.body.AddForce(body.body.Mass * gravity);
            body.body.Step(dt);
        }
    }

    void ResolveNullShapes()
    {
        shapes.RemoveWhere(s => s == null);
    }

    void ResolveCollisions()
    {
        Shape[] shapesTemp = new Shape[shapes.Count];
        shapes.CopyTo(shapesTemp);

        Vector2 normal;
        float depth;

        for (int i = 0; i < shapesTemp.Length - 1; i++)
        {
            Shape a = shapesTemp[i];
            for (int j = i + 1; j < shapesTemp.Length; j++)
            {
                Shape b = shapesTemp[j];
                if (a.body.isStatic && b.body.isStatic) continue;
                
                if (Collide(a, b, out normal, out depth))
                {
                  
                    if (a.body.isStatic)
                    {
                        b.body.Move(normal * depth * 1f);
                    }
                    else if (b.body.isStatic)
                    {
                        a.body.Move(-normal * depth * 1);
                    }
                    else
                    {
                        a.body.Move(-normal * depth * 0.5f);
                        b.body.Move(normal * depth * 0.5f); 
                    }

                    CollisionManifold manifold = new CollisionManifold(a, b, normal, depth, new Vector2[] { });
                    this.contactList.Add(manifold);

                    a.OnCollision(b);
                    b.OnCollision(a);
                }
            }

        }

        for(int i = 0; i < this.contactList.Count; i++)
        {
            ResolveCollision(this.contactList[i]);
        }
    }

    void ResolveCollision(CollisionManifold man)
    {
        Vector2 relVel = man.b.body.linearVelocity - man.a.body.linearVelocity;
        float restitution = Mathf.Min(man.a.body.restitution, man.b.body.restitution);
        float impulseMag = -(1 + restitution) * Vector2.Dot(relVel, man.normal);
        impulseMag /= (man.a.body.InvMass + man.b.body.InvMass);
        //disregard rotation and friction

        man.a.body.linearVelocity -= impulseMag * man.a.body.InvMass * man.normal;
        man.b.body.linearVelocity += impulseMag * man.b.body.InvMass * man.normal;
    }

    bool Collide(Shape a, Shape b, out Vector2 normal, out float depth)
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

    Vector2[] GetCounterClockwiseVertices(BoxVertices vertices)
    {
        return new Vector2[] { vertices.topLeft, vertices.topRight, vertices.bottomRight, vertices.bottomLeft };
    }

    bool IsPolygon(ShapeType type)
    {
        return type == ShapeType.Box;
    }
}
