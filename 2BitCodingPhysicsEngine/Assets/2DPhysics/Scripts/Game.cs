using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField]
    Circle circlePrefab;
    [SerializeField]
    Square squarePrefab;
    [SerializeField]
    float moveSpeed = 5f;
    [SerializeField]
    float scrollSpeed = 100f;

    Camera cam;

    List<Shape> shapes = new List<Shape>();

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        Vector2 spawnPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetMouseButtonDown(0))
        {
            shapes.Add(Instantiate(squarePrefab, spawnPos, Quaternion.identity));
        }
        else if(Input.GetMouseButtonDown(1))
        {
            shapes.Add(Instantiate(circlePrefab, spawnPos, Quaternion.identity));
        }

        cam.transform.Translate(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * Time.deltaTime * moveSpeed);
        cam.orthographicSize += Input.mouseScrollDelta.y * Time.deltaTime * scrollSpeed;

        ResolveNullShapes();
        ResolveCollisions();
    }

    void ResolveNullShapes()
    {
        for(int i = shapes.Count-1; i >= 0; i--)
        {
            Shape shape = shapes[i];
            if (shape == null) shapes.RemoveAt(i);

        }
    }

    void ResolveCollisions()
    {
        for (int i = 0; i < shapes.Count-1; i++)
        {
            Shape a = shapes[i];
            for (int j = i+1; j < shapes.Count; j++)
            {
                Shape b = shapes[j];
                if (a.body.type == ShapeType.Circle && b.body.type == ShapeType.Circle)
                {
                    Vector2 normal;
                    float depth;
                    if(Collisions.IntersetCircles(a.body.position, a.body.radius, b.body.position, b.body.radius, out normal, out depth))
                    {
                        a.body.Move(-normal * depth * 0.5f);
                        b.body.Move(normal * depth * 0.5f);
                        a.OnCollision(b);
                        b.OnCollision(a);
                    }
                }
                else if (IsPolygon(a.body.type) && IsPolygon(b.body.type))
                {
                    Vector2 normal;
                    float depth;
                    BoxVertices PolyGonA = new BoxVertices(a.body.position, a.body.size, a.body.rotation);
                    BoxVertices PolyGonB = new BoxVertices(b.body.position, b.body.size, b.body.rotation);
                    Vector2[] verticesA = GetCounterClockwiseVertices(PolyGonA);
                    Vector2[] verticesB = GetCounterClockwiseVertices(PolyGonB);
                    Vector2[] normalsA = Collisions.GetNormals(verticesA);
                    Vector2[] normalsB = Collisions.GetNormals(verticesB);

                    Vector2[] z = new Vector2[normalsA.Length + normalsB.Length];
                    normalsA.CopyTo(z, 0);
                    normalsB.CopyTo(z, normalsA.Length);

                    if (Collisions.IntersectPolygons(verticesA, verticesB, z, out normal, out depth))
                    {
                        a.body.Move(-normal * depth * 0.5f);
                        b.body.Move(normal * depth * 0.5f);
                        a.OnCollision(b);
                        b.OnCollision(a);
                    }
                }
            }

        }
    }

    Vector2[] GetCounterClockwiseVertices(BoxVertices vertices)
    {
        return new Vector2[] { vertices.topLeft, vertices.topRight, vertices.bottomRight, vertices.bottomLeft };
    }

    bool IsPolygon(ShapeType type)
    {
        return type == ShapeType.Box;
    }
}
