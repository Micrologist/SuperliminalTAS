using System;
using System.Collections.Generic;
using System.Text;

namespace SuperliminalTAS
{
	internal static class TASInput
	{
		public static bool playingRecording = false;
		private static DemoRecording recording;

		internal static bool GetButton(string actionName, bool originalResult)
		{
			if (!playingRecording || (actionName != "Jump" && actionName != "Grab" && actionName != "Rotate"))
				return originalResult;

			return recording.GetRecordedButton(actionName);
		}
		internal static bool GetButtonDown(string actionName, bool originalResult)
		{
			if (!playingRecording || (actionName != "Jump" && actionName != "Grab" && actionName != "Rotate"))
				return originalResult;

			return recording.GetRecordedButtonDown(actionName);
		}

		public static bool GetButtonUp(string actionName, bool originalResult)
		{
			if (!playingRecording || (actionName != "Jump" && actionName != "Grab" && actionName != "Rotate"))
				return originalResult;

			return recording.GetRecordedButtonUp(actionName);
		}

		internal static bool GetAnyButtonDown(bool originalResult)
		{
			return originalResult;
		}

		internal static float GetAxis(string actionName, float originalResult)
		{
			if (!playingRecording || (actionName != "Look Horizontal" && actionName != "Look Vertical" && actionName != "Move Horizontal" && actionName != "Move Vertical"))
				return originalResult;

			return recording.GetRecordedAxis(actionName);
		}

		public static void StartPlayback(DemoRecording recordingToPlay)
		{
			recording = recordingToPlay;
			playingRecording = true;
		}

		public static void StopPlayback()
		{
			recording = null;
			playingRecording = false;
		}
	}
}
