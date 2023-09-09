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
    Material material;
    [SerializeField]
    float3 gravity;
    [SerializeField]
    int substeps;

    World world;
    // Start is called before the first frame update
    void Awake()
    {
        world = new World(Allocator.Persistent, 1000000, 1244243, gravity);
        world.AddSphere(new SphereBody(20, true, 1, 0.5f, 0.1f, 0.5f) { position = new float3(0, -10, 0) }, out int id);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            AddSphere(Camera.main.ScreenToWorldPoint(Input.mousePosition), Quaternion.identity, UnityEngine.Random.Range(0.5f, 1f));
        }
        world.Tick(Time.deltaTime, substeps);

        RenderWorld();
    }

    void RenderWorld()
    {
        // render
        NativeArray<int> keys = world._bodies.GetKeyArray(Allocator.Temp);
        Matrix4x4[] matrices = new Matrix4x4[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            int key = keys[i];
            SphereBody body = world._bodies[key];
            matrices[i] = Matrix4x4.TRS(body.position, body.rotation, Vector3.one * body.radius * 2);
        }
        Graphics.DrawMeshInstanced(sphereMesh, 0, material, matrices);
        keys.Dispose();
    }

    int AddSphere(Vector3 positions, Quaternion rotation, float radius)
    {
        SphereBody body = new SphereBody(radius, false, 1, 0.5f, 0.4f, 0.8f);
        body.position = positions;
        body.rotation = rotation;
        int id;
        world.AddSphere(body, out id);
        return id;
    }

    private void OnDestroy()
    {
        world.Dispose();
    }
}

