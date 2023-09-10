using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public interface IBody
{
    public void Step(float dt);
    public AABB AABB();
}

public enum BodyType
{
    SPHERE, BOX, CYLINDER
}

public struct Body: IBody
{
    public float3 position;
    public quaternion rotation;
    public float3 size;
    public BodyType type;

    public float3 velocity;
    public float3 angularVelocityRadians;
    public float3 force;

    public Color color;

    public readonly float mass;
    public readonly float invMass;
    public readonly float density;

    private readonly float3x3 inertia, inverseInertia;

    public float restitution; // bouncy
    public float staticFriction, dynamicFriction;


    public bool isStatic;

    public Body(BodyType t, float3 _s, bool _static, float _d, float _restitution, float _sf, float _df)
    {
        position = float3.zero;
        rotation = quaternion.identity;
        size = _s;
        type = t;
        velocity = float3.zero;
        angularVelocityRadians = float3.zero;
        force = float3.zero;
        density = _d;
        isStatic = _static;
        restitution = Mathf.Clamp01(_restitution);
        staticFriction = _sf;
        dynamicFriction = _df;

        float vol = 0;
        if (t == BodyType.SPHERE)
        {
            vol = (4f / 3f) * math.PI * size.x * size.y * size.z;
        }
        else if (t == BodyType.BOX)
        {
            vol = size.x * size.y * size.z;
        }
        else
        {
            throw new System.InvalidOperationException("");
        }
        mass = density * vol;

        invMass = 1f / mass;

        if (t == BodyType.SPHERE)
        {
            // solid sphere
            inertia = float3x3.identity;
            inertia = new float3x3((2f / 5f) * mass * size.x * size.z, 0, 0,
                                   0, (2f / 5f) * mass * size.y * size.z, 0,
                                   0, 0, (2f / 5f) * mass * size.y * size.x);
        }
        else if(t == BodyType.BOX)
        {
            float w2 = size.x * size.x;
            float h2 = size.y * size.y;
            float d2 = size.z * size.z;
            inertia = (1f / 12f) * mass * 
                    new float3x3(
                        w2 + d2, 0, 0,
                        0, h2 + d2, 0,
                        0, 0, h2 + w2
                        );
        }
        else
        {
            throw new System.InvalidOperationException("");
        }
        inverseInertia = math.inverse(inertia);
        Color c = UnityEngine.Random.ColorHSV();
        color = c;
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

    public void Move(float3 amt)
    {
        this.position += amt;
    }

    public void MoveTo(float3 pos)
    {
        this.position = pos;
    }

    public void AddForce(float3 f)
    {
        force += f;
    }

    public AABB AABB()
    {
        if (type == BodyType.SPHERE)
        {
            return new AABB(position - size, position + size);
        }
        else if(type == BodyType.BOX)
        {
            return new AABB(position - size/2, position + size/2);
        }
        else
        {
            throw new System.InvalidOperationException();
        }
    }

}
