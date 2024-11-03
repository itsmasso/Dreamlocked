using System;
using FishNet;
using FishNet.Transporting;
using UnityEngine;


public enum ConnectionType
{
	Host,
	Client
}

public class ConnectionHandler : MonoBehaviour
{
	
	public ConnectionType connectionType;
	
	#if UNITY_EDITOR
	private void OnEnable() 
	{
		InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;
		
	}

	private void OnClientConnectionState(ClientConnectionStateArgs args)
	{
		if(args.ConnectionState == LocalConnectionState.Stopping)
		{
			UnityEditor.EditorApplication.isPlaying = false;
		}
	}
	
	private void OnDisable() 
	{
		InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;
	}
	#endif

	private void Start()
	{
		#if UNITY_EDITOR
		if(ParrelSync.ClonesManager.IsClone())
		{
			InstanceFinder.ClientManager.StartConnection();
		}
		else
		{
			if(connectionType == ConnectionType.Host)
			{
				InstanceFinder.ServerManager.StartConnection();
				InstanceFinder.ClientManager.StartConnection();
			}
			else
			{
				InstanceFinder.ClientManager.StartConnection();
			}
			
		}

		#endif
		
	}
}
