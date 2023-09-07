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
    [SerializeField]
    int iterations;

    Camera cam;

    World world;

    Square player;
    Square platform;

    void Awake()
    {
        cam = Camera.main;
        world = new World();

       
    }

    private void Start()
    {
        // player
        /*player = Instantiate(squarePrefab, Vector2.zero, Quaternion.identity);
        string error;
        if (!Body.CreateBoxBody(Vector2.one * 0.5f, Vector2.zero, 5f, false, 0.5f, out player.body, out error))
        {
            Debug.LogError(error);
        }
        else
        {
            print("Created player");
            player.reg = Color.Lerp(Color.blue, Color.white, 0.5f);
            player.reg.a = 1;
            world.AddBody(player);
        }*/
        string error;
        // platform
        platform = Instantiate(squarePrefab, Vector2.zero, Quaternion.identity);
        if (!Body.CreateBoxBody(new Vector2(5, 0.5f), Vector2.down * 3f, 5f, true, 0.5f, out platform.body, out error))
        {
            Debug.LogError(error);
        }
        else
        {
            print("Created platform");
            platform.reg = Color.Lerp(Color.red, Color.white, 0.5f);
            platform.reg.a = 1;
            //platform.body.rotation = Quaternion.AngleAxis(-15, Vector3.forward);
            world.AddBody(platform);
        }
    }

    void Update()
    {
        Vector2 spawnPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetMouseButtonDown(0))
        {
            Shape s = Instantiate(squarePrefab, spawnPos, Quaternion.identity);
            s.RandomGenerate();
            world.AddBody(s);
        }
        else if(Input.GetMouseButtonDown(1))
        {
            Shape s = Instantiate(circlePrefab, spawnPos, Quaternion.identity);
            s.RandomGenerate();
            world.AddBody(s);
        }

        cam.transform.Translate((new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * moveSpeed * Time.deltaTime));
        //player.body.AddForce((new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * moveSpeed));
        //cam.transform.position = Vector3.Lerp(cam.transform.position, new Vector3(player.body.position.x, player.body.position.y, -10), 0.05f);
        cam.orthographicSize += Input.mouseScrollDelta.y * Time.deltaTime * scrollSpeed;

        world.Step(Time.deltaTime, iterations);
    }

    private void OnDrawGizmos()
    {
        if (world != null)
        {
            Gizmos.color = Color.green;
            foreach (Vector2 contactPoint in world.ContactPointsList)
            {
                Gizmos.DrawSphere(contactPoint, 0.1f);
            }
        }
    }

}
