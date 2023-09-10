using Unity.Mathematics;
using UnityEngine;

// https://github.com/irixapps/Unity-Separating-Axis-SAT
public class Cube : MonoBehaviour
{
    private bool _hit;

	public bool isStatic;
	public float restitution;
	float invMass;
	public Vector3 velocity;

	private Vector3[] vertices;
	private Vector3[] axes;

	Color[] colors;
	Vector3[] verts ;

	public Transform Transform
	{
		get
		{
			return gameObject.transform;
		}
	}

	public float InvMass
    {
		get { return invMass; }
    }


	public bool Hit
    {
        get
        {
            return _hit;
        }
        set
        {
            if (_hit != value)
            {
                _hit = value;

                Tint();
            }
        }
    }


	public void Start()
	{
		float mass = Transform.localScale.x * Transform.localScale.y * Transform.localScale.z;

		invMass = 1 / mass;
	}
		

	Vector3[] removeDuplicates(Vector3[] dupArray) {

		for (int j = 0; j < dupArray.Length; j++) {
			dupArray[j] = transform.TransformPoint(dupArray[j]);
		}

		Vector3[] newArray = new Vector3[8];  //change 8 to a variable dependent on shape
		bool isDup = false;
		int newArrayIndex = 0;
		for (int i = 0; i < dupArray.Length; i++) {
			for (int j = 0; j < newArray.Length; j++) {
				if (dupArray[i] == newArray[j]) {
					isDup = true;
				}
			}
			if (!isDup) {
				newArray[newArrayIndex] = dupArray[i];
				newArrayIndex++;
				isDup = false;
			}
		}
		return newArray;
	}

	public Vector3[] GetVertices()
	{
		// world vertices
		vertices  = GetComponent<MeshFilter>().mesh.vertices;
		verts = removeDuplicates(vertices);
		return verts;
	}

	public Vector3[] GetAxes()
	{
		axes = new[]
		{
			(this.transform.right),
			(this.transform.up),
			(this.transform.forward)
		};
		return axes;

	}

	private void Tint()
	{
		GetComponent<Renderer>().material.SetColor( "_Color", Hit ? Color.red : Color.white);
	}

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
		Vector3[] vertices = GetVertices();
		for (int i = 0; i < vertices.Length; i++) {
			Gizmos.DrawSphere(vertices[i], 0.1f);
        }

	}
}