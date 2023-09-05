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

    World world;

    void Awake()
    {
        cam = Camera.main;
        world = new World();
    }

    void Update()
    {
        Vector2 spawnPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetMouseButtonDown(0))
        {
            world.AddBody(Instantiate(squarePrefab, spawnPos, Quaternion.identity));
        }
        else if(Input.GetMouseButtonDown(1))
        {
            world.AddBody(Instantiate(circlePrefab, spawnPos, Quaternion.identity));
        }

        cam.transform.Translate(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * Time.deltaTime * moveSpeed);
        cam.orthographicSize += Input.mouseScrollDelta.y * Time.deltaTime * scrollSpeed;

        world.Step(Time.deltaTime);
    }

   
}
