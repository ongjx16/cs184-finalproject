using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundMapping : MonoBehaviour
{
    public GameObject grassTerrain; // Assign the terrain GameObject in the Inspector
    public GameObject groundTexture; // Assign the ground texture GameObject in the Inspector

    void Start()
    {
        if (grassTerrain != null)
        {
            // Attempt to get the GrassInstantiator component from the terrain GameObject
            GrassInstantiator grassScript = grassTerrain.GetComponent<GrassInstantiator>();
            if (grassScript != null)
            {
                // Get the field size of the terrain
                float fieldSize = grassScript.GetFieldSize();

                // Set the scale of the ground texture to match the size of the terrain
                groundTexture.transform.localScale = new Vector3(fieldSize, 1f, fieldSize);

                // Align the position of the ground texture with the terrain
                // Assuming the terrain's pivot is at the center, adjust if your setup is different
                groundTexture.transform.position = new Vector3(grassTerrain.transform.position.x, transform.position.y, grassTerrain.transform.position.z);
            }
            else
            {
                Debug.LogError("GrassInstantiator component not found on the terrain GameObject.");
            }
        }
        else
        {
            Debug.LogError("Terrain GameObject is not assigned to GroundMapping.");
        }
    }
}
