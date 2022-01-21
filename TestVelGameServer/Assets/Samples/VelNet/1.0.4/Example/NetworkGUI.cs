using System.Collections;
using System.Collections.Generic;
using System.Text;
using Dissonance;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace VelNet
{
	public class NetworkGUI : MonoBehaviour
	{
		[FormerlySerializedAs("networkManager")] public VelNetManager velNetManager;
		public InputField userInput;
		public InputField sendInput;
		public InputField roomInput;
		public Text messages;
		public List<string> messageBuffer;
		public Dropdown microphones;
		DissonanceComms comms;

		public void HandleSend()
		{
			if (sendInput.text != "")
			{
				VelNetManager.SendToRoom(Encoding.UTF8.GetBytes(sendInput.text));
			}
		}

		public void HandleLogin()
		{
			if (userInput.text != "")
			{
				VelNetManager.Login(userInput.text, "nopass");
			}
		}

		public void HandleGetRooms()
		{
			if (VelNetManager.instance.connected)
			{
				VelNetManager.GetRooms();
			}
		}
		public void HandleJoin()
		{
			if (roomInput.text != "")
			{
				VelNetManager.Join(roomInput.text);
			}
		}

		public void HandleLeave()
		{
			VelNetManager.Leave();
		}

		// Start is called before the first frame update
		private void Start()
		{
			comms = FindObjectOfType<DissonanceComms>();
			microphones.AddOptions(new List<string>(Microphone.devices));

			/* todo
			VelNetManager.MessageReceived += (m) =>
			{
				string s = m.type + ":" + m.sender + ":" + m.text;
				messageBuffer.Add(s);
				messages.text = "";


				if (messageBuffer.Count > 10)
				{
					messageBuffer.RemoveAt(0);
				}

				foreach (string msg in messageBuffer)
				{
					messages.text = messages.text + msg + "\n";
				}
			};
			*/
			StartCoroutine(testes());
			
			
		}
		IEnumerator testes()
		{
			yield return new WaitForSeconds(1.0f); 
			HandleLogin(); 
			yield return new WaitForSeconds(1.0f); 
			HandleJoin();
			yield return null;
		}

		public void handleMicrophoneSelection()
		{
			comms.MicrophoneName = microphones.options[microphones.value].text;
		}
	}
}