using AIChara;
using AIProject;
using Manager;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AI_PovX
{
    public static class Controller
	{
		public static bool toggled = false;
		public static bool showCursor = false;

		public static Quaternion bodyQuaternion;
		public static float bodyAngle = 0f; // Actual body, not the camera.

		// Angle offsets are used for situations where the character can't move.
		// The offsets are added to the neck's current rotation.
		// This means that the values can be negative.
		public static float cameraAngleOffsetX = 0f;
		public static float cameraAngleOffsetY = 0f;
		public static float headAngleOffsetX = 0f;
		public static float headAngleOffsetY = 0f;
		public static float cameraAngleY = 0f;

		// 0 = Player; 1 = 1st Partner; 2 = 2nd Partner; 3 = ...
		public static int focus = 0;
		public static ChaControl chaCtrl;
		public static Vector3 eyeOffset = Vector3.zero;
		public static Vector3 backupHead;
		public static float backupFov;

		public static bool inScene;
		public static bool lockMaleHeadPosition;

		public static HScene hScene;
		public static string currentHMotion;

		private static readonly List<string> maleLockHeadAllHPositions = new List<string>() { "aia_f_10", "ait_f_00", "ait_f_07"};
		private static readonly List<string> maleLockHeadHPositions = new List<string>() { "aia_f_00", "aia_f_01", "aia_f_04", "aia_f_06", "aia_f_07", "aia_f_08", "aia_f_11", "aia_f_12", "aia_f_13", "aia_f_18", "aia_f_19", "aia_f_23", "aia_f_24", "aia_f_26", "ai3p", "h2a_f_00" };
		private static readonly List<string> lockHeadHMotionExceptions = new List<string>() { "Idle", "_A" };

		public static void TogglePoV(bool flag)
		{
			if (toggled == flag)
				return;

			toggled = flag;

			if (flag)
			{
				SetChaControl(FromFocus());

				cameraAngleOffsetX = cameraAngleOffsetY = 0f;
				headAngleOffsetX = headAngleOffsetY = 0f;

				cameraAngleY = chaCtrl.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_Head_s")).FirstOrDefault().eulerAngles.y;
				bodyQuaternion = Map.Instance.Player.Rotation;
				bodyAngle = bodyQuaternion.eulerAngles.y;
				backupFov = Camera.main.fieldOfView;
			}
			else
			{
				if (AI_PovX.HSceneLockCursor.Value)
				{
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
				}

				Camera.main.fieldOfView = backupFov;

				SetChaControl(null);
			}
		}

		public static void Update()
		{
			if (AI_PovX.PovKey.Value.IsDown())
				TogglePoV(!toggled);

			if (AI_PovX.CursorReleaseKey.Value.IsDown())
				showCursor = !showCursor;

			if (!toggled)
				return;

			if (Tools.IsHScene())
			{
				if (AI_PovX.CharaCycleKey.Value.IsDown())
				{
					focus = (focus + 1) % 3;

					if (focus != 0)
					{
						Actor[] females = HSceneManager.Instance.females;

						if (females[focus - 1] == null || females[focus - 1] == Map.Instance.Player)
						{
							focus = 0;

							SetChaControl(Map.Instance.Player.ChaControl);
						}
						else
							SetChaControl(females[focus - 1].ChaControl);
					}
					else
						SetChaControl(Map.Instance.Player.ChaControl);
				}
			}
			else if (focus != 0)
			{
				focus = 0;

				SetChaControl(Map.Instance.Player.ChaControl);
			}

			float sensitivity = AI_PovX.Sensitivity.Value;

			if (AI_PovX.ZoomKey.Value.IsPressed())
				sensitivity *= AI_PovX.ZoomFov.Value / AI_PovX.Fov.Value;

			float x = UnityEngine.Input.GetAxis("Mouse Y") * sensitivity;
			float y = UnityEngine.Input.GetAxis("Mouse X") * sensitivity;

			if (Cursor.lockState != CursorLockMode.None || AI_PovX.CameraDragKey.Value.IsPressed())
			{
				if (Tools.IsHScene() && lockMaleHeadPosition)
				{
					cameraAngleOffsetX = Mathf.Clamp(cameraAngleOffsetX - x, -(AI_PovX.CameraMaxX.Value - AI_PovX.HeadMaxX.Value), (AI_PovX.CameraMinX.Value - AI_PovX.HeadMinX.Value));
					cameraAngleOffsetY = Mathf.Clamp(cameraAngleOffsetY + y, -(AI_PovX.CameraSpanY.Value- AI_PovX.HeadMaxY.Value), (AI_PovX.CameraSpanY.Value - AI_PovX.HeadMaxY.Value));
					headAngleOffsetX = 0;
					headAngleOffsetY = 0;
				}
				else
				{
					cameraAngleOffsetX = Mathf.Clamp(cameraAngleOffsetX - x, -AI_PovX.CameraMaxX.Value, AI_PovX.CameraMinX.Value);
					cameraAngleOffsetY = Mathf.Clamp(cameraAngleOffsetY + y, -AI_PovX.CameraSpanY.Value, AI_PovX.CameraSpanY.Value);
					headAngleOffsetX = Mathf.Clamp(cameraAngleOffsetX, -AI_PovX.HeadMaxX.Value, AI_PovX.HeadMinX.Value);
					headAngleOffsetY = Mathf.Clamp(cameraAngleOffsetY, -AI_PovX.HeadMaxY.Value, AI_PovX.HeadMaxY.Value);
				}

				cameraAngleY = Tools.Mod2(cameraAngleY + y, 360f);
			}
		}

		public static void LateUpdate()
		{
			if (toggled)
			{
				bool hScene = Tools.IsHScene();

				// Make it so that the player doesn't go visible if they're not supposed to be in the scene.
				if (!hScene || Map.Instance.Player.ChaControl.visibleAll)
					Map.Instance.Player.ChaControl.visibleAll = true;

				if (hScene && AI_PovX.HSceneLockCursor.Value && !showCursor)
				{
					Cursor.lockState = CursorLockMode.Locked;
					Cursor.visible = false;
				}
			}

			if (showCursor)
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}

			if (AI_PovX.RevealAll.Value)
			{
				foreach (KeyValuePair<int, AgentActor> agent in Map.Instance.AgentTable)
					agent.Value.ChaControl.visibleAll = true;

				MerchantActor merchant = Map.Instance.Merchant;

				if (merchant != null)
					merchant.ChaControl.visibleAll = true;
			}
		}

		public static void SetChaControl(ChaControl next)
		{
			if (chaCtrl != null)
			{
				chaCtrl.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_Head_s")).FirstOrDefault().localRotation = Quaternion.Euler(0, 0, 0);

				if (backupHead != null)
					chaCtrl.objHeadBone.transform.localScale = backupHead;
			}

			chaCtrl = next;
			if (chaCtrl != null)
			{
				eyeOffset = Tools.GetEyesOffset(chaCtrl);
				backupHead = chaCtrl.objHeadBone.transform.localScale;

				// Scale Z coordinate to hide head.  Preserves shadows (mostly)
				if (Tools.ShouldHideHead())
					chaCtrl.objHeadBone.transform.localScale = new Vector3(chaCtrl.objHeadBone.transform.localScale.x, chaCtrl.objHeadBone.transform.localScale.y, AI_PovX.HideHeadScaleZ.Value);
			}
		}

		public static ChaControl FromFocus()
		{
			return focus == 0 ?
				Map.Instance.Player.ChaControl :
				HSceneManager.Instance.females[focus - 1]?.ChaControl;
		}


		public static void RotatePlayerTowardsCharacter(ChaControl character)
        {
			PlayerActor player = Map.Instance.Player;

			Vector3 playerPosition = player.Position;
			Vector3 agentPosition = character.objBodyBone.transform.position;

			Vector2 playerForward = new Vector2(player.Forward.x, player.Forward.z);
			Vector2 turnVector = new Vector2(agentPosition.x - playerPosition.x, agentPosition.z - playerPosition.z);

			bodyAngle = player.Rotation.eulerAngles.y - Vector2.SignedAngle(playerForward, turnVector);
			player.Rotation = bodyQuaternion = Quaternion.Euler(0f, bodyAngle, 0f);
			var head = player.ChaControl.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_Head_s")).FirstOrDefault();
			var leftEye = player.ChaControl.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_pupil_s_L")).FirstOrDefault();
			var rightEye = player.ChaControl.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_pupil_s_R")).FirstOrDefault();

			head.localRotation = Quaternion.Euler(0, 0, 0);
			Vector3 playerEyePosition = Vector3.Lerp(leftEye.position, rightEye.position, 0.5f);
			Vector3 playerLookPosition = character.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_Mune00")).FirstOrDefault().position;

			headAngleOffsetX = cameraAngleOffsetX = Tools.GetHeadRotationX(playerEyePosition, playerLookPosition);
			headAngleOffsetY = cameraAngleOffsetY = 0;
			cameraAngleY = head.eulerAngles.y;

			head.localRotation = Quaternion.Euler(headAngleOffsetX, headAngleOffsetY, 0);

			Camera.main.transform.rotation = head.rotation;
			Camera.main.transform.Rotate(new Vector3(cameraAngleOffsetX - headAngleOffsetX, cameraAngleOffsetY - headAngleOffsetY, 0f));

			SetCamera(head);

			leftEye = character.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_pupil_s_L")).FirstOrDefault();
			rightEye = character.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_pupil_s_R")).FirstOrDefault();

			Vector3 characterEyePosition = Vector3.Lerp(leftEye.position, rightEye.position, 0.5f);
			Vector3 characterLookPosition = playerEyePosition;

			RotateCharacterHead(character, Tools.GetHeadRotationX(characterEyePosition, characterLookPosition));
		}

		public static void RotateCharacterHead(ChaControl character, float rotationX)
        {
			Transform head = character.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_Head_s")).FirstOrDefault();

			if (head == null)
				return;

			head.localEulerAngles = new Vector3(rotationX, 0, 0);
		}

		public static void SetCamera(Transform head)
		{
			Camera.main.fieldOfView =
				AI_PovX.ZoomKey.Value.IsPressed() ?
					AI_PovX.ZoomFov.Value :
					AI_PovX.Fov.Value;
			Camera.main.transform.position =
				head.position +
				(AI_PovX.OffsetX.Value + eyeOffset.x) * head.right +
				(AI_PovX.OffsetY.Value + eyeOffset.y) * head.up +
				(AI_PovX.OffsetZ.Value + eyeOffset.z) * head.forward;
			Camera.main.nearClipPlane = AI_PovX.NearClip.Value;
		}

		// Used for fishing scene.
		public static void FishingPoV()
		{
			if (!inScene)
			{
				inScene = true;

				// Refresh when switching PoV modes.
				SetChaControl(FromFocus());
			}

			PlayerActor player = Map.Instance.Player;
			AIProject.MiniGames.Fishing.Lure lure = player.GetComponentInChildren<AIProject.MiniGames.Fishing.FishingManager>().lure;

			Transform head = player.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_Head_s")).FirstOrDefault();
			Quaternion oldRotation = head.localRotation;

			if (lure == null || (lure.state != AIProject.MiniGames.Fishing.Lure.State.Float && lure.state != AIProject.MiniGames.Fishing.Lure.State.Hit))
				head.localRotation = Quaternion.Euler(0, 0, 0);
			else
				head.LookAt(lure.transform.position);

			head.localRotation = Quaternion.RotateTowards(oldRotation, head.localRotation, 180f * Time.deltaTime);

			Camera.main.transform.rotation = head.rotation;
			SetCamera(head);
		}

		// Used for scenes where the focused character cannot be controlled.
		public static void ScenePoV()
		{
			if (!inScene)
			{
				inScene = true;

				// Refresh when switching PoV modes.
				SetChaControl(FromFocus());
			}

			Transform head = chaCtrl.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_Head_s")).FirstOrDefault();
			head.localRotation = Quaternion.Euler(headAngleOffsetX, headAngleOffsetY, 0);

			Camera.main.transform.rotation = head.rotation;
			Camera.main.transform.Rotate(new Vector3(cameraAngleOffsetX - headAngleOffsetX, cameraAngleOffsetY - headAngleOffsetY, 0f));
			SetCamera(head);
		}

		// PoV exclusively for the player.
		public static void FreeRoamPoV()
		{
			PlayerActor player = Map.Instance.Player;

			if (player.Controller.State is AIProject.Player.Fishing)
            {
				FishingPoV();
				return;
            }

			// When the player is unable to move, treat it as a scene.
			if (!(player.Controller.State is AIProject.Player.Normal))
			{
				ScenePoV();
				return;
			}

			if (inScene)
			{
				inScene = false;

				// Refresh when switching PoV modes.
				SetChaControl(FromFocus());
			}

			if (!AI_PovX.RotateHead.Value || player.StateInfo.move.magnitude > 0f)
			{
				// Move entire body when moving.
				bodyAngle = cameraAngleY;
				bodyQuaternion = Quaternion.Euler(0f, bodyAngle, 0f);
				headAngleOffsetY = cameraAngleOffsetY = 0;
			}
			else
			{
				// Rotate head first. If head rotation is at the limit, rotate body.
				float angle = Tools.GetClosestAngle(bodyAngle, cameraAngleY, out bool clockwise);

				if (angle > AI_PovX.HeadMaxY.Value)
				{
					if (clockwise)
						bodyAngle = Tools.Mod2(bodyAngle + angle - AI_PovX.HeadMaxY.Value, 360f);
					else
						bodyAngle = Tools.Mod2(bodyAngle - angle + AI_PovX.HeadMaxY.Value, 360f);

					bodyQuaternion = Quaternion.Euler(0f, bodyAngle, 0f);
				}
			}

			player.Rotation = bodyQuaternion;
			Transform head = chaCtrl.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_Head_s")).FirstOrDefault();
			head.localRotation = Quaternion.Euler(headAngleOffsetX, headAngleOffsetY, 0);

			if (AI_PovX.HeadBob.Value)
			{
				Camera.main.transform.rotation = head.rotation;
				Camera.main.transform.Rotate(new Vector3(cameraAngleOffsetX - headAngleOffsetX, cameraAngleOffsetY - headAngleOffsetY, 0f));
			}
			else
            {
				Camera.main.transform.rotation = Quaternion.Euler(cameraAngleOffsetX, cameraAngleY, 0f);
			}

			SetCamera(head);
		}

		public static void CheckHSceneHeadLock(string hMotion = null)
		{
			lockMaleHeadPosition = false;

			if (!Tools.IsHScene() || hScene == null)
				return;

			string currentHAnimation = hScene.ctrlFlag.nowAnimationInfo.fileFemale;

			if (hMotion != null)
				currentHMotion = hMotion;

			if (currentHAnimation == null || currentHMotion == null)
				return;

			if (maleLockHeadAllHPositions.Contains(currentHAnimation) || 
			   (maleLockHeadHPositions.Contains(currentHAnimation) && !lockHeadHMotionExceptions.Contains(currentHMotion)))
				lockMaleHeadPosition = true;
		}
	}
}
