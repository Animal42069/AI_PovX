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

		public static bool ShouldHideHead()
		{
			return Controller.toggled && (
				Controller.inScene ||
				AI_PovX.HideHead.Value ||
				IsHScene()
			);
		}

		// Return the offset of the eyes in the neck's object space.
		public static Vector3 GetEyesOffset(ChaControl chaCtrl)
		{
			Transform head = chaCtrl.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_Head_s")).FirstOrDefault();

			Transform[] eyes = new Transform[2];
			eyes[0] = chaCtrl.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_pupil_s_L")).FirstOrDefault();
			eyes[1] = chaCtrl.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_pupil_s_R")).FirstOrDefault();

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

			for (int i = 0; i < 50; i++)
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

		public static float GetHeadRotationX(Vector3 eyePosition, Vector3 lookTarget)
		{
			float headRotationX = 0;
			float hypotenuse = Vector3.Distance(eyePosition, lookTarget);
			float side = Vector3.Distance(new Vector2(eyePosition.x, eyePosition.z), new Vector2(lookTarget.x, lookTarget.z));
			if (hypotenuse != 0)
				headRotationX = (float)(Math.Acos(side / hypotenuse) * 180 / Math.PI);

			if (lookTarget.y > eyePosition.y)
				headRotationX = -headRotationX;

			headRotationX = Tools.Mod2(headRotationX, 360f);

			if (headRotationX > 180)
				headRotationX -= 360;
			if (headRotationX < -180)
				headRotationX += 360;

			return Mathf.Clamp(headRotationX, -AI_PovX.HeadMaxX.Value, AI_PovX.HeadMinX.Value);
		}
	}
}
