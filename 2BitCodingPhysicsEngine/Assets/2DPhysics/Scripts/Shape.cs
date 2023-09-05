using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Shape : MonoBehaviour
{
    public Body body;

    public abstract void OnCollision(Shape other);
    public abstract void RandomGenerate();
}
