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

    // [Range(0, 1000.0f)]
    // public float lodCutoff = 1000.0f;

    // [Range(0, 1000.0f)]
    // public float distanceCutoff = 1000.0f;
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

        // numInstancesPerChunk = resolution * resolution;
        // numVoteThreadGroups = Mathf.CeilToInt(numInstancesPerChunk / 128.0f);
        // numGroupScanThreadGroups = Mathf.CeilToInt(numInstancesPerChunk / 1024.0f);

        // numThreadGroups = Mathf.CeilToInt(numInstancesPerChunk / 128.0f);
        // if (numThreadGroups > 128)
        // {
        //     int powerOfTwo = 128;
        //     while (powerOfTwo < numThreadGroups)
        //         powerOfTwo *= 2;

        //     numThreadGroups = powerOfTwo;
        // }
        // else
        // {
        //     while (128 % numThreadGroups != 0)
        //         numThreadGroups++;
        // }
        // args = new uint[5] { 0, 0, 0, 0, 0 };
        // args[0] = (uint)grassMesh.GetIndexCount(0);
        // args[1] = (uint)0;
        // args[2] = (uint)grassMesh.GetIndexStart(0);
        // args[3] = (uint)grassMesh.GetBaseVertex(0);

        // voteBuffer = new ComputeBuffer(numInstancesPerChunk, 4);
        // scanBuffer = new ComputeBuffer(numInstancesPerChunk, 4);
        // groupSumArrayBuffer = new ComputeBuffer(numThreadGroups, 4);
        // scannedGroupSumBuffer = new ComputeBuffer(numThreadGroups, 4);

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

        Vector3 c = new Vector3(0.0f, 0.0f, 0.0f);

        c.y = 0.0f;
        c.x = -(chunkDim * 0.5f * numChunks) + chunkDim * xOffset;
        c.z = -(chunkDim * 0.5f * numChunks) + chunkDim * yOffset;
        c.x += chunkDim * 0.5f;
        c.z += chunkDim * 0.5f;

        chunk.bounds = new Bounds(c, new Vector3(-chunkDim, 10.0f, chunkDim));

        return chunk;
    }

    // void CullGrass(GrassChunk chunk, Matrix4x4 VP, bool noLOD)
    // {
    //     //Reset Args
    //     if (noLOD)
    //         if (chunk.argsBuffer == null)
    //             Debug.LogError("chunk.argsBuffer is null");
    //         else
    //             chunk.argsBuffer.SetData(args);
    //     else
    //     {
    //         if (chunk.argsBufferLOD == null)
    //             Debug.LogError("chunk.argsBufferLOD is null");
    //         else
    //             chunk.argsBufferLOD.SetData(args);
    //     };


    //     // Vote
    //     cullGrassShader.SetMatrix("MATRIX_VP", VP);
    //     cullGrassShader.SetBuffer(0, "_GrassDataBuffer", chunk.posBuffer);
    //     cullGrassShader.SetBuffer(0, "_VoteBuffer", voteBuffer);
    //     cullGrassShader.SetVector("_CameraPosition", Camera.main.transform.position);
    //     cullGrassShader.SetFloat("_Distance", distanceCutoff);
    //     cullGrassShader.Dispatch(0, numVoteThreadGroups, 1, 1);

    //     // Scan Instances
    //     cullGrassShader.SetBuffer(1, "_VoteBuffer", voteBuffer);
    //     cullGrassShader.SetBuffer(1, "_ScanBuffer", scanBuffer);
    //     cullGrassShader.SetBuffer(1, "_GroupSumArray", groupSumArrayBuffer);
    //     cullGrassShader.Dispatch(1, numThreadGroups, 1, 1);

    //     // Scan Groups
    //     cullGrassShader.SetInt("_NumOfGroups", numThreadGroups);
    //     cullGrassShader.SetBuffer(2, "_GroupSumArrayIn", groupSumArrayBuffer);
    //     cullGrassShader.SetBuffer(2, "_GroupSumArrayOut", scannedGroupSumBuffer);
    //     cullGrassShader.Dispatch(2, numGroupScanThreadGroups, 1, 1);

    //     // Compact
    //     cullGrassShader.SetBuffer(3, "_GrassDataBuffer", chunk.posBuffer);
    //     cullGrassShader.SetBuffer(3, "_VoteBuffer", voteBuffer);
    //     cullGrassShader.SetBuffer(3, "_ScanBuffer", scanBuffer);
    //     cullGrassShader.SetBuffer(3, "_ArgsBuffer", noLOD ? chunk.argsBuffer : chunk.argsBufferLOD);
    //     cullGrassShader.SetBuffer(3, "_CulledGrassOutputBuffer", chunk.culledposBuffer);
    //     cullGrassShader.SetBuffer(3, "_GroupSumArray", scannedGroupSumBuffer);
    //     cullGrassShader.Dispatch(3, numThreadGroups, 1, 1);
    // }

    // Update is called once per frame. Draws the grass clusters.
    void Update()
    {
        // Matrix4x4 P = Camera.main.projectionMatrix;
        // Matrix4x4 V = Camera.main.transform.worldToLocalMatrix;
        // Matrix4x4 VP = P * V;


        // if (voteBuffer == null)
        // {
        //     Debug.LogError($"voteBuffer is null.");
        // }
        // if (scanBuffer == null)
        // {
        //     Debug.LogError($"scanBuffer is null.");
        // }
        // if (groupSumArrayBuffer == null)
        // {
        //     Debug.LogError($"groupSumArrayBuffer is null.");
        // }
        // if (scannedGroupSumBuffer == null)
        // {
        //     Debug.LogError($"scannedGroupSumBuffer is null.");
        // }


        // render by chunk
        for (int i = 0; i < numChunks * numChunks; i++)
        {
            // if (allChunks[i].posBuffer == null)
            // {
            //     Debug.LogError($"allChunks[i].posBuffer is null.");
            // }
            // if (allChunks[i].argsBuffer == null)
            // {
            //     Debug.LogError($"allChunks[i].argsBuffer is null.");
            // }

            // float dist = Vector3.Distance(Camera.main.transform.position, allChunks[i].bounds.center);

            // bool noLOD = dist < lodCutoff;
            // // bool noLOD = true;

            // CullGrass(allChunks[i], VP, noLOD);

            // if (noLOD)
            // {
            Graphics.DrawMeshInstancedIndirect(grassMesh, 0, allChunks[i].grassMaterial, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), allChunks[i].argsBuffer);
            Graphics.DrawMeshInstancedIndirect(grassMesh, 0, allChunks[i].grassMaterial2, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), allChunks[i].argsBuffer);
            Graphics.DrawMeshInstancedIndirect(grassMesh, 0, allChunks[i].grassMaterial3, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), allChunks[i].argsBuffer);
            // }
            // else
            // {
            //     if (allChunks[i].argsBufferLOD != null)
            //     {
            //         Graphics.DrawMeshInstancedIndirect(grassMesh, 0, allChunks[i].grassMaterial, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), allChunks[i].argsBufferLOD);
            //         Graphics.DrawMeshInstancedIndirect(grassMesh, 0, allChunks[i].grassMaterial2, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), allChunks[i].argsBufferLOD);
            //         Graphics.DrawMeshInstancedIndirect(grassMesh, 0, allChunks[i].grassMaterial3, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), allChunks[i].argsBufferLOD);
            //     }
            //     else
            //     {
            //         Debug.LogError("argsBufferLOD is null for chunk " + i);
            //     }


            // }

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
