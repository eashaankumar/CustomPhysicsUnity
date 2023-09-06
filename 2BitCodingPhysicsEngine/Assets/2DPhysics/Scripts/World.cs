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
    public List<Vector2> ContactPointsList;

    public World()
    {
        gravity = Vector2.down * 9.81f;
        shapes = new HashSet<Shape>();
        contactList = new List<CollisionManifold>();
        this.ContactPointsList = new List<Vector2>();
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
            DoCollisions();
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

    void DoCollisions()
    {
        this.contactList.Clear();
        this.ContactPointsList.Clear();

        Shape[] shapesTemp = new Shape[shapes.Count];
        shapes.CopyTo(shapesTemp);

        Vector2 normal;
        float depth;

        for (int i = 0; i < shapesTemp.Length - 1; i++)
        {
            Shape a = shapesTemp[i];
            AABB a_aabb = a.body.GetAABB();
            for (int j = i + 1; j < shapesTemp.Length; j++)
            {
                Shape b = shapesTemp[j];
                AABB b_aabb = b.body.GetAABB();

                if (a.body.isStatic && b.body.isStatic) continue;

                if (!Collisions.IntersectAABB(a_aabb, b_aabb))
                {
                    continue;
                }
                
                if (Collisions.Collide(a, b, out normal, out depth))
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

                    Vector2[] contacts;
                    Collisions.FindContactPoints(a.body, b.body, out contacts);
                    CollisionManifold manifold = new CollisionManifold(a, b, normal, depth, contacts);
                    this.contactList.Add(manifold);

                    ContactPointsList.AddRange(contacts);

                    a.OnCollision(b);
                    b.OnCollision(a);
                }
            }

        }

        for(int i = 0; i < this.contactList.Count; i++)
        {
            CollisionManifold man = contactList[i];
            ResolveCollision(in man);
        }
    }

    void ResolveCollision(in CollisionManifold man)
    {
        Vector2 relVel = man.b.body.linearVelocity - man.a.body.linearVelocity;
        float restitution = Mathf.Min(man.a.body.restitution, man.b.body.restitution);
        float impulseMag = -(1 + restitution) * Vector2.Dot(relVel, man.normal);
        impulseMag /= (man.a.body.InvMass + man.b.body.InvMass);
        //disregard rotation and friction

        man.a.body.linearVelocity -= impulseMag * man.a.body.InvMass * man.normal;
        man.b.body.linearVelocity += impulseMag * man.b.body.InvMass * man.normal;
    }


}
