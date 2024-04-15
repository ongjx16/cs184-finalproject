using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassInstantiator : MonoBehaviour
{
    // -- Public variables to be assigned in unity editor. --
    public Mesh grassMesh;
    public Material grassMaterial;

    // `resolution`: The length of the terrain on the x and z axis.
    public int resolution = 100;

    // `scale`: The number of grass clusters to render in each length unit.
    // Eg. If `resolution` = 100 and `scale` = 2, we render 200 grass clusters on the x and z axis each.
    public int scale = 1;
    
    // -- Private variables to be assigned in code. --
    // `grassMaterial2`, `grassMaterial3`: Rotated grass image. Together with `grassMaterial`, these 3 grass images form the complete grass cluster. 
    private Material grassMaterial2, grassMaterial3;

    // `initializeGrassShader`: The compute shader script found in `Resources/clusterCompute.compute`. Runs on the GPU.
    private ComputeShader initializeGrassShader;

    // `grassDataBuffer`: Buffer / Array of all positions of grass clusters
    // `argsBuffer`: Buffer of indirect arguments to be used later on in in Graphics.DrawMeshInstancedIndirect to render the mesh with positions from the grass data buffer.
    private ComputeBuffer grassDataBuffer, argsBuffer;

    // `GrassCluster`: Data for each grass cluster.
    // A grass cluster consists of 3 meshes of grass images placed in a asterisk (*)-like arrangement.
    private struct GrassCluster
    {
        public Vector4 position;
    }

    // Start is called before the first frame update.
    // Sets up the necessary variables before drawing.
    void Start()
    {
        initializeGrassShader = Resources.Load<ComputeShader>("clusterCompute");
        if (initializeGrassShader == null)
        {
            Debug.LogError("compute shader not found");
        }

        resolution *= scale;
        grassDataBuffer = new ComputeBuffer(resolution * resolution, 4 * 4);
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(int), ComputeBufferType.IndirectArguments);

        // Updates the position of the grass.
        updateGrassBuffer();
    }

    // Updates the position of all grass clusters, sets up the grass materials for drawing.
    void updateGrassBuffer() {
        initializeGrassShader.SetInt("_Dimension", resolution);
        initializeGrassShader.SetInt("_Scale", scale);
        initializeGrassShader.SetBuffer(0, "_GrassDataBuffer", grassDataBuffer);
        initializeGrassShader.Dispatch(0, Mathf.CeilToInt(resolution / 8.0f), Mathf.CeilToInt(resolution / 8.0f), 1);

        // Setup args for graphics draw call.
        uint[] args = new uint[5] { (uint)grassMesh.GetIndexCount(0), (uint)(grassDataBuffer.count), (uint)grassMesh.GetIndexStart(0), (uint)grassMesh.GetBaseVertex(0), 0 };
        argsBuffer.SetData(args);

        // Grass mesh 1: No rotation.
        grassMaterial.SetBuffer("positionBuffer", grassDataBuffer);
        grassMaterial.SetFloat("_Rotation", 0.0f);

        // Grass mesh 2: Copy of grass mesh 1 with 50 degrees rotation.
        grassMaterial2 = new Material(grassMaterial);
        grassMaterial2.SetBuffer("positionBuffer", grassDataBuffer);
        grassMaterial2.SetFloat("_Rotation", 50.0f);
        
        // Grass mesh 3: Copy of grass mesh 2 with -50 degrees rotation.
        grassMaterial3 = new Material(grassMaterial);
        grassMaterial3.SetBuffer("positionBuffer", grassDataBuffer);
        grassMaterial3.SetFloat("_Rotation", -50.0f);
    }

    // Update is called once per frame. Draws the grass clusters.
    void Update()
    {
        Graphics.DrawMeshInstancedIndirect(grassMesh, 0, grassMaterial, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), argsBuffer);
        Graphics.DrawMeshInstancedIndirect(grassMesh, 0, grassMaterial2, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), argsBuffer);
        Graphics.DrawMeshInstancedIndirect(grassMesh, 0, grassMaterial3, new Bounds(Vector3.zero, new Vector3(-500.0f, 200.0f, 500.0f)), argsBuffer);
    }

    void OnDisable() 
    {
        grassDataBuffer.Release();
        argsBuffer.Release();
        grassDataBuffer = null;
        argsBuffer = null;
    }
}
