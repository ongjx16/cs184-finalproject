using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassInstantiator : MonoBehaviour
{
    // -- Public variables to be assigned in unity editor. --
    public Mesh grassMesh;
    public Material grassMaterial;

    // `fieldSize`: The length of one side of the field.
    public int fieldSize = 100;
    // `numChunks`: The number of chunks to render along each axis. (Eg. `numChunks` = 10 means that there will be 10 chunks along the x and z axis.)
    public int numChunks = 10;

    // `scale`: The number of grass clusters to render in each length unit.
    // Eg. If `resolution` = 10 and `scale` = 2, we render 20 grass clusters on the x and z axis of a chunk each. (i.e. 20x20 clusters rendered in one chunk)
    public int scale = 1;

    // -- Private variables to be assigned in code. --
    // `resolution`: The length of the chunk on the x and z axis. equal to `fieldSize / numChunks`.
    private int resolution;

    // `initializeGrassShader`: The compute shader script found in `Resources/clusterCompute.compute`. Runs on the GPU.
    private ComputeShader initializeGrassShader, cullGrassShader;

    // `GrassCluster`: Data for each grass cluster.
    // A grass cluster consists of 3 meshes of grass images placed in a asterisk (*)-like arrangement.
    private struct GrassCluster
    {
        public Vector4 position;
    }

    // `GrassChunk`: Data for each grass chunk.
    // A grass chunk consists of (resolution x scale)^2 number of `GrassClusters`.
    private struct GrassChunk
    {
        // `argsBuffer`: Buffer of indirect arguments to be used later on in in `Graphics.DrawMeshInstancedIndirect` to render the mesh with positions from the pos buffer.
        public ComputeBuffer argsBuffer;
        // `posBuffer`: Buffer / Array of all positions of grass clusters in the chunk
        public ComputeBuffer posBuffer;
        // `grassMaterial`: Non-rotated grass mesh.
        public Material grassMaterial;
        // `grassMaterial2`: Grass mesh rotated by 50 degrees.
        public Material grassMaterial2;
        // `grassMaterial3`: Grass mesh rotated by -50 degrees.
        public Material grassMaterial3;

        public ComputeBuffer argsBufferLOD;
        public ComputeBuffer culledposBuffer;
        public Bounds bounds;

    }
    private int numVoteThreadGroups, numGroupScanThreadGroups, numInstancesPerChunk, numThreadGroups;
    private ComputeBuffer voteBuffer, scanBuffer, groupSumArrayBuffer, scannedGroupSumBuffer;

    GrassChunk[] allChunks;
    uint[] args;

    // Start is called before the first frame update.
    // Sets up the necessary variables before drawing.
    void Start()
    {
        resolution = fieldSize / numChunks;

        initializeGrassShader = Resources.Load<ComputeShader>("clusterCompute");
        if (initializeGrassShader == null)
        {
            Debug.LogError("compute shader not found");
        }
        resolution *= scale;

        // Access the camera component
        Camera cam = Camera.main; // Adjust this if you're not using the main camera

        // Increase the field of view to broaden the visible area
        cam.fieldOfView += 10.0f; // Increase by 5 degrees, adjust as necessary


        cullGrassShader = Resources.Load<ComputeShader>("CullGrass");
        if (cullGrassShader == null)
        {
            Debug.LogError("cull grass compute shader not found");
        }

        // Updates the position of the grass.
        populateField();
    }
    void populateField()
    {
        allChunks = new GrassChunk[numChunks * numChunks];

        for (int x = 0; x < numChunks; ++x)
        {
            for (int y = 0; y < numChunks; ++y)
            {
                allChunks[x + y * numChunks] = createChunk(x, y);
            }
        }
    }

    // Updates the position of all grass clusters in each chunk.
    GrassChunk createChunk(int x, int y)
    {
        GrassChunk chunk = new GrassChunk { };
        chunk.posBuffer = new ComputeBuffer(resolution * resolution, 4 * 4);
        chunk.culledposBuffer = new ComputeBuffer(resolution * resolution, 4 * 4);
        chunk.argsBuffer = new ComputeBuffer(1, 5 * sizeof(int), ComputeBufferType.IndirectArguments);
        chunk.argsBufferLOD = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        int chunkDim = resolution;
        int xOffset = x * resolution / 4;
        int yOffset = y * resolution / 4;

        initializeGrassShader.SetInt("_Dimension", resolution);
        initializeGrassShader.SetInt("_Scale", scale);
        initializeGrassShader.SetInt("x_offset", xOffset);
        initializeGrassShader.SetInt("y_offset", yOffset);
        initializeGrassShader.SetBuffer(0, "_GrassDataBuffer", chunk.posBuffer);
        initializeGrassShader.Dispatch(0, Mathf.CeilToInt(resolution / 8.0f), Mathf.CeilToInt(resolution / 8.0f), 1);

        // Setup args for graphics draw call.
        uint[] args = new uint[5] { (uint)grassMesh.GetIndexCount(0), (uint)(chunk.posBuffer.count), (uint)grassMesh.GetIndexStart(0), (uint)grassMesh.GetBaseVertex(0), 0 };
        chunk.argsBuffer.SetData(args);

        // Grass mesh 1: No rotation.
        chunk.grassMaterial = new Material(grassMaterial);
        chunk.grassMaterial.SetBuffer("positionBuffer", chunk.posBuffer);

        // Grass mesh 2: Copy of grass mesh 1 with 50 degrees rotation.
        chunk.grassMaterial2 = new Material(grassMaterial);
        chunk.grassMaterial2.SetFloat("_Rotation", 50.0f);
        chunk.grassMaterial2.SetBuffer("positionBuffer", chunk.posBuffer);

        // Grass mesh 3: Copy of grass mesh 1 with -50 degrees rotation.
        chunk.grassMaterial3 = new Material(grassMaterial);
        chunk.grassMaterial3.SetFloat("_Rotation", -50.0f);
        chunk.grassMaterial3.SetBuffer("positionBuffer", chunk.posBuffer);

        return chunk;
    }


    // Update is called once per frame. Draws the grass clusters.
    void Update()
    {

        // render by chunk
        for (int i = 0; i < numChunks * numChunks; i++)
        {
           
            Graphics.DrawMeshInstancedIndirect(grassMesh, 0, allChunks[i].grassMaterial, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), allChunks[i].argsBuffer);
            Graphics.DrawMeshInstancedIndirect(grassMesh, 0, allChunks[i].grassMaterial2, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), allChunks[i].argsBuffer);
            Graphics.DrawMeshInstancedIndirect(grassMesh, 0, allChunks[i].grassMaterial3, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), allChunks[i].argsBuffer);
        }

    }

    void OnDisable()
    {
        for (int i = 0; i < numChunks * numChunks; i++)
        {
            allChunks[i].posBuffer.Release();
            allChunks[i].argsBuffer.Release();
            allChunks[i].posBuffer = null;
            allChunks[i].argsBuffer = null;
            allChunks[i].grassMaterial = null;
            allChunks[i].grassMaterial2 = null;
            allChunks[i].grassMaterial3 = null;
        }
        voteBuffer.Release();
        voteBuffer = null;
        scanBuffer.Release();
        scanBuffer = null;
        groupSumArrayBuffer.Release();
        groupSumArrayBuffer = null;
        scannedGroupSumBuffer.Release();
        scannedGroupSumBuffer = null;

    }


}
