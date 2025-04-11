using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TestingNetcodeUI : MonoBehaviour
{
	[SerializeField] private Button startHostButton;
	[SerializeField] private Button startClientButton;
	[SerializeField] private Button startGame;

	private void Awake()
	{
		
		startHostButton.onClick.AddListener(() => 
		{
			NetworkManager.Singleton.StartHost();
	
			
		});
		
		startClientButton.onClick.AddListener(() => 
		{
			NetworkManager.Singleton.StartClient();
			
		});
		
		startGame.onClick.AddListener(()=>
		{
			if(NetworkManager.Singleton.IsHost)
			{
				NetworkManager.Singleton.SceneManager.LoadScene("PersistScene", LoadSceneMode.Single);
				
			}
		});

		
	}
	
	private void Hide()
	{
		gameObject.SetActive(false);
	}
}
