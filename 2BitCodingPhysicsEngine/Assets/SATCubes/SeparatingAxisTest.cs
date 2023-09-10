using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// https://github.com/irixapps/Unity-Separating-Axis-SAT
public class SeparatingAxisTest : MonoBehaviour {

	// References
	// Getting the Right Axes to Test with
	//https://gamedev.stackexchange.com/questions/44500/how-many-and-which-axes-to-use-for-3d-obb-collision-with-sat/

	//Unity Code, that nearly worked, but registered collisions incorrectly in some cases
	//http://thegoldenmule.com/blog/2013/12/supercolliders-in-unity/

	[SerializeField]
	private Cube[] _cubes;

	Vector3[] aAxes;
	Vector3[] bAxes;
	Vector3[] AllAxes;
	Vector3[] aVertices;
	Vector3[] bVertices;

	float minOverlap = 0;
	Vector3 minOverlapAxis = Vector3.zero;

	List<Vector3> penetrationAxes;
	List<float> penetrationAxesDistance;

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {

		for (int i = 0; i < _cubes.Length-1; i++)
		{
			for (int j = i+1; j < _cubes.Length; j++)
			{
				Cube a = _cubes[i];
				Cube b = _cubes[j];
				if (CheckCollision(a, b))
				{
					a.Hit = b.Hit = true;

				}
				else
				{
					a.Hit = b.Hit = false;
				}
			}
		}

		for (int i = 0; i < _cubes.Length - 1; i++)
        {
			Cube a = _cubes[i];
			if (!a.isStatic)
            {
				a.velocity += Vector3.down * 5 * Time.deltaTime;
				a.transform.position += a.velocity * Time.deltaTime;
            }
		}

	}

	public bool CheckCollision( Cube a, Cube b)
	{
		minOverlap = 0;
		minOverlapAxis = Vector3.zero;

		aAxes = a.GetAxes();
		bAxes = b.GetAxes();

		AllAxes = new Vector3[]
		{
			aAxes[0],
			aAxes[1],
			aAxes[2],
			bAxes[0],
			bAxes[1],
			bAxes[2],
			Vector3.Cross(aAxes[0], bAxes[0]),
			Vector3.Cross(aAxes[0], bAxes[1]),
			Vector3.Cross(aAxes[0], bAxes[2]),
			Vector3.Cross(aAxes[1], bAxes[0]),
			Vector3.Cross(aAxes[1], bAxes[1]),
			Vector3.Cross(aAxes[1], bAxes[2]),
			Vector3.Cross(aAxes[2], bAxes[0]),
			Vector3.Cross(aAxes[2], bAxes[1]),
			Vector3.Cross(aAxes[2], bAxes[2])
		};

		aVertices = a.GetVertices();
		bVertices = b.GetVertices();

		penetrationAxes = new List<Vector3>();
		penetrationAxesDistance = new List<float>();

		bool hasOverlap = false;

		if ( ProjectionHasOverlap(a.Transform, b.Transform, AllAxes.Length, AllAxes, bVertices.Length, bVertices, aVertices.Length, aVertices, Color.red, Color.green) )
		{
			hasOverlap = true;
		}
		else if (ProjectionHasOverlap(b.Transform, a.Transform, AllAxes.Length, AllAxes, aVertices.Length, aVertices, bVertices.Length, bVertices, Color.green, Color.red) )
		{
			hasOverlap = true;
		}

		if (hasOverlap)
		{

			// Penetration can be seen here, but its not reliable 
			Debug.Log(minOverlap + " : " + minOverlapAxis);

			Vector3 normal = penetrationAxes[penetrationAxes.Count - 1];
			Vector3 direction = b.transform.position - a.transform.position;
			if (Vector3.Dot(direction, normal) < 0)
			{
				normal = -normal;
			}

			float depth = penetrationAxesDistance[penetrationAxesDistance.Count - 1];
			if (a.isStatic)
			{
				b.transform.position += normal * 1.0f * depth;
			}
			if (b.isStatic)
			{
				a.transform.position -= normal * 1.0f * depth;
			}
			if (!a.isStatic && !b.isStatic)
			{
				a.transform.position -= normal * 0.5f * depth;
				b.transform.position += normal * 0.5f * depth;
			}

			ResolveCollisionBasic(ref a, ref b, normal, depth);
		}

		return hasOverlap;
	}

	void ResolveCollisionBasic(ref Cube a, ref Cube b, Vector3 normal, float depth)
	{
		Vector3 relVel = b.velocity - a.velocity;
		float restitution = Mathf.Min(a.restitution, b.restitution);
		float impulseMag = -(1 + restitution) * Vector3.Dot(relVel, normal);
		impulseMag /= (a.InvMass + b.InvMass);
		//disregard rotation and friction
		Vector3 impulse = impulseMag * normal;

		a.velocity -= impulse * a.InvMass;
		b.velocity += impulse * b.InvMass;
	}

	/// Detects whether or not there is overlap on all separating axes.
	private bool ProjectionHasOverlap(
		Transform aTransform,
		Transform bTransform,

		int aAxesLength,
		Vector3[] aAxes,

		int bVertsLength,
		Vector3[] bVertices,

		int aVertsLength,
		Vector3[] aVertices,

		Color aColor,
		Color bColor)
	{
		bool hasOverlap = true;

		minOverlap = float.PositiveInfinity;

		for (int i = 0; i < aAxesLength; i++)
		{
			
			
			float bProjMin = float.MaxValue, aProjMin = float.MaxValue;
			float bProjMax = float.MinValue, aProjMax = float.MinValue;

			Vector3 axis = aAxes[i];

			// Handles the cross product = {0,0,0} case
			if (aAxes[i] == Vector3.zero ) return true;

			for (int j = 0; j < bVertsLength; j++)
			{
				float val = FindScalarProjection((bVertices[j]), axis);

				if (val < bProjMin)
				{
					bProjMin = val;
				}

				if (val > bProjMax)
				{
					bProjMax = val;
				}
			}

			for (int j = 0; j < aVertsLength; j++)
			{
				float val = FindScalarProjection((aVertices[j]), axis);

				if (val < aProjMin)
				{
					aProjMin = val;
				}

				if (val > aProjMax)
				{
					aProjMax = val;
				}
			}

			float overlap = FindOverlap(aProjMin, aProjMax, bProjMin, bProjMax);

			if ( overlap < minOverlap )
			{
				minOverlap = overlap;
				minOverlapAxis = axis;

				penetrationAxes.Add(axis);
				penetrationAxesDistance.Add(overlap);

			}

			//Debug.Log(overlap);

			if (overlap <= 0)
			{
				// Separating Axis Found Early Out
				return false;
			}
		}

		return true; // A penetration has been found
	}


	/// Calculates the scalar projection of one vector onto another, assumes normalised axes
	private static float FindScalarProjection(Vector3 point, Vector3 axis)
	{
		return Vector3.Dot(point, axis);
	}

	/// Calculates the amount of overlap of two intervals.
	private float FindOverlap(float astart, float aend, float bstart, float bend)
	{
		if (astart < bstart)
		{
			if (aend < bstart)
			{
				return 0f;
			}

			return aend - bstart;
		}

		if (bend < astart)
		{
			return 0f;
		}

		return bend - astart;
	}
}