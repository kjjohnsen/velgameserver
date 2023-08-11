using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace VelNet
{
	/// <summary>
	/// This is a base class for all objects that a player can instantiated/owned
	/// </summary>
	public class NetworkObject : MonoBehaviour
	{
		[Header("NetworkObject properties")] public VelNetPlayer owner;

		[Tooltip("Whether this object's ownership is transferrable. Should be true for player objects.")]
		public bool ownershipLocked;

		public bool IsMine => owner?.isLocal ?? false;

		/// <summary>
		/// This is forged from the combination of the creator's id (-1 in the case of a scene object) and an object id, so it's always unique for a room
		/// </summary>
		public string networkId;

		/// <summary>
		/// This is used internally to handle spawning of objects for players that joined late.
		/// This way objects can be spawned in a static location
		/// </summary>
		internal bool instantiatedWithTransform = false;

		internal Vector3 initialPosition;
		internal Quaternion initialRotation;

		/// <summary>
		/// This is generated at editor time and used to generate the network id at runtime.
		/// This is needed because finding all objects of type at runtime doesn't have a guaranteed order.
		/// </summary>
		public int sceneNetworkId;

		/// <summary>
		/// This may be empty if it's not a prefab (scene object)
		/// </summary>
		[Tooltip("For spawnable prefab objects")]
		public string prefabName;

		public bool isSceneObject;

		public List<NetworkComponent> syncedComponents;

		/// <summary>
		/// Player is the new owner
		/// </summary>
		public Action<VelNetPlayer> OwnershipChanged;

		public bool SendBytes(NetworkComponent component, bool isRpc, byte[] message, bool reliable = true)
		{
			// only needs to be owner if this isn't an RPC
			// RPC calls can be called by non-owner
			if (!IsMine && !isRpc)
			{
				Debug.LogError("Can't send message if owner is null or not local", this);
				return false;
			}

			if (!VelNetManager.InRoom)
			{
				Debug.LogError("Can't send message if not in a room", this);
				return false;
			}

			// send the message and an identifier for which component it belongs to
			if (!syncedComponents.Contains(component))
			{
				Debug.LogError("Can't send message if this component is not registered with the NetworkObject.", this);
				return false;
			}

			int componentIndex = syncedComponents.IndexOf(component);
			switch (componentIndex)
			{
				case > 127:
					Debug.LogError("Too many components.", component);
					return false;
				case < 0:
					Debug.LogError("WAAAAAAAH. NetworkObject doesn't have a reference to this component.", component);
					return false;
			}

			byte componentByte = (byte)(componentIndex << 1);
			// the leftmost bit determines if this is an rpc or not
			// this leaves only 128 possible NetworkComponents per NetworkObject
			componentByte |= (byte)(isRpc ? 1 : 0);

			return VelNetPlayer.SendMessage(this, componentByte, message, reliable);
		}


		public bool SendBytesToGroup(NetworkComponent component, bool isRpc, string group, byte[] message, bool reliable = true)
		{
			// only needs to be owner if this isn't an RPC
			// RPC calls can be called by non-owner
			if (!IsMine && !isRpc)
			{
				Debug.LogError("Can't send message if owner is null or not local", this);
				return false;
			}

			// send the message and an identifier for which component it belongs to
			int componentIndex = syncedComponents.IndexOf(component);
			switch (componentIndex)
			{
				case > 127:
					Debug.LogError("Too many components.", component);
					return false;
				case < 0:
					Debug.LogError("WAAAAAAAH. NetworkObject doesn't have a reference to this component.", component);
					return false;
			}

			byte componentByte = (byte)(componentIndex << 1);
			componentByte |= (byte)(isRpc ? 1 : 0);

			return VelNetPlayer.SendGroupMessage(this, group, componentByte, message, reliable);
		}

		public void ReceiveBytes(byte componentIdx, bool isRpc, byte[] message)
		{
			// send the message to the right component
			try
			{
				if (isRpc)
				{
					syncedComponents[componentIdx].ReceiveRPC(message);
				}
				else
				{
					syncedComponents[componentIdx].ReceiveBytes(message);
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"Error in handling message:\n{e}", this);
			}
		}

		public void TakeOwnership()
		{
			VelNetManager.TakeOwnership(networkId);
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// Sets up the interface for the CopyTransform script.
	/// </summary>
	[CustomEditor(typeof(NetworkObject))]
	public class NetworkObjectEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			NetworkObject t = target as NetworkObject;

			EditorGUILayout.Space();

			if (t == null) return;

			EditorGUILayout.HelpBox("Network Object. One per prefab pls.\nAssign components to the list to be synced.", MessageType.Info);

			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.Toggle("IsMine", t.IsMine);
			EditorGUILayout.TextField("Owner ID", t.owner?.userid.ToString() ?? "No owner");
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();

			if (EditorApplication.isPlaying && GUILayout.Button("Take ownership now."))
			{
				t.TakeOwnership();
			}

			if (GUILayout.Button("Find Network Components and add backreferences."))
			{
				NetworkComponent[] components = t.GetComponentsInChildren<NetworkComponent>();
				SerializedObject so = new SerializedObject(t);
				SerializedProperty prop = so.FindProperty("syncedComponents");
				prop.ClearArray();
				foreach (NetworkComponent comp in components)
				{
					prop.InsertArrayElementAtIndex(0);
					prop.GetArrayElementAtIndex(0).objectReferenceValue = comp;
					SerializedObject soComponent = new SerializedObject(comp);
					soComponent.FindProperty("networkObject").objectReferenceValue = t;
					soComponent.ApplyModifiedProperties();
				}

				so.ApplyModifiedProperties();
			}



			// make the sceneNetworkId a new unique value
			if (Application.isEditor && !Application.isPlaying && t.isSceneObject && t.sceneNetworkId == 0)
			{
				// find the first unused value
				int[] used = FindObjectsOfType<NetworkObject>().Select(o => o.sceneNetworkId).ToArray();
				int available = -1;
				for (int i = 1; i <= used.Max() + 1; i++)
				{
					if (!used.Contains(i))
					{
						available = i;
						break;
					}
				}

				SerializedObject so = new SerializedObject(t);
				SerializedProperty prop = so.FindProperty("sceneNetworkId");
				prop.intValue = available;
				so.ApplyModifiedProperties();
			}

			EditorGUILayout.Space();

			DrawDefaultInspector();
		}
	}
#endif
}