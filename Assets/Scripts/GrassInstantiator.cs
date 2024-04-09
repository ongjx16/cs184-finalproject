using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassInstantiator : MonoBehaviour
{
    //assigned by code
    public Mesh grassMesh;
    //assigned in unity editor
    public Material grassMaterial;

    // private ComputeBuffer positionBuffer;

    // //buffer of indirect arguments (e.g. ) to be used later on in Graphics.DrawMeshInstancedIndirect to render the mesh with positions from the position Buffer
    // private ComputeBuffer argsBuffer;

    private struct GrassData
    {
        public Vector3 position;
        // public Vector2 uv;
    }
    int grassDataSize = (3 * 4); // 12 bytes for vector3

    private struct GrassCluster
    {
        public ComputeBuffer argsBuffer;
        public ComputeBuffer posBuffer;
        public Material material;
    }

    //to be used for every args buffer initialised for each grass chunk
    uint[] args;
    GrassCluster[] allClusters;

    private int numClusters = 10;
    private int clusterSize;
    private int grassPerCluster = 5;

    //length of one side of the field
    private int fieldSize = 500;
    // private int clusterDensity = 1;

    Bounds bounds;
    private ComputeShader clusterShader;


    // Start is called before the first frame update
    void Start()
    {
        clusterSize = fieldSize / numClusters;
        // grassPerCluster = clusterSize * clusterDensity;
        // clusterSize = grassPerCluster;
        // grassPerCluster *= grassPerCluster;

        clusterShader = Resources.Load<ComputeShader>("clusterCompute");
        if (clusterShader == null)
        {
            Debug.LogError("compute shader not found");
        }

        clusterShader.SetInt("fieldSize", fieldSize);
        clusterShader.SetInt("grassSize", clusterSize/grassPerCluster);
        clusterShader.SetInt("grassPerCluster", grassPerCluster);

        var grassMeshScript = FindObjectOfType<GrassMesh>(); // Find the GrassMesh component
        if (grassMeshScript != null)
        {
            grassMesh = grassMeshScript.generatedMesh; // Access the generated mesh
        }
        else
        {
            Debug.LogError("GrassMesh component with generated mesh not found in the scene.");
        }

        // Setup args for instancing
        args = new uint[5] { grassMesh.GetIndexCount(0), (uint)(grassPerCluster*grassPerCluster), grassMesh.GetIndexStart(0), grassMesh.GetBaseVertex(0), 0 };

        // call command to populate field [aka populate allClusters array and populate args and position buffers for each cluster]
        populateField();

        Vector3 boundsCenter = new Vector3(0.0f, 0.0f, 0.0f);
        // Define the size of the area that encompasses all your grass instances.
        Vector3 boundsSize = new Vector3(fieldSize, 50.0f, fieldSize); // For example, 100 units wide, 50 units tall, 100 units deep.


        bounds = new Bounds(boundsCenter, boundsCenter);

    }

    //function to initialise all clusters across the field 
    void populateField()
    {
        allClusters = new GrassCluster[numClusters * numClusters];

        for (int x = 0; x < numClusters; ++x)
        {
            for (int y = 0; y < numClusters; ++y)
            {
                allClusters[x + y * numClusters] = generateCluster(x, y);
            }
        }
    }

    GrassCluster generateCluster(int x, int y)
    {
        GrassCluster cluster = new GrassCluster { };
        if (grassPerCluster > 0)
        {
            cluster.argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            cluster.posBuffer = new ComputeBuffer(grassPerCluster*grassPerCluster, grassDataSize);
            cluster.material = new Material(grassMaterial);

            cluster.material.SetBuffer("_Positions", cluster.posBuffer);

            cluster.argsBuffer.SetData(args);

            // initialise custom shader to render each cluster
            clusterShader.SetBuffer(0, "_posBuffer", cluster.posBuffer);
            clusterShader.SetInt("x_offset", x*clusterSize);
            clusterShader.SetInt("y_offset", y*clusterSize);

            Debug.Log($"x_offset: {x*clusterSize}");
            Debug.Log($"y_offset: {y*clusterSize}");
            // dispatch shader 
            // first arg: 0 [kernel number]
            // second arg: numthreads[x,y,z]; this means we are dispatching the clustershader for each cluster to be number of grass objects in x axis x number of grass objects in y axis x 1 (2d grid of grass);
            clusterShader.Dispatch(0, grassPerCluster, 1, grassPerCluster);

            int bufferCount = grassPerCluster*grassPerCluster;
            Vector3[] positions = new Vector3[bufferCount];
            cluster.posBuffer.GetData(positions);

            // Log the positions
            for (int i = 0; i < positions.Length; i++)
            {
                Debug.Log($"Position {i}: {positions[i]}");
            }
        }
        else
        {
            Debug.LogError("grassPerCluster is zero, skipping cluster generation.");
        }


        return cluster;

    }

    // Update is called once per frame
    void Update()
    {
        Debug.LogError("update call");
        Debug.LogError(numClusters);
        for (int i = 0; i < numClusters * numClusters; i++)
        {
            Graphics.DrawMeshInstancedIndirect(grassMesh, 0, allClusters[i].material, bounds, allClusters[i].argsBuffer);
            // Debug.LogError("cluster rendered");
        }

    }
}
