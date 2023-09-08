using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public interface IBody
{
    public void Step(float dt);
}

public struct SphereBody: IBody
{
    public float3 position;
    public quaternion rotation;
    public float radius;

    public float3 velocity;
    public float3 angularVelocityRadians;
    public float3 force;

    public readonly float mass;
    public readonly float invMass;
    public readonly float density;

    private readonly float inertia, inverseInertia;

    public float restitution; // bouncy
    public float staticFriction, dynamicFriction;


    public bool isStatic;

    public SphereBody(float _r, bool _static, float _d, float _restitution, float _sf, float _df)
    {
        position = float3.zero;
        rotation = quaternion.identity;
        radius = _r;
        velocity = float3.zero;
        angularVelocityRadians = float3.zero;
        force = float3.zero;
        density = _d;
        isStatic = _static;
        restitution = _restitution;
        staticFriction = _sf;
        dynamicFriction = _df;

        float vol = (4f / 3f) * math.PI * radius * radius * radius;
        mass = density * vol;

        invMass = 1f / mass;

        // solid sphere
        inertia = (2f / 5f) * mass * radius * radius;
        inverseInertia = 1 / inertia;
    }

    public void Step(float dt)
    {
        if (!isStatic)
        {
            this.velocity += this.force / this.mass * dt;
            this.position += this.velocity * dt;
            this.rotation = math.mul(this.rotation, quaternion.EulerZXY(this.angularVelocityRadians * dt));
        }
        else
        {
            this.velocity = float3.zero;
            this.angularVelocityRadians = 0;
        }
        this.force = float3.zero;
    }
}
