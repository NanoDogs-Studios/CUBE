using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuCubeSpawner : MonoBehaviour
{
    public Mesh cubeMesh;
    public Texture2D baseMap;

    // store all spawned cubes
    private List<GameObject> spawnedCubes = new List<GameObject>();

    // timer for automatic spawning
    private float spawnTimer = 0f;
    public float spawnInterval = 5f; // every 5 seconds

    private void Update()
    {
        // countdown automatic spawn
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnCube();
        }

        // spawn on mouse click
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SpawnCube();
        }
    }

    private void SpawnCube()
    {
        // create cube
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = new Vector3(Random.Range(-35, -10), 35, Random.Range(-25, 25));
        cube.transform.rotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
        cube.transform.localScale = Vector3.one * Random.Range(110f, 370f);

        // apply mesh
        cube.GetComponent<MeshFilter>().sharedMesh = cubeMesh;

        // apply material
        Material material = new Material(Shader.Find("Shader Graphs/PolishedProto"));
        material.color = new Color(Random.value, Random.value, Random.value);
        material.SetTexture("_Base", baseMap);
        material.SetTextureScale("_Base", new Vector2(4, 4));
        material.SetTexture("_EmissionMap", baseMap);
        material.SetTextureScale("_EmissionMap", new Vector2(4, 4));
        cube.GetComponent<Renderer>().material = material;

        // collider + rigidbody
        cube.GetComponent<BoxCollider>().size = new Vector3(0.02f, 0.02f, 0.02f);
        Rigidbody rb = cube.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearVelocity = new Vector3(0, -Random.Range(5f, 50f), 0);

        // add to list
        spawnedCubes.Add(cube);

        // if more than 15 cubes exist, destroy the oldest one
        if (spawnedCubes.Count > 15)
        {
            GameObject oldestCube = spawnedCubes[0];
            spawnedCubes.RemoveAt(0);
            Destroy(oldestCube);
        }
    }
}
