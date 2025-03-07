using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlendShapeManager : MonoBehaviour
{
    public SkinnedMeshRenderer mesh1;
    public SkinnedMeshRenderer mesh2;
    public Slider[] sliders;  // Must be exactly 3 sliders

    [Header("Blend Shape Names")]
    public string[] blendShapeNames;  // Must contain exactly 3 names

    private int[] blendShapeIndices;

    private void Start()
    {
        // Check if blendShapeNames and sliders have exactly 3 entries
        if (blendShapeNames.Length != 3 || sliders.Length != 3)
        {
            Debug.LogError("You must have exactly 3 blend shape names and 3 sliders.");
            return;
        }

        // Initialize blend shape indices
        blendShapeIndices = new int[4];
        for (int i = 0; i < 3; i++)
        {
            blendShapeIndices[i] = mesh1.sharedMesh.GetBlendShapeIndex(blendShapeNames[i]);
            if (blendShapeIndices[i] == -1)
            {
                Debug.LogWarning($"Blend shape '{blendShapeNames[i]}' not found.");
            }
        }

        // Add listeners to sliders
        for (int i = 0; i < sliders.Length; i++)
        {
            int blendShapeIndex = i;
            sliders[i].onValueChanged.AddListener(value => UpdateBlendShape(blendShapeIndex, value));
        }
    }

    private void UpdateBlendShape(int index, float value)
    {
        if (blendShapeIndices[index] != -1)
        {
            mesh1.SetBlendShapeWeight(blendShapeIndices[index], value);
            mesh2.SetBlendShapeWeight(blendShapeIndices[index], value);
        }
    }

    // Manually set blend shape weight by name
    public void SetBlendShapeWeightByName(string blendShapeName, float value)
    {
        int index = System.Array.IndexOf(blendShapeNames, blendShapeName);
        if (index != -1)
        {
            UpdateBlendShape(index, value);
        }
        else
        {
            Debug.LogWarning($"Blend shape '{blendShapeName}' not found in the managed list.");
        }
    }

    // Save preset to PlayerPrefs
    public void SavePreset()
    {
        for (int i = 0; i < sliders.Length; i++)
        {
            PlayerPrefs.SetFloat("BlendShape" + i, sliders[i].value);
        }
        PlayerPrefs.Save();
    }

    // Load preset from PlayerPrefs
    public void LoadPreset()
    {
        for (int i = 0; i < sliders.Length; i++)
        {
            if (PlayerPrefs.HasKey("BlendShape" + i))
            {
                float value = PlayerPrefs.GetFloat("BlendShape" + i);
                sliders[i].value = value;
                UpdateBlendShape(i, value); // Ensure changes reflect on both meshes
            }
        }
    }
}
