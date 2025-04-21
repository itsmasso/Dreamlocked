using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dissolve : MonoBehaviour
{
    [SerializeField] private List<MeshRenderer> rendererList = new List<MeshRenderer>();
    [SerializeField] private List<SkinnedMeshRenderer> skinnedRendererList = new List<SkinnedMeshRenderer>();
    [SerializeField] private float dissolveDuration;
    [SerializeField] private Material dissolveMat;
    private List<Material[]> dissolveMaterials = new List<Material[]>();

    public float GetDissolveDuration()
    {
        return dissolveDuration;
    }
    public void StartDissolvingSkinnedMesh()
    {
        dissolveMaterials.Clear();
        foreach (SkinnedMeshRenderer rend in skinnedRendererList)
        {
             if (rend == null) continue;
            Material[] newMats = new Material[rend.materials.Length];
            for (int i = 0; i < rend.materials.Length; i++)
            {
                Material newMat = new Material(dissolveMat);
                Texture mainTex = rend.materials[i].GetTexture("_MainTex");
                if (mainTex != null)
                    newMat.SetTexture("_MainTex", mainTex);

                newMats[i] = newMat;
            }

            rend.materials = newMats;
            dissolveMaterials.Add(newMats);
        }

        StartCoroutine(DissolveCoroutine());
    }
    public void StartDissolving()
    {
        dissolveMaterials.Clear();
        foreach (MeshRenderer rend in rendererList)
        {
             if (rend == null) continue;
            Material[] newMats = new Material[rend.materials.Length];
            for (int i = 0; i < rend.materials.Length; i++)
            {
                Material newMat = new Material(dissolveMat);
                Texture mainTex = rend.materials[i].GetTexture("_MainTex");
                if (mainTex != null)
                    newMat.SetTexture("_MainTex", mainTex);

                newMats[i] = newMat;
            }

            rend.materials = newMats;
            dissolveMaterials.Add(newMats);
        }
    }
    private IEnumerator DissolveCoroutine()
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
                    if(mat != null)
                    {
                        mat.SetFloat("_DissolveStrength", dissolveStrength);
                    }
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
                if(mat != null)
                {
                    mat.SetFloat("_DissolveStrength", 1f);
                }
            }
        }
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}
