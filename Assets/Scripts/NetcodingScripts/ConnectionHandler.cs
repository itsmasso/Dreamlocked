using System;
using FishNet;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using UnityEngine;


public enum ConnectionType
{
	Host,
	Client
}

public class ConnectionHandler : MonoBehaviour
{
	private Tugboat tugboat;
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
		if(TryGetComponent(out Tugboat t))
		{
			tugboat = t;
		}
		else
		{
			Debug.LogError("Couldn't get tugboat!", this);
			return;
		}
		
		#if UNITY_EDITOR
		if(ParrelSync.ClonesManager.IsClone())
		{
			tugboat.StartConnection(false); //start a client connection
		}
		else
		{
			if(connectionType == ConnectionType.Host)
			{
				tugboat.StartConnection(true); //start a server connection
				tugboat.StartConnection(false); //start a client connection
			}
			else
			{
				tugboat.StartConnection(false); //start a client connection
			}
			
		}

		#endif
		
	}
}
