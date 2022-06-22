using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VelNet;

public class RPCTest : NetworkComponent
{
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			SendRPC(nameof(TestRPC), true);
		}
	}

	private void TestRPC()
	{
		Debug.LogError("RPC RECEIVED!");
	}

	public override void ReceiveBytes(byte[] message)
	{
		Debug.LogError("WOW. BYTES");
	}
}