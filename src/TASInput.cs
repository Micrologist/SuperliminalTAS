namespace SuperliminalTAS
{
	internal static class TASInput
	{
		public static bool blockAllInput = false;
		public static bool passthrough = true;
		public static bool disablePause = false;
		private static DemoRecorder recording;

		internal static bool GetButton(string actionName, bool originalResult)
		{
			if (blockAllInput) return false;

			if (passthrough || (actionName != "Jump" && actionName != "Grab" && actionName != "Rotate"))
				return originalResult;

			return recording.GetRecordedButton(actionName);
		}
		internal static bool GetButtonDown(string actionName, bool originalResult)
		{
            if (blockAllInput) return false;

			if (actionName == "Pause" && disablePause)
				return false;

            if (passthrough || (actionName != "Jump" && actionName != "Grab" && actionName != "Rotate"))
				return originalResult;

			return recording.GetRecordedButtonDown(actionName);
		}

		public static bool GetButtonUp(string actionName, bool originalResult)
		{
            if (blockAllInput) return false;

            if (passthrough || (actionName != "Jump" && actionName != "Grab" && actionName != "Rotate"))
				return originalResult;

			return recording.GetRecordedButtonUp(actionName);
		}

		internal static float GetAxis(string actionName, float originalResult)
		{
			if (blockAllInput) return 0f;

            if (passthrough || (actionName != "Look Horizontal" && actionName != "Look Vertical" && actionName != "Move Horizontal" && actionName != "Move Vertical"))
				return originalResult;

			return recording.GetRecordedAxis(actionName);
		}

		public static void StartPlayback(DemoRecorder recordingToPlay)
		{
			recording = recordingToPlay;
			passthrough = false;
		}

		public static void StopPlayback()
		{
			recording = null;
			passthrough = true;
		}
	}
}
