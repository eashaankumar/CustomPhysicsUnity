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
    internal NativeHashMap<int, Body> _bodies;
    internal NativeList<float3> _contactPointsList;
    NativeList<(int, int)> _contactPairs;
    Unity.Mathematics.Random _random;
    float3 _gravity;
    public World(Allocator worldAllocator, int maxBodies, uint seed, float3 _gravity)
    {
        _bodies = new NativeHashMap<int, Body>(maxBodies, worldAllocator);
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

    public bool AddBody(Body b, out int id)
    {
        id = _random.NextInt();
        return _bodies.TryAdd(id, b);
    }

    public void Tick(float dt, float substeps)
    {
        int before = _bodies.Count();
        float dtSub = dt / substeps;
        NativeArray<int> keys = _bodies.GetKeyArray(Allocator.Temp);
        this._contactPointsList.Clear();
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
            Body body = _bodies[key];
            body.AddForce(body.mass * _gravity);
            body.Step(dt);
            _bodies[key] = body;
        }
    }

    void BroadPhase(NativeArray<int> keys)
    {
        _contactPairs.Clear();
        for (int i = 0; i < keys.Length - 1; i++)
        {
            int keyA = keys[i];
            Body a = _bodies[keyA];
            AABB a_aabb = a.AABB();
            for (int j = i + 1; j < keys.Length; j++)
            {
                int keyB = keys[j];
                Body b = _bodies[keyB];
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
            Body a = _bodies[pair.Item1];
            Body b = _bodies[pair.Item2];
            if (Collisions.Collide(a, b, out normal, out depth))
            {
                Vector3 direction = b.position - a.position;
                if (Vector3.Dot(direction, normal) < 0)
                {
                    normal = -normal;
                }
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

                for(int c = 0; c < contacts.Length; c++)
                {
                    _contactPointsList.Add(contacts[c]);
                }
                contacts.Dispose();
                
                ResolveCollisionBasic(ref a, ref b, normal, depth);

                _bodies[pair.Item1] = a;
                _bodies[pair.Item2] = b;
                
                
            }
        }
    }

    void ResolveCollisionBasic(ref Body a, ref Body b, float3 normal, float depth)
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
