using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dissolve : MonoBehaviour
{
    [SerializeField] private List<MeshRenderer> rendererList = new List<MeshRenderer>();
    [SerializeField] private List<SkinnedMeshRenderer> skinnedRendererList = new List<SkinnedMeshRenderer>();
    [SerializeField] private float dissolveDuration;
    [SerializeField] private Material dissolveMat;
    private List<Material[]> originalMaterials = new List<Material[]>();
    private List<Material[]> dissolveMaterials = new List<Material[]>();

    public float GetDissolveDuration()
    {
        return dissolveDuration;
    }
    public void StartDissolvingSkinnedMesh()
    {
        // Prepare dissolve materials
        originalMaterials.Clear();
        dissolveMaterials.Clear();

        foreach (SkinnedMeshRenderer rend in skinnedRendererList)
        {
            Material[] originals = rend.materials;
            Material[] dissolved = new Material[originals.Length];

            for (int i = 0; i < originals.Length; i++)
            {
                Material newMat = new Material(dissolveMat);
                Texture originalTex = originals[i].GetTexture("_MainTex");
                if (originalTex != null)
                {
                    newMat.SetTexture("_MainTex", originalTex);
                }
                dissolved[i] = newMat;
            }

            originalMaterials.Add(originals);
            dissolveMaterials.Add(dissolved);

            // Apply the dissolve materials
            rend.materials = dissolved;
        }

        StartCoroutine(StartDissolveCoroutine());
    }
    public void StartDissolving()
    {
        // Prepare dissolve materials
        originalMaterials.Clear();
        dissolveMaterials.Clear();

        foreach (MeshRenderer rend in rendererList)
        {
            Material[] originals = rend.materials;
            Material[] dissolved = new Material[originals.Length];

            for (int i = 0; i < originals.Length; i++)
            {
                Material newMat = new Material(dissolveMat);
                Texture originalTex = originals[i].GetTexture("_MainTex");
                if (originalTex != null)
                {
                    newMat.SetTexture("_MainTex", originalTex);
                }
                dissolved[i] = newMat;
            }

            originalMaterials.Add(originals);
            dissolveMaterials.Add(dissolved);

            // Apply the dissolve materials
            rend.materials = dissolved;
        }

        StartCoroutine(StartDissolveCoroutine());
    }
    private IEnumerator StartDissolveCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < dissolveDuration)
        {
            float t = elapsed / dissolveDuration;
            float dissolveStrength = Mathf.Lerp(0, 1, t);

            foreach (Material[] mats in dissolveMaterials)
            {
                foreach (Material mat in mats)
                {
                    mat.SetFloat("_DissolveStrength", dissolveStrength);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Final snap
        foreach (Material[] mats in dissolveMaterials)
        {
            foreach (Material mat in mats)
            {
                mat.SetFloat("_DissolveStrength", 1f);
            }
        }
    }


}
