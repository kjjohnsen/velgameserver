using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VelNet;

namespace VelNetExample
{
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
			Debug.Log("RPC RECEIVED!");
		}

		public override void ReceiveBytes(byte[] message)
		{
			Debug.Log("WOW. BYTES");
		}
	}
}