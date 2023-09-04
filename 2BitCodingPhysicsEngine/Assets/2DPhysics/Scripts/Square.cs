using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Square : Shape
{
    [SerializeField]
    bool isStatic;

    SpriteRenderer spriteRenderer;
    // Start is called before the first frame update
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = Color.HSVToRGB(Random.value, Random.value, Random.Range(0.5f, 1.0f));

        Vector2 size = new Vector2(1, 1);//new Vector2(Random.Range(0.2f, 1.0f), Random.Range(0.2f, 1.0f)) * 1;
        float density = Random.Range(0.5f, 10f);
        float restitution = Random.value;
        string error = "";

        if(!Body.CreateBoxBody(size, transform.position, density, isStatic, restitution, out body, out error))
        {
            Debug.LogError(error);
            Destroy(gameObject);
        }
        transform.localScale = body.size/2;
        body.rotation = Quaternion.AngleAxis(Random.value * 360, Vector3.forward);
    }

    private void Update()
    {
        transform.position = body.position;
        transform.localScale = body.size/2;
        transform.rotation = body.rotation;
    }

    /*private void OnDrawGizmos()
    {
        BoxVertices vertices = new BoxVertices(body.position, body.size, body.rotation);

        Gizmos.color = spriteRenderer.color;
        Gizmos.DrawSphere(vertices.topLeft, body.size.magnitude/10);
        Gizmos.DrawSphere(vertices.topRight, body.size.magnitude / 10 + 3e-2f);
        Gizmos.DrawSphere(vertices.bottomLeft, body.size.magnitude / 10 + 6e-2f);
        Gizmos.DrawSphere(vertices.bottomRight, body.size.magnitude / 10 + 9e-2f);

    }*/
}
