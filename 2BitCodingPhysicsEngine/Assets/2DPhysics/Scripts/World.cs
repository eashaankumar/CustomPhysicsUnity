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
    Vector2[] impulseList;
    Vector2[] raList;
    Vector2[] rbList;
    List<(int, int)> contactPairs;

    public World()
    {
        gravity = Vector2.down * 9.81f;
        shapes = new HashSet<Shape>();
        contactList = new List<CollisionManifold>();
        this.ContactPointsList = new List<Vector2>();
        contactPairs = new List<(int, int)>();
        impulseList = new Vector2[2];
        raList = new Vector2[2];
        rbList = new Vector2[2];
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
            Shape[] shapesTemp = new Shape[shapes.Count];
            shapes.CopyTo(shapesTemp);
            StepBodies(dtIter);
            BroadPhase(shapesTemp);
            NarrowPhase(shapesTemp);
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
    
    void BroadPhase(Shape[] shapesTemp)
    {
        this.contactList.Clear();
        this.ContactPointsList.Clear();
        contactPairs.Clear();

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

                contactPairs.Add((i, j));
            }

        }
    }

    void NarrowPhase(Shape[] shapesTemp)
    {
        for (int i = 0; i < contactPairs.Count; i++)
        {
            Vector2 normal;
            float depth;
            (int, int) pair = contactPairs[i];
            Shape a = shapesTemp[pair.Item1];
            Shape b = shapesTemp[pair.Item2];
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

                ResolveCollisionWithRotation(in manifold);
            }
        }
    }

    void ResolveCollisionBasic(in CollisionManifold man)
    {
        Vector2 relVel = man.b.body.linearVelocity - man.a.body.linearVelocity;
        float restitution = Mathf.Min(man.a.body.restitution, man.b.body.restitution);
        float impulseMag = -(1 + restitution) * Vector2.Dot(relVel, man.normal);
        impulseMag /= (man.a.body.InvMass + man.b.body.InvMass);
        //disregard rotation and friction
        Vector2 impulse = impulseMag * man.normal;

        man.a.body.linearVelocity -= impulse * man.a.body.InvMass;
        man.b.body.linearVelocity += impulse * man.b.body.InvMass;
    }

    void ResolveCollisionWithRotation(in CollisionManifold man)
    {
        float restitution = Mathf.Min(man.a.body.restitution, man.b.body.restitution);
        this.impulseList[0] = this.impulseList[1] = Vector2.zero;
        this.raList[0] = this.raList[1] = Vector2.zero;
        this.rbList[0] = this.rbList[1] = Vector2.zero;

        for (int i = 0; i < man.contacts.Length; i++)
        {
            Vector2 cp = man.contacts[i];
            Vector2 ra = cp - man.a.body.position;
            Vector2 rb = cp - man.b.body.position;

            raList[i] = ra;
            rbList[i] = rb;

            Vector2 raPerp = Quaternion.AngleAxis(90, Vector3.forward) * ra;
            Vector2 rbPerp = Quaternion.AngleAxis(90, Vector3.forward) * rb;

            Vector2 angularLinearVelA = raPerp * man.a.body.angularVelocityRadians;
            Vector2 angularLinearVelB = rbPerp * man.b.body.angularVelocityRadians;

            Vector2 relativeVelocity = (man.b.body.linearVelocity + angularLinearVelB) - 
                (man.a.body.linearVelocity + angularLinearVelA);

            float contactVelMag = Vector2.Dot(relativeVelocity, man.normal);
            if (contactVelMag > 0f)
            {
                // moving apart
                continue;
            }

            float raPerpDotN = Vector2.Dot(raPerp, man.normal);
            float rbPerpDotN = Vector2.Dot(rbPerp, man.normal);
            float denominator = man.a.body.InvMass + man.b.body.InvMass +
                (raPerpDotN * raPerpDotN) * man.a.body.InvInertia +
                (rbPerpDotN * rbPerpDotN) * man.b.body.InvInertia;

            float impulseMag = -(1 + restitution) * contactVelMag;
            impulseMag /= denominator;
            impulseMag /= (float)man.contacts.Length;
            //disregard rotation and friction
            Vector2 impulse = impulseMag * man.normal;

            impulseList[i] = impulse;
            // NOTE: impulse must be added in separate loop because we dont want to
            // change the velocities before visiting next contacts
        }

        for(int i = 0; i < man.contacts.Length; i++)
        {
            Vector2 impulse = impulseList[i];
            man.a.body.linearVelocity += -impulse * man.a.body.InvMass;
            man.a.body.angularVelocityRadians += -Cross(raList[i], impulse) * man.a.body.InvInertia;
            man.b.body.linearVelocity += impulse * man.b.body.InvMass;
            man.b.body.angularVelocityRadians += Cross(rbList[i], impulse) * man.b.body.InvInertia;
        }

    }

    public static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }


}
