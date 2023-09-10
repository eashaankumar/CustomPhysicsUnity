using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField]
    Mesh sphereMesh;
    [SerializeField]
    Mesh boxMesh;
    [SerializeField]
    Material material;
    [SerializeField]
    float3 gravity;
    [SerializeField]
    int substeps;

    World world;

    int platformCubeId;

    Unity.Mathematics.Random random;
    // Start is called before the first frame update
    void Awake()
    {
        world = new World(Allocator.Persistent, 1000000, 1244243, gravity);
        world.AddBody(new Body(BodyType.BOX, 20, true, 1, 0.5f, 0.1f, 0.5f), out platformCubeId);
        random = new Unity.Mathematics.Random(1234145);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            AddSphere(Camera.main.ScreenToWorldPoint(Input.mousePosition), Quaternion.identity, UnityEngine.Random.Range(0.5f, 1f));
        }
        else if (Input.GetMouseButtonDown(1))
        {
            AddBox(Camera.main.ScreenToWorldPoint(Input.mousePosition), Quaternion.identity, math.abs(random.NextFloat3()));
        }

        Body b = world._bodies[platformCubeId];
        //b.Rotate(new float3(1, 0, 0) * Time.deltaTime);
        world._bodies[platformCubeId] = b;

        world.Tick(Time.deltaTime, substeps);

        RenderWorld();
    }

    void RenderWorld()
    {
        // render
        NativeArray<int> keys = world._bodies.GetKeyArray(Allocator.Temp);
        List<Matrix4x4> sphereMatrices = new List<Matrix4x4>();
        List<Matrix4x4> boxMatrices = new List<Matrix4x4>();
        List<Vector4> colors = new List<Vector4>();
        for (int i = 0; i < keys.Length; i++)
        {
            int key = keys[i];
            Body body = world._bodies[key];
            if (body.type == BodyType.SPHERE)
                sphereMatrices.Add(Matrix4x4.TRS(body.position, body.rotation, body.size * 2));

            if (body.type == BodyType.BOX)
                boxMatrices.Add(Matrix4x4.TRS(body.position, body.rotation, body.size));

            colors.Add(body.color);
        }
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetVectorArray("_Colors", colors);
        Graphics.DrawMeshInstanced(sphereMesh, 0, material, sphereMatrices, mpb);
        Graphics.DrawMeshInstanced(boxMesh, 0, material, boxMatrices, mpb);
        keys.Dispose();
    }

    private void OnDrawGizmos()
    {
        NativeArray<int> keys = world._bodies.GetKeyArray(Allocator.Temp);
        for (int i = 0; i < keys.Length; i++)
        {
            Body body = world._bodies[keys[i]];
            BoxVertices PolyGonA = new BoxVertices(body.position, body.size, body.rotation);

            /*float3[] verticesA = Collisions.GetVertices(PolyGonA);
            foreach(float3 vertex in verticesA)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(vertex, 0.1f);
            }

            float3[] normalsA = Collisions.GetAxis(PolyGonA); // 3
            foreach (float3 normal in normalsA)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(body.position, normal);
            }*/

            /*float3 point = Camera.main.transform.position;
            float3 closestPoint = Collisions.ClosestPointOnBox(body.position, body.rotation, body.size, point);
            Gizmos.color = Color.black;
            Gizmos.DrawLine(point, closestPoint);*/
        }
        keys.Dispose();

        foreach(float3 contactPoint in world._contactPointsList)
        {
            Gizmos.color = Color.Lerp(Color.green, Color.white, 0.5f);
            Gizmos.DrawSphere(contactPoint, 0.01f);
        }

    }

    int AddSphere(Vector3 positions, Quaternion rotation, float radius)
    {
        Body body = new Body(BodyType.SPHERE, radius, false, 1, 0.5f, 0.4f, 0.8f);
        body.position = positions;
        body.rotation = rotation;
        int id;
        world.AddBody(body, out id);
        return id;
    }

    int AddBox(Vector3 positions, Quaternion rotation, float3 size)
    {
        Body body = new Body(BodyType.BOX, size, false, 1, 0.5f, 0.4f, 0.8f);
        body.position = positions;
        body.rotation = rotation;
        int id;
        world.AddBody(body, out id);
        return id;
    }

    private void OnDestroy()
    {
        world.Dispose();
    }
}

