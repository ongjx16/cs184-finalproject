using UnityEngine;

public class GrassMesh : MonoBehaviour
{
    public float width = 10;
    public float height = 10;

    // Public variable to store the generated mesh
    public Mesh generatedMesh { get; private set; }
    public Material grassMaterial;

    void Awake()
    {
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = grassMaterial;

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();

        generatedMesh = new Mesh(); // Use the property to store the mesh

        // vertices, tris, normals, and uv calculations
        // Initialize vertices list for 3 intersecting quads (12 vertices total)
        Vector3[] vertices = new Vector3[12];

        // Initialize triangles list (3 quads * 2 triangles per quad * 3 vertices per triangle)
        int[] tris = new int[18];

        // Initialize normals list (12 vertices total)
        Vector3[] normals = new Vector3[12];

        // Initialize UVs list (for texturing the quads, assuming simple mapping)
        Vector2[] uv = new Vector2[12];

        float angleOffset = 60.0f; // degrees between each quad
        float halfHeight = height * 0.5f;

        for (int i = 0; i < 3; i++) // 3 blades
        {
            // Calculate the rotation for each quad
            Quaternion rotation = Quaternion.Euler(0f, angleOffset * i, 0f);

            // Calculate the normal for each quad (pointing outwards)
            Vector3 normal = rotation * Vector3.forward;

            // Calculate the vertices for each quad
            int baseIndex = i * 4;
            vertices[baseIndex + 0] = rotation * new Vector3(-halfHeight, 0f, 0f);
            vertices[baseIndex + 1] = rotation * new Vector3(halfHeight, 0f, 0f);
            vertices[baseIndex + 2] = rotation * new Vector3(-halfHeight, height, 0f);
            vertices[baseIndex + 3] = rotation * new Vector3(halfHeight, height, 0f);

            // Triangles for each quad
            int triBaseIndex = i * 6;
            tris[triBaseIndex + 0] = baseIndex + 0;
            tris[triBaseIndex + 1] = baseIndex + 2;
            tris[triBaseIndex + 2] = baseIndex + 1;
            tris[triBaseIndex + 3] = baseIndex + 2;
            tris[triBaseIndex + 4] = baseIndex + 3;
            tris[triBaseIndex + 5] = baseIndex + 1;

            // Assign normals pointing outwards
            normals[baseIndex + 0] = normal;
            normals[baseIndex + 1] = normal;
            normals[baseIndex + 2] = normal;
            normals[baseIndex + 3] = normal;

            // UVs can be set here if necessary
            uv[baseIndex + 0] = new Vector2(0f, 0f);
            uv[baseIndex + 1] = new Vector2(1f, 0f);
            uv[baseIndex + 2] = new Vector2(0f, 1f);
            uv[baseIndex + 3] = new Vector2(1f, 1f);
        }

        // Assign to mesh
        generatedMesh.vertices = vertices;
        generatedMesh.triangles = tris;
        generatedMesh.normals = normals;
        generatedMesh.uv = uv;

        generatedMesh.RecalculateBounds();

        meshFilter.mesh = generatedMesh; // Assign the generated mesh to the MeshFilter
        Debug.Log("GrassMesh created with " + generatedMesh.vertexCount + " vertices.");
    }
}
