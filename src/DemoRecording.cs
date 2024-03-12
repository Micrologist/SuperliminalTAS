using Rewired;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace SuperliminalTAS
{
    public class DemoRecording : MonoBehaviour
    {
        public int frame;

        private bool recording, playingBack = false;

        private Dictionary<string, List<bool>> button;
        private Dictionary<string, List<bool>> buttonDown;
        private Dictionary<string, List<bool>> buttonUp;

        private Dictionary<string, List<float>> axis;

        private Text statusText;

		private int fixedUpdates, updates;

		void Update()
		{
			updates++;
		}

		void FixedUpdate()
		{
			fixedUpdates++;
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
                    statusText.text = "idle";

				if (GameManager.GM.player != null)
				{
					var playerPos = GameManager.GM.player.transform.position;
					statusText.text += $"\nx: {playerPos.x:0.00} \ny: {playerPos.y:0.00} \nz: {playerPos.z:0.00}";
				}
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
        }

        private void StartRecording()
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

            recording = true;
            TASInput.StopPlayback();
            frame = 0;
			fixedUpdates = updates = 0;
		}

        private void StopRecording()
        {
            recording = false;
            frame = 0;
			fixedUpdates = updates = 0;
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
			fixedUpdates = updates = 0;
        }

        private void StopPlayback()
        {
            recording = false;
            playingBack = false;
            TASInput.StopPlayback();
            frame = 0;
			fixedUpdates = updates = 0;
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
            GameObject gameObject = new GameObject("TASMod_UI");
            gameObject.transform.parent = GameObject.Find("UI_PAUSE_MENU").transform.Find("Canvas");
            gameObject.AddComponent<CanvasGroup>().interactable = false;

            statusText = gameObject.AddComponent<Text>();
            statusText.fontSize = 40;
            foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
                if (font.name == "BebasNeue Bold")
                    statusText.font = font;

            var rect = statusText.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(Screen.currentResolution.width / 4, Screen.currentResolution.height / 5);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(25f, -25f);
        }

    }
}