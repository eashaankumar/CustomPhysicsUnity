using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public interface IWorld
{
    public void Tick(float dt, float substeps);
}

public struct World : System.IDisposable, IWorld
{
    internal NativeHashMap<int, SphereBody> _bodies;
    NativeList<float3> _contactPointsList;
    NativeList<(int, int)> _contactPairs;
    Unity.Mathematics.Random _random;
    float3 _gravity;
    public World(Allocator worldAllocator, int maxBodies, uint seed, float3 _gravity)
    {
        _bodies = new NativeHashMap<int, SphereBody>(maxBodies, worldAllocator);
        _random = new Unity.Mathematics.Random(seed);
        _contactPointsList = new NativeList<float3>(worldAllocator);
        _contactPairs = new NativeList<(int, int)>(worldAllocator);
        this._gravity = _gravity;
    }

    public void Dispose()
    {
        if (_bodies.IsCreated) _bodies.Dispose();
        if (_contactPointsList.IsCreated) _contactPointsList.Dispose();
        if (_contactPairs.IsCreated) _contactPairs.Dispose();
    }

    public bool AddSphere(SphereBody b, out int id)
    {
        id = _random.NextInt();
        return _bodies.TryAdd(id, b);
    }

    public void Tick(float dt, float substeps)
    {
        int before = _bodies.Count();
        float dtSub = dt / substeps;
        NativeArray<int> keys = _bodies.GetKeyArray(Allocator.Temp);
        for (int s = 0; s < substeps; s++)
        {
            StepBodiesNoRemove(dtSub, keys);
            BroadPhase(keys);
            NarrowPhase();
        }
        keys.Dispose();
        if (_bodies.Count() != before)
        {
            throw new System.Exception("cannot add/remove bodies here");
        }
    }

    void StepBodiesNoRemove(float dt, NativeArray<int> keys)
    {
        foreach (int key in keys)
        {
            SphereBody body = _bodies[key];
            body.AddForce(body.mass * _gravity);
            body.Step(dt);
            _bodies[key] = body;
        }
    }

    void BroadPhase(NativeArray<int> keys)
    {
        //this.contactList.Clear();
        this._contactPointsList.Clear();
        _contactPairs.Clear();

        for (int i = 0; i < keys.Length - 1; i++)
        {
            int keyA = keys[i];
            SphereBody a = _bodies[keyA];
            AABB a_aabb = a.AABB();
            for (int j = i + 1; j < keys.Length; j++)
            {
                int keyB = keys[j];
                SphereBody b = _bodies[keyB];
                AABB b_aabb = b.AABB();

                if (a.isStatic && b.isStatic) continue;

                if (!Collisions.IntersectAABB(a_aabb, b_aabb))
                {
                    continue;
                }

                _contactPairs.Add((keyA, keyB));
            }
        }
    }

    void NarrowPhase()
    {
        for (int i = 0; i < _contactPairs.Length; i++)
        {
            float3 normal;
            float depth;
            (int, int) pair = _contactPairs[i];
            SphereBody a = _bodies[pair.Item1];
            SphereBody b = _bodies[pair.Item2];
            if (Collisions.Collide(a, b, out normal, out depth))
            {

                if (a.isStatic)
                {
                    b.Move(normal * depth * 1f);
                }
                else if (b.isStatic)
                {
                    a.Move(-normal * depth * 1);
                }
                else
                {
                    a.Move(-normal * depth * 0.5f);
                    b.Move(normal * depth * 0.5f);
                }

                NativeList<float3> contacts;
                Collisions.FindContactPoints(a, b, out contacts);
                CollisionManifold manifold = new CollisionManifold(pair.Item1, pair.Item2, normal, depth);
                //this._contactList.Add(manifold);

                for(int c = 0; c < contacts.Length; c++)
                {
                    _contactPointsList.Add(contacts[c]);
                }
                contacts.Dispose();

                //a.OnCollision(b);
                //b.OnCollision(a);

                //ResolveCollisionWithRotationAndFriction(in manifold);
                ResolveCollisionBasic(ref a, ref b, normal, depth);

                _bodies[pair.Item1] = a;
                _bodies[pair.Item2] = b;
                
                
            }
        }
    }

    void ResolveCollisionBasic(ref SphereBody a, ref SphereBody b, float3 normal, float depth)
    {
        float3 relVel = b.velocity - a.velocity;
        float restitution = Mathf.Min(a.restitution, b.restitution);
        float impulseMag = -(1 + restitution) * math.dot(relVel, normal);
        impulseMag /= (a.invMass + b.invMass);
        //disregard rotation and friction
        float3 impulse = impulseMag * normal;

        a.velocity -= impulse * a.invMass;
        b.velocity += impulse * b.invMass;
    }

}
