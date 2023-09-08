using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyPhysics.TwoD.TwoBitCoding
{
    public enum ShapeType
    {
        Circle = 0, Box = 1
    }

    public struct Body
    {
        public Vector2 position;
        public Vector2 linearVelocity;
        public Quaternion rotation;
        public float angularVelocityRadians;
        private Vector2 force;

        private float mass;
        private float invMass;
        public float density;
        public float restitution; // bouncy
        public float area;
        public bool isStatic;
        private float inertia, inverseInertia;
        public float staticFriction, dynamicFriction;

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

        public float Mass
        {
            get { return mass; }
        }

        public float Inertia
        {
            get { return inertia; }
        }

        public float InvInertia
        {
            get { return inverseInertia; }
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
                this.rotation *= Quaternion.AngleAxis(this.angularVelocityRadians * Mathf.Rad2Deg * dt, Vector3.forward);
            }
            else
            {
                this.linearVelocity = Vector2.zero;
                this.angularVelocityRadians = 0;
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
            if (_density > World.MaxDensity)
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
                position = _position,
                mass = mass,
                invMass = 1 / mass,
                density = _density,
                restitution = _restitution,
                area = _area,
                isStatic = _isStatic,
                radius = _radius,
                type = ShapeType.Circle,
            };

            body.rotation = Quaternion.identity;

            body.staticFriction = 0.6f;
            body.dynamicFriction = 0.4f;

            body.inertia = body.CalculateRotationalIntertia();
            body.inverseInertia = 1f / body.inertia;
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

            body.staticFriction = 0.6f;
            body.dynamicFriction = 0.4f;

            body.inertia = body.CalculateRotationalIntertia();
            body.inverseInertia = 1f / body.inertia;

            body.rotation = Quaternion.identity;

            return true;
        }

        private float CalculateRotationalIntertia()
        {
            if (type == ShapeType.Circle)
            {
                return 0.5f * this.Mass * this.radius * this.radius;
            }
            else if (type == ShapeType.Box)
            {
                return (1f / 12f) * this.Mass * (this.size.x * this.size.x + this.size.y * this.size.y);
            }
            else
            {
                throw new System.ArgumentException("Unsupported shape type for rotation inertia " + type);
            }
        }

        public AABB GetAABB()
        {
            AABB aabb;
            if (this.type == ShapeType.Box)
            {
                BoxVertices vertices = new BoxVertices(position, size, rotation);
                float[] xVerts = new float[] { vertices.topLeft.x, vertices.topRight.x, vertices.bottomLeft.x, vertices.bottomRight.x };
                float[] yVerts = new float[] { vertices.topLeft.y, vertices.topRight.y, vertices.bottomLeft.y, vertices.bottomRight.y };

                float minX = Mathf.Min(xVerts);
                float maxX = Mathf.Max(xVerts);

                float minY = Mathf.Min(yVerts);
                float maxY = Mathf.Max(yVerts);

                aabb = new AABB(new Vector2(minX, minY), new Vector2(maxX, maxY));
            }
            else if (this.type == ShapeType.Circle)
            {
                aabb = new AABB(position + (Vector2.left + Vector2.down) * radius, position + (Vector2.up + Vector2.right) * radius);
            }
            else
            {
                throw new System.Exception("Unknown shape type " + this.type);
            }

            return aabb;
        }
    }

    public struct FixedJoint
    {
        private readonly Shape a, b;
        private readonly Vector2 abVecLocalA;
        private readonly Quaternion abRot;

        public FixedJoint(Shape _a, Shape _b)
        {
            a = _a;
            b = _b;
            abVecLocalA = Quaternion.Inverse(_a.body.rotation) * (b.body.position - a.body.position);
            abRot = Quaternion.Inverse(_a.body.rotation) * _b.body.rotation;
        }

        public bool Step(float dt)
        {
            if (a == null || b == null) return false;
            Vector2 rotatedABVec = a.body.rotation * abVecLocalA;
            b.body.position = a.body.position + rotatedABVec;

            a.body.linearVelocity = (a.body.linearVelocity + b.body.linearVelocity) / 2f;
            b.body.linearVelocity = a.body.linearVelocity;

            a.body.angularVelocityRadians = (a.body.angularVelocityRadians + b.body.angularVelocityRadians) / 2f;
            b.body.angularVelocityRadians = a.body.angularVelocityRadians;

            b.body.rotation = abRot * a.body.rotation;


            return true;
        }
    }

    public struct FixedDistanceJoint
    {
        private readonly Shape a, b;
        private readonly Vector2 abVecLocalA;

        public FixedDistanceJoint(Shape _a, Shape _b)
        {
            a = _a;
            b = _b;
            abVecLocalA = Quaternion.Inverse(_a.body.rotation) * (b.body.position - a.body.position);
        }

        public bool Step(float dt)
        {
            if (a == null || b == null) return false;
            Vector2 rotatedABVec = a.body.rotation * abVecLocalA;
            b.body.position = a.body.position + rotatedABVec;
            return true;
        }
    }

}