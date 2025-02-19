using UnityEngine;
using System.Collections;
using Unity.Netcode;
using UnityEngine.SceneManagement;



public class BootstrapManager : MonoBehaviour
{
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{
		StartCoroutine(LoadMainScene());
	}

	IEnumerator LoadMainScene()
	{
		yield return new WaitUntil(() =>NetworkManager.Singleton != null);
		SceneManager.LoadScene("MenuScene");

	}
}
