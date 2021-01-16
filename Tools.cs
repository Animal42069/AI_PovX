using AIChara;
using AIProject;
using Manager;
using System;
using System.Linq;
using UnityEngine;

namespace AI_PovX
{
    public static class Tools
	{
		public static bool IsMainGame()
		{
			return BepInEx.Paths.ProcessName == "AI-Syoujyo";
		}

		public static bool IsHScene()
		{
			return Map.Instance.Player.CameraControl.Mode == CameraMode.H;
		}

		public static bool IsFishingScene()
		{
			return Map.Instance.Player.Controller.State is AIProject.Player.Fishing;
		}

		public static bool IsFreeRoam()
		{
			return Map.Instance.Player.Controller.State is AIProject.Player.Normal || Map.Instance.Player.Controller.State is AIProject.Player.Onbu;

		}

		public static bool ShouldHideHead()
		{
			return Controller.povEnabled && AI_PovX.HideHead.Value;
		}

		// Return the offset of the eyes in the head's object space.
		public static Vector3 GetEyesOffset(ChaControl chaCtrl)
		{
			Transform head = chaCtrl.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(Controller.headBone)).FirstOrDefault();

			Transform[] eyes = new Transform[2];
			eyes[0] = chaCtrl.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(Controller.leftEyePupil)).FirstOrDefault();
			eyes[1] = chaCtrl.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(Controller.rightEyePupil)).FirstOrDefault();

			if (AI_PovX.CameraPoVLocation.Value == AI_PovX.CameraLocation.LeftEye)
				return GetEyesOffsetInternal(head, eyes[0]);
			else if (AI_PovX.CameraPoVLocation.Value == AI_PovX.CameraLocation.RightEye)
				return GetEyesOffsetInternal(head, eyes[1]);

			return Vector3.Lerp(
				GetEyesOffsetInternal(head, eyes[0]),
				GetEyesOffsetInternal(head, eyes[1]),
				0.5f);
		}
		
		private static Vector3 GetEyesOffsetInternal(Transform head, Transform eye)
		{
			Vector3 offset = Vector3.zero;

			for (int bone = 0; bone < 50; bone++)
			{
				if (eye == null || eye == head)
					break;

				offset += eye.localPosition;
				eye = eye.parent;
			}

			return offset;
		}

		// Find smallest degrees to rotate in order to get to the next angle.
		public static float GetClosestAngle(float from, float to, out bool clockwise)
		{
			float angle = to - from;
			clockwise = (angle >= 0f && angle <= 180f) || angle <= -180f;

			if (angle < 0)
				angle += 360f;

			return clockwise ? angle : 360f - angle;
		}

		// Modulo without negative.
		public static float Mod2(float value, float mod)
		{
			if (value < 0)
				value = mod + (value % mod);

			return value % mod;
		}

		// Restrict angle where origin is at 0 angle.
		public static float AngleClamp(float value, float min, float max)
		{
			if (value > min && value < 360f - max)
				return min;
			else if (value < 360f - max && value > min)
				return 360f - max;

			return value;
		}

		// Return True if Vectors are close enough to each other
		public static bool VectorsEqual(Vector3 firstVector, Vector3 secondVector, float threshold = 0.01f)
		{
			if (Math.Abs(firstVector.x - secondVector.x) > threshold)
				return false;

			if (Math.Abs(firstVector.y - secondVector.y) > threshold)
				return false;

			if (Math.Abs(firstVector.z - secondVector.z) > threshold)
				return false;

			return true;
		}

		// Calculate Pitch necessary to align eyes with lookTarget
		public static float GetHeadPitch(Vector3 eyePosition, Vector3 lookTarget)
		{
			float headPitch = 0;
			float hypotenuse = Vector3.Distance(eyePosition, lookTarget);
			float side = Vector3.Distance(new Vector2(eyePosition.x, eyePosition.z), new Vector2(lookTarget.x, lookTarget.z));
			if (hypotenuse != 0)
				headPitch = (float)(Math.Acos(side / hypotenuse) * 180 / Math.PI);

			if (lookTarget.y > eyePosition.y)
				headPitch = -headPitch;

			headPitch = Tools.Mod2(headPitch, 360f);

			if (headPitch > 180)
				headPitch -= 360;
			if (headPitch < -180)
				headPitch += 360;

			return Mathf.Clamp(headPitch, -AI_PovX.HeadMaxPitch.Value, AI_PovX.HeadMaxPitch.Value);
		}
	}
}
