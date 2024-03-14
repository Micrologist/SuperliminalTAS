using SFB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Screen = UnityEngine.Screen;

namespace SuperliminalTAS
{
	public class DemoRecorder : MonoBehaviour
	{
		public int frame;

		private bool recording, playingBack = false;

		private Dictionary<string, List<bool>> button;
		private Dictionary<string, List<bool>> buttonDown;
		private Dictionary<string, List<bool>> buttonUp;

		private Dictionary<string, List<float>> axis;

		private Text statusText;


		private readonly StandaloneFileBrowserWindows fileBrowser = new();

		private readonly ExtensionFilter[] extensionList = new[] {
			new SFB.ExtensionFilter("Superliminal TAS Recording (*.slt)", "slt"),
			new SFB.ExtensionFilter("All Files", "*")
		};

		private readonly string demoDirectory = AppDomain.CurrentDomain.BaseDirectory + "\\demos";

		private void Awake()
		{
			if (!Directory.Exists(demoDirectory))
			{
				Directory.CreateDirectory(demoDirectory);
			}
			ResetLists();
		}

		private void LateUpdate()
		{
			HandleInput();
			if (recording)
			{
				RecordInputs();
				frame++;
			}
			else if (playingBack)
			{
				frame++;
				if (frame >= button["Jump"].Count)
				{
					StopPlayback();
				}
			}

			if (statusText == null && GameObject.Find("UI_PAUSE_MENU") != null)
			{
				GenerateStatusText();
			}

			if (statusText != null)
			{
				if (playingBack)
					statusText.text = "playback: " + frame + " / " + button["Jump"].Count;
				else if (recording)
					statusText.text = "recording: " + frame + " / ?";
				else
					statusText.text = "stopped: 0 / " + button["Jump"].Count;

				if (GameManager.GM.player != null)
				{
					var playerPos = GameManager.GM.player.transform.position;
					statusText.text += $"\n\nx: {playerPos.x:0.0000} \ny: {playerPos.y:0.0000} \nz: {playerPos.z:0.0000}";
				}

				statusText.text += "\n\nF5 - Play\nF6 - Stop\nF7 - Record\nF11 - Open\nF12 - Save";
			}
		}


		private void HandleInput()
		{
			if (recording)
			{
				if (Input.GetKeyDown(KeyCode.F6))
				{
					StopRecording();
				}
			}
			else if (playingBack)
			{
				if (Input.GetKeyDown(KeyCode.F6))
				{
					StopPlayback();
				}
			}
			else
			{
				if (Input.GetKeyDown(KeyCode.F5))
				{
					StartPlayback();
				}
				else if (Input.GetKeyDown(KeyCode.F7))
				{
					StartRecording();
				}
			}

			if (Input.GetKeyDown(KeyCode.F12))
			{
				UnityEngine.Cursor.lockState = CursorLockMode.None;
				UnityEngine.Cursor.visible = true;
				SaveDemo();
				UnityEngine.Cursor.visible = false;
			}
			if (Input.GetKeyDown(KeyCode.F11))
			{
				UnityEngine.Cursor.lockState = CursorLockMode.None;
				UnityEngine.Cursor.visible = true;
				OpenDemo();
				UnityEngine.Cursor.visible = false;
			}
		}

		private void OpenDemo()
		{
			var selectedFile = fileBrowser.OpenFilePanel("Open", demoDirectory, extensionList, false);
			if (selectedFile.FirstOrDefault() != null)
			{
				var stream = File.OpenRead(selectedFile.FirstOrDefault()?.Name);
				if (stream != null)
				{
					ReadFromFileStream(stream);
				}
			}
		}

		private void SaveDemo()
		{
			if (button["Jump"].Count == 0)
				return;

			var selectedFile = fileBrowser.SaveFilePanel("Save Recording as", demoDirectory, $"SuperliminalTAS-{System.DateTime.Now:yyyy-MM-dd-HH-mm-ss}.slt", extensionList);
			if (selectedFile != null)
			{
				if (!selectedFile.Name.EndsWith(".slt"))
					selectedFile.Name += ".slt";
				File.WriteAllBytes(selectedFile.Name, SerializeToByteArray());
			}
		}

		private void StartRecording()
		{
			ResetLists();
			recording = true;
			TASInput.StopPlayback();
			frame = 0;
			GameManager.GM.GetComponent<PlayerSettingsManager>()?.SetMouseSensitivity(2.0f);
		}

		private void ResetLists()
		{
			button = new()
			{
				["Jump"] = new(),
				["Grab"] = new(),
				["Rotate"] = new()
			};

			buttonUp = new()
			{
				["Jump"] = new(),
				["Grab"] = new(),
				["Rotate"] = new()
			};

			buttonDown = new()
			{
				["Jump"] = new(),
				["Grab"] = new(),
				["Rotate"] = new()
			};

			axis = new()
			{
				["Move Horizontal"] = new(),
				["Move Vertical"] = new(),
				["Look Horizontal"] = new(),
				["Look Vertical"] = new()
			};
		}

		private void StopRecording()
		{
			recording = false;
			frame = 0;
		}

		private void RecordInputs()
		{
			button["Jump"].Add(GameManager.GM.playerInput.GetButton("Jump"));
			button["Grab"].Add(GameManager.GM.playerInput.GetButton("Grab"));
			button["Rotate"].Add(GameManager.GM.playerInput.GetButton("Rotate"));

			buttonUp["Jump"].Add(GameManager.GM.playerInput.GetButtonUp("Jump"));
			buttonUp["Grab"].Add(GameManager.GM.playerInput.GetButtonUp("Grab"));
			buttonUp["Rotate"].Add(GameManager.GM.playerInput.GetButtonUp("Rotate"));

			buttonDown["Jump"].Add(GameManager.GM.playerInput.GetButtonDown("Jump"));
			buttonDown["Grab"].Add(GameManager.GM.playerInput.GetButtonDown("Grab"));
			buttonDown["Rotate"].Add(GameManager.GM.playerInput.GetButtonDown("Rotate"));

			axis["Move Horizontal"].Add(GameManager.GM.playerInput.GetAxis("Move Horizontal"));
			axis["Move Vertical"].Add(GameManager.GM.playerInput.GetAxis("Move Vertical"));
			axis["Look Horizontal"].Add(GameManager.GM.playerInput.GetAxis("Look Horizontal"));
			axis["Look Vertical"].Add(GameManager.GM.playerInput.GetAxis("Look Vertical"));
		}

		private void StartPlayback()
		{
			if (button["Jump"].Count < 1)
				return;
			recording = false;
			playingBack = true;
			TASInput.StartPlayback(this);
			frame = 0;
			GameManager.GM.GetComponent<PlayerSettingsManager>()?.SetMouseSensitivity(2.0f);
		}

		private void StopPlayback()
		{
			recording = false;
			playingBack = false;
			TASInput.StopPlayback();
			frame = 0;
		}

		internal bool GetRecordedButton(string actionName)
		{
			return button[actionName][frame];
		}

		internal bool GetRecordedButtonDown(string actionName)
		{
			return buttonDown[actionName][frame];
		}

		internal bool GetRecordedButtonUp(string actionName)
		{
			return buttonUp[actionName][frame];
		}

		internal float GetRecordedAxis(string actionName)
		{
			return axis[actionName][frame];
		}

		private void GenerateStatusText()
		{
			GameObject gameObject = new("TASMod_UI");
			gameObject.transform.parent = GameObject.Find("UI_PAUSE_MENU").transform.Find("Canvas");
			gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

			statusText = gameObject.AddComponent<Text>();
			statusText.fontSize = 40;
			foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
				if (font.name == "NotoSans-CondensedSemiBold")
					statusText.font = font;

			var rect = statusText.GetComponent<RectTransform>();
			rect.sizeDelta = new Vector2(Screen.currentResolution.width / 4, Screen.currentResolution.height);
			rect.pivot = new Vector2(0f, 1f);
			rect.anchorMin = new Vector2(0f, 1f);
			rect.anchorMax = new Vector2(0f, 1f);
			rect.anchoredPosition = new Vector2(25f, -25f);
		}

		private byte[] SerializeToByteArray()
		{
			if (button["Jump"].Count < 1)
			{
				return null;
			}

			string magic = "SUPERLIMINALTAS1";
			byte[] magicBytes = Encoding.ASCII.GetBytes(magic);
			byte[] lengthBytes = BitConverter.GetBytes(button["Jump"].Count);
			Dictionary<string, byte[]> axisBytes = new()
			{
				["Move Horizontal"] = FloatListToByteArray(axis["Move Horizontal"]),
				["Move Vertical"] = FloatListToByteArray(axis["Move Vertical"]),
				["Look Horizontal"] = FloatListToByteArray(axis["Look Horizontal"]),
				["Look Vertical"] = FloatListToByteArray(axis["Look Vertical"])
			};
			Dictionary<string, byte[]> buttonBytes = new()
			{
				["Jump"] = BoolListToByteArray(button["Jump"]),
				["Grab"] = BoolListToByteArray(button["Grab"]),
				["Rotate"] = BoolListToByteArray(button["Rotate"])
			};
			Dictionary<string, byte[]> buttonDownBytes = new()
			{
				["Jump"] = BoolListToByteArray(buttonDown["Jump"]),
				["Grab"] = BoolListToByteArray(buttonDown["Grab"]),
				["Rotate"] = BoolListToByteArray(buttonDown["Rotate"])
			};
			Dictionary<string, byte[]> buttonUpBytes = new()
			{
				["Jump"] = BoolListToByteArray(buttonUp["Jump"]),
				["Grab"] = BoolListToByteArray(buttonUp["Grab"]),
				["Rotate"] = BoolListToByteArray(buttonUp["Rotate"])
			};

			byte[] result;
			using (MemoryStream memoryStream = new())
			{
				memoryStream.Write(magicBytes, 0, magicBytes.Length);
				memoryStream.Write(lengthBytes, 0, lengthBytes.Length);

				memoryStream.Write(axisBytes["Move Horizontal"], 0, axisBytes["Move Horizontal"].Length);
				memoryStream.Write(axisBytes["Move Vertical"], 0, axisBytes["Move Vertical"].Length);
				memoryStream.Write(axisBytes["Look Horizontal"], 0, axisBytes["Look Horizontal"].Length);
				memoryStream.Write(axisBytes["Look Vertical"], 0, axisBytes["Look Vertical"].Length);

				memoryStream.Write(buttonBytes["Jump"], 0, buttonBytes["Jump"].Length);
				memoryStream.Write(buttonBytes["Grab"], 0, buttonBytes["Grab"].Length);
				memoryStream.Write(buttonBytes["Rotate"], 0, buttonBytes["Rotate"].Length);

				memoryStream.Write(buttonDownBytes["Jump"], 0, buttonDownBytes["Jump"].Length);
				memoryStream.Write(buttonDownBytes["Grab"], 0, buttonDownBytes["Grab"].Length);
				memoryStream.Write(buttonDownBytes["Rotate"], 0, buttonDownBytes["Rotate"].Length);

				memoryStream.Write(buttonUpBytes["Jump"], 0, buttonUpBytes["Jump"].Length);
				memoryStream.Write(buttonUpBytes["Grab"], 0, buttonUpBytes["Grab"].Length);
				memoryStream.Write(buttonUpBytes["Rotate"], 0, buttonUpBytes["Rotate"].Length);

				result = memoryStream.ToArray();
			}

			return result;
		}

		private bool ReadFromFileStream(FileStream stream)
		{
			byte[] buffer = new byte[16];
			stream.Read(buffer, 0, buffer.Length);
			var magic = Encoding.ASCII.GetString(buffer);
			Debug.Log("Magic: " + magic);

			if (magic != "SUPERLIMINALTAS1")
				return false;

			buffer = new byte[4];
			stream.Read(buffer, 0, buffer.Length);
			var length = BitConverter.ToInt32(buffer, 0);
			Debug.Log("Length: " + length);

			axis = new();
			buffer = new byte[length * 4];

			stream.Read(buffer, 0, buffer.Length);
			axis["Move Horizontal"] = DeserializeFloatList(buffer);

			stream.Read(buffer, 0, buffer.Length);
			axis["Move Vertical"] = DeserializeFloatList(buffer);

			stream.Read(buffer, 0, buffer.Length);
			axis["Look Horizontal"] = DeserializeFloatList(buffer);

			stream.Read(buffer, 0, buffer.Length);
			axis["Look Vertical"] = DeserializeFloatList(buffer);


			button = new();
			buffer = new byte[length];

			stream.Read(buffer, 0, buffer.Length);
			button["Jump"] = DeserializeBoolList(buffer);

			stream.Read(buffer, 0, buffer.Length);
			button["Grab"] = DeserializeBoolList(buffer);

			stream.Read(buffer, 0, buffer.Length);
			button["Rotate"] = DeserializeBoolList(buffer);


			buttonDown = new();

			stream.Read(buffer, 0, buffer.Length);
			buttonDown["Jump"] = DeserializeBoolList(buffer);

			stream.Read(buffer, 0, buffer.Length);
			buttonDown["Grab"] = DeserializeBoolList(buffer);

			stream.Read(buffer, 0, buffer.Length);
			buttonDown["Rotate"] = DeserializeBoolList(buffer);


			buttonUp = new();

			stream.Read(buffer, 0, buffer.Length);
			buttonUp["Jump"] = DeserializeBoolList(buffer);

			stream.Read(buffer, 0, buffer.Length);
			buttonUp["Grab"] = DeserializeBoolList(buffer);

			stream.Read(buffer, 0, buffer.Length);
			buttonUp["Rotate"] = DeserializeBoolList(buffer);


			return true;
		}

		private List<float> DeserializeFloatList(byte[] buffer)
		{
			List<float> result = new();

			for (int i = 0; i < buffer.Length / 4; i++)
			{
				result.Add(BitConverter.ToSingle(buffer, i * 4));
			}

			return result;
		}

		private List<bool> DeserializeBoolList(byte[] buffer)
		{
			List<bool> result = new();

			for (int i = 0; i < buffer.Length; i++)
			{
				result.Add(BitConverter.ToBoolean(buffer, i));
			}

			return result;
		}

		private byte[] FloatListToByteArray(List<float> list)
		{
			byte[] result;
			using (MemoryStream memoryStream = new())
			{
				foreach (float value in list)
				{
					byte[] buffer = BitConverter.GetBytes(value);
					memoryStream.Write(buffer, 0, buffer.Length);
				}
				result = memoryStream.ToArray();
			}
			return result;
		}

		private byte[] BoolListToByteArray(List<bool> list)
		{
			byte[] result;
			using (MemoryStream memoryStream = new())
			{
				foreach (bool value in list)
				{
					byte[] buffer = BitConverter.GetBytes(value);
					memoryStream.Write(buffer, 0, buffer.Length);
				}
				result = memoryStream.ToArray();
			}
			return result;
		}

	}
}
