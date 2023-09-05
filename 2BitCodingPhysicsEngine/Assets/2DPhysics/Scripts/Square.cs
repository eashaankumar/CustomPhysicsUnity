using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Square : Shape
{
    [SerializeField]
    bool isStatic;

    int isColliding;

    SpriteRenderer spriteRenderer;
    public Color reg;
    // Start is called before the first frame update
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        transform.position = body.position;
        transform.localScale = body.size/2;

        //body.rotation *= Quaternion.AngleAxis(90 * Time.deltaTime, Vector3.forward);

        transform.rotation = body.rotation;

        if (isColliding > 0)
        {
            spriteRenderer.color = Color.white;
            isColliding--;
        }
        else
        {
            spriteRenderer.color = reg;
        }
    }

    private void OnDrawGizmos()
    {
        BoxVertices vertices = new BoxVertices(body.position, body.size, body.rotation);

        Gizmos.color = spriteRenderer.color;
        Gizmos.DrawSphere(vertices.topLeft, body.size.magnitude/10);
        Gizmos.DrawSphere(vertices.topRight, body.size.magnitude / 10 + 3e-2f);
        Gizmos.DrawSphere(vertices.bottomLeft, body.size.magnitude / 10 + 6e-2f);
        Gizmos.DrawSphere(vertices.bottomRight, body.size.magnitude / 10 + 9e-2f);


        DrawNormals(new Vector2[] { vertices.topLeft, vertices.topRight, vertices.bottomRight, vertices.bottomLeft });

        AABB aabb = body.GetAABB();

        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(body.position, aabb.max - aabb.min);
    }

    void DrawNormals(Vector2[] verts)
    {
        Vector2[] normals = Collisions.GetNormals(verts);
        Debug.Assert(normals.Length == verts.Length);
        for(int i = 0; i < normals.Length; i++)
        {
            Gizmos.color = Color.Lerp(Color.yellow, Color.white, (float)i / verts.Length);
            Gizmos.DrawRay((verts[i] + verts[(i+1) % verts.Length]) / 2, normals[i]);
        }
    }

    public override void OnCollision(Shape other)
    {
        isColliding++;
    }

    public override void RandomGenerate()
    {
        reg = Color.HSVToRGB(Random.value, Random.value, Random.Range(0.5f, 1.0f));
        spriteRenderer.color = reg;

        Vector2 size = new Vector2(Random.Range(0.2f, 1.0f), Random.Range(0.2f, 1.0f)) * 1;
        float density = Random.Range(0.5f, 10f);
        float restitution = Random.value;
        string error = "";

        if (!Body.CreateBoxBody(size, transform.position, density, isStatic, restitution, out body, out error))
        {
            Debug.LogError(error);
            Destroy(gameObject);
        }
        transform.localScale = body.size / 2;
        body.rotation = Quaternion.AngleAxis(Random.value * 360, Vector3.forward);
    }
}
