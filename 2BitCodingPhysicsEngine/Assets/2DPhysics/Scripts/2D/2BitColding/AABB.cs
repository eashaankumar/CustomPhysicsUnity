using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyPhysics.TwoD.TwoBitCoding
{
    public struct AABB
    {
        public readonly Vector2 min, max;

        public AABB(Vector2 _min, Vector2 _max)
        {
            min = _min;
            max = _max;
        }
    }
}
