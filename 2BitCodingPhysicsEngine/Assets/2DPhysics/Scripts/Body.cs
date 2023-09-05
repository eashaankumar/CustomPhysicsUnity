using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ShapeType
{
    Circle = 0, Box = 1
}

public struct Body
{
    public Vector2 position;
    public Vector2 linearVelocity;
    public Quaternion rotation;
    public float rotationalVelocity;
    private Vector2 force;

    private float mass;
    private float invMass;
    public float density;
    public float restitution; // bouncy
    public float area;
    public bool isStatic;

    public float radius;
    public Vector2 size;

    public ShapeType type;

    public float InvMass
    {
        get
        {
            return invMass;
        }
    }

    public void Move(Vector2 amt)
    {
        this.position += amt;
    }

    public void MoveTo(Vector2 pos)
    {
        this.position = pos;
    }

    public void AddForce(Vector2 f)
    {
        force += f;
    }


    public void Step(float dt)
    {
        if (!isStatic)
        {
            this.linearVelocity += this.force / this.mass * dt;
            this.position += this.linearVelocity * dt;
            this.rotation *= Quaternion.AngleAxis(rotationalVelocity * dt, Vector3.forward);
        }
        else
        {
            this.linearVelocity = Vector2.zero;
            this.rotationalVelocity = 0;
        }
        this.force = Vector2.zero;
    }


    public static bool CreateCircleBody(float _radius, Vector2 _position, float _density, bool _isStatic, float _restitution, out Body body, out string error)
    {
        body = new Body { };
        error = string.Empty;

        float _area = _radius * _radius;
        if (_area < World.minBodySize)
        {
            error = $"Circle radius is too small. Area {_area}";
            return false;
        }
        if (_area > World.maxBodySize)
        {
            error = $"Circle radius is too large. Area {_area}";
            return false;
        }
        if(_density > World.MaxDensity)
        {
            error = $"Circle density is too large";
            return false;
        }
        if (_density < World.MinDensity)
        {
            error = $"Circle density is too small";
            return false;
        }
        _restitution = Mathf.Clamp01(_restitution);

        // mass = area * depth * density
        float mass = _area * 1f * _density;

        body = new Body
        {
            position=_position,
            mass = mass,
            invMass = 1 / mass,
            density = _density,
            restitution = _restitution,
            area = _area,
            isStatic = _isStatic,
            radius = _radius,
            type=ShapeType.Circle,
        };
        return true;
    }

    public static bool CreateBoxBody(Vector2 _size, Vector2 _position, float _density, bool _isStatic, float _restitution, out Body body, out string error)
    {
        body = new Body { };
        error = string.Empty;

        float _area = _size.x * _size.y;
        if (_area < World.minBodySize)
        {
            error = $"Box radius is too small. Area {_area}";
            return false;
        }
        if (_area > World.maxBodySize)
        {
            error = $"Box radius is too large. Area {_area}";
            return false;
        }
        if (_density > World.MaxDensity)
        {
            error = $"Box density is too large";
            return false;
        }
        if (_density < World.MinDensity)
        {
            error = $"Box density is too small";
            return false;
        }
        _restitution = Mathf.Clamp01(_restitution);

        // mass = area * depth * density
        float mass = _area * 1f * _density;

        body = new Body
        {
            position = _position,
            mass = mass,
            invMass = 1 / mass,
            density = _density,
            restitution = _restitution,
            area = _area,
            isStatic = _isStatic,
            size = _size,
            type = ShapeType.Box,
        };
        return true;
    }
}
