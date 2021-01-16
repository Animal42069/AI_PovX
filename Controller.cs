using AIChara;
using AIProject;
using Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AI_PovX
{
    public static class Controller
	{
		public static bool povEnabled = false;
		public static bool showCursor = false;

		// PoV Body world Yaw
		public static float bodyWorldYaw = 0f;
		// Camera world Yaw
		public static float cameraWorldYaw = 0f;

		// total camera rotation relative to body forward
		public static float cameraLocalPitch = 0f;
		public static float cameraLocalYaw = 0f;
		// portion of camera rotation that is acheived through head/neck rotation
		public static float headLocalPitch = 0f;
		public static float headLocalYaw = 0f;
		// portion of camera rotation that is acheived through eye rotation
		public static float eyeLocalPitch = 0f;
		public static float eyeLocalYaw = 0f;

		// 0 = Player; 1 = 1st Partner; 2 = 2nd Partner; 3 = ...
		public static Focus currentFocus = Focus.Player;
		public static ChaControl povCharacter;
		public static Vector3 eyeOffset = Vector3.zero;
		public static Vector3 normalHeadScale;
		public static float backupFoV;

		public static bool inScene;
		public static bool inHScene;
		public static bool lockMaleHeadPosition;

		public static HScene hScene;
		public static string currentHMotion;

		private static readonly List<string> maleLockHeadAllHPositions = new List<string>() { "aia_f_10", "ait_f_00", "ait_f_07"};
		private static readonly List<string> maleLockHeadHPositions = new List<string>() { "aia_f_00", "aia_f_01", "aia_f_04", "aia_f_06", "aia_f_07", "aia_f_08", "aia_f_11", "aia_f_12", "aia_f_13", "aia_f_18", "aia_f_19", "aia_f_23", "aia_f_24", "aia_f_26", "ai3p", "h2a_f_00" };
		private static readonly List<string> lockHeadHMotionExceptions = new List<string>() { "Idle", "_A" };

		internal static readonly string lowerNeckBone = "cf_J_Neck";
		internal static readonly string upperNeckBone = "cf_J_Head";
		internal static readonly string headBone = "cf_J_Head_s";
		internal static readonly string leftEyeBone = "cf_J_eye_rs_L";
		internal static readonly string rightEyeBone = "cf_J_eye_rs_R";
		internal static readonly string leftEyePupil = "cf_J_pupil_s_L";
		internal static readonly string rightEyePupil = "cf_J_pupil_s_R";
	
		public static Transform agentHead;
		public static Vector3 agentHeadTargetRotation = Vector3.zero;
		public static bool agentHeadRotationReached = true;
		public static bool playerBodyRotationReached = true;

		private static Transform povLowerNeck;
		private static Transform povUpperNeck;
		private static Transform povHead;
//		private static Transform povEyeLeft;
//		private static Transform povEyeRight;
//		private static Transform povPupilLeft;
//		private static Transform povPupilRight;

		public enum Focus
		{
			Player,
			FirstAgent,
			SecondAgent
		}

		public static void Update()
		{
			if (AI_PovX.PovKey.Value.IsDown())
				EnablePoV(!povEnabled);

			if (AI_PovX.CursorReleaseKey.Value.IsDown())
				showCursor = !showCursor;

			if (!povEnabled)
				return;

			if (inHScene && AI_PovX.CharaCycleKey.Value.IsDown())
				SetPoVCharacter(GetCharacterFromFocus(GetNextValidFocus(currentFocus)));
			else if (!inHScene && currentFocus != Focus.Player)
				SetPoVCharacter(GetCharacterFromFocus(Focus.Player));

			UpdateMouseLook();
			UpdateAgentHeadRotation();
		}

		public static void LateUpdate()
		{
			if (povEnabled)
			{
				inHScene = Tools.IsHScene();

				// Make it so that the player doesn't go visible if they're not supposed to be in the scene.
				if (!inHScene || Map.Instance.Player.ChaControl.visibleAll)
					Map.Instance.Player.ChaControl.visibleAll = true;

				if (inHScene && AI_PovX.HSceneLockCursor.Value && !showCursor)
				{
					Cursor.lockState = CursorLockMode.Locked;
					Cursor.visible = false;
				}

				if (inHScene && agentHead != null)
					ResetAgentHeadRotation();
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

			if (AI_PovX.RevealPartner.Value)
			{
				if (Map.Instance.Player.AgentPartner != null)
					Map.Instance.Player.AgentPartner.ChaControl.visibleAll = true;
			}
		}

		public static void EnablePoV(bool enable)
		{
			if (povEnabled == enable)
				return;

			povEnabled = enable;
			if (enable)
			{
				SetPoVCharacter(GetCharacterFromFocus(currentFocus));

				cameraLocalPitch = headLocalPitch = eyeLocalPitch = 0f;
				cameraLocalYaw = headLocalYaw = eyeLocalYaw = 0f;

				cameraWorldYaw = povHead.eulerAngles.y;
				bodyWorldYaw = Map.Instance.Player.Rotation.eulerAngles.y;
				backupFoV = Camera.main.fieldOfView;
			}
			else
			{
				if (AI_PovX.HSceneLockCursor.Value)
				{
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
				}

				Camera.main.fieldOfView = backupFoV;
				SetPoVCharacter(null);
			}
		}

		public static void SetPoVCharacter(ChaControl character)
        {
			if (povCharacter == character)
				return;

            if (povCharacter != null)
            {
				povUpperNeck.localRotation = Quaternion.identity;
				povLowerNeck.localRotation = Quaternion.identity;
				povHead.localRotation = Quaternion.identity;
				eyeOffset = Vector3.zero;

				if (normalHeadScale != null)
                    povCharacter.objHeadBone.transform.localScale = normalHeadScale;
            }

            povCharacter = character;
            if (povCharacter == null)        
                return;
            
            povUpperNeck = povCharacter.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(upperNeckBone)).FirstOrDefault();
            povLowerNeck = povCharacter.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(lowerNeckBone)).FirstOrDefault();
            povHead = povCharacter.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(headBone)).FirstOrDefault();
		//	povEyeLeft = povCharacter.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(leftEyeBone)).FirstOrDefault();
		//	povEyeRight = povCharacter.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(rightEyeBone)).FirstOrDefault();
		//	povPupilLeft = povCharacter.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(leftEyePupil)).FirstOrDefault();
		//	povPupilRight = povCharacter.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(rightEyePupil)).FirstOrDefault();

			normalHeadScale = povCharacter.objHeadBone.transform.localScale;

			CalculateEyesOffset();
			AdjustPoVHeadScale();
		}

        public static ChaControl GetCharacterFromFocus(Focus focus)
		{
			currentFocus = focus;

            switch (focus)
            {
				case Focus.Player: return Map.Instance.Player.ChaControl;
				case Focus.FirstAgent: return HSceneManager.Instance.females[0]?.ChaControl;
				case Focus.SecondAgent: return HSceneManager.Instance.females[1]?.ChaControl;
				default: return null;
			}
		}

		public static Focus GetNextFocus(Focus focus)
		{
			switch (focus)
			{
				case Focus.Player: return Focus.FirstAgent;
				case Focus.FirstAgent: return Focus.SecondAgent;
				case Focus.SecondAgent: return Focus.Player;
				default: return Focus.Player;
			}
		}

		public static Focus GetNextValidFocus(Focus focus)
		{
			focus = GetNextFocus(focus);

			if (GetCharacterFromFocus(focus) == null)
				return Focus.Player;

			return focus;
		}

		public static void RotatePlayerTowardsCharacter(ChaControl character)
        {
			if (!povEnabled || currentFocus != Focus.Player)
				return;

			PlayerActor player = Map.Instance.Player;
			if (player == null)
				return;

			Vector3 playerPosition = player.Position;
			Vector3 agentPosition = character.objBodyBone.transform.position;

			Vector2 playerForward = new Vector2(player.Forward.x, player.Forward.z);
			Vector2 turnVector = new Vector2(agentPosition.x - playerPosition.x, agentPosition.z - playerPosition.z);

			bodyWorldYaw = player.Rotation.eulerAngles.y - Vector2.SignedAngle(playerForward, turnVector);
			var leftEye = player.ChaControl.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(leftEyePupil)).FirstOrDefault();
			var rightEye = player.ChaControl.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(rightEyePupil)).FirstOrDefault();

			povHead.localRotation = povLowerNeck.localRotation = povUpperNeck.localRotation = Quaternion.identity;
			Vector3 playerEyePosition = Vector3.Lerp(leftEye.position, rightEye.position, 0.5f);
			Vector3 playerLookPosition = character.GetComponentsInChildren<Transform>().Where(x => x.name.Contains(lowerNeckBone)).FirstOrDefault().position;

			cameraLocalPitch = Tools.GetHeadPitch(playerEyePosition, playerLookPosition);
			headLocalPitch = cameraLocalPitch * AI_PovX.HeadMaxPitch.Value / (AI_PovX.EyeMaxPitch.Value + AI_PovX.HeadMaxPitch.Value);
			eyeLocalPitch = cameraLocalPitch - headLocalPitch;
			eyeLocalYaw = headLocalYaw = cameraLocalYaw = 0;
			cameraWorldYaw = povHead.eulerAngles.y;
			playerBodyRotationReached = false;

			leftEye = character.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(leftEyePupil)).FirstOrDefault();
			rightEye = character.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(rightEyePupil)).FirstOrDefault();

			Vector3 characterEyePosition = Vector3.Lerp(leftEye.position, rightEye.position, 0.5f);
			Vector3 characterLookPosition = playerEyePosition;

			RotateCharacterHead(character, Tools.GetHeadPitch(characterEyePosition, characterLookPosition));
		}

		public static void RotateCharacterHead(ChaControl character, float rotationX)
        {
			if (!povEnabled)
				return;

			agentHead = character.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(headBone)).FirstOrDefault();

			if (agentHead == null)
				return;

			agentHeadTargetRotation = new Vector3(rotationX, 0, 0);
			agentHeadRotationReached = false;
		}

		public static void UpdateCamera(Transform head, Vector3 offsetRotation)
        {
			Camera.main.transform.rotation = head.rotation;
			Camera.main.transform.Rotate(offsetRotation);

			UpdateCamera(head);
        }

		public static void UpdateCamera(Transform head)
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

/*		public static void UpdateEyeCamera()
		{
			Camera.main.fieldOfView =
				AI_PovX.ZoomKey.Value.IsPressed() ?
					AI_PovX.ZoomFov.Value :
					AI_PovX.Fov.Value;

			Vector3 eyePosition;

			if (AI_PovX.CameraPoVLocation.Value == AI_PovX.CameraLocation.LeftEye)
				eyePosition = povPupilLeft.position;
			else if (AI_PovX.CameraPoVLocation.Value == AI_PovX.CameraLocation.RightEye)
				eyePosition = povPupilRight.position;
			else
				eyePosition = Vector3.Lerp(povPupilLeft.position, povPupilRight.position, 0.5f);

			Camera.main.transform.rotation = povPupilLeft.rotation;

			Camera.main.transform.position =
				eyePosition +
				(AI_PovX.OffsetX.Value) * povPupilLeft.right +
				(AI_PovX.OffsetY.Value) * povPupilLeft.up +
				(AI_PovX.OffsetZ.Value) * povPupilLeft.forward;
			Camera.main.nearClipPlane = AI_PovX.NearClip.Value;
		}
*/
		public static void UpdatePoVNeck()
		{
			if (Tools.IsHScene())
				UpdatePoVHScene();
			else if (Tools.IsFishingScene())
				UpdatePoVFishing();
			else if (Tools.IsFreeRoam())
				UpdatePoVFreeRoam();
			else
				UpdatePoVScene();
		}

		// Used for fishing scene.
		public static void UpdatePoVFishing()
		{
			// Refresh when switching PoV modes.
			RefreshInScene(true);

			Console.WriteLine("UpdatePoVFishing");

			AIProject.MiniGames.Fishing.Lure lure = Map.Instance.Player.GetComponentInChildren<AIProject.MiniGames.Fishing.FishingManager>().lure;

			Quaternion currentRotation = povHead.localRotation;


			if (lure == null || (lure.state != AIProject.MiniGames.Fishing.Lure.State.Float && lure.state != AIProject.MiniGames.Fishing.Lure.State.Hit))
				povHead.localRotation = Quaternion.identity;
			else
				povHead.LookAt(lure.transform.position);

			povHead.localRotation = Quaternion.RotateTowards(currentRotation, povHead.localRotation, 120f * Time.deltaTime);

			Camera.main.transform.rotation = povHead.rotation;
			UpdateCamera(povHead);

			//		UpdateEyeCamera();
		}

		// Used for scenes where the focused character cannot be controlled.
		public static void UpdatePoVScene()
		{
			// Refresh when switching PoV modes.
			RefreshInScene(true);

			UpdatePlayerBodyRotation();
			UpdateNeckRotations();
		//	UpdateEyeRotations();

			UpdateCamera(povHead, new Vector3(eyeLocalPitch, eyeLocalYaw, 0f));
		//	UpdateEyeCamera();
		}

		public static void UpdatePoVHScene()
		{
			// Refresh when switching PoV modes.
			RefreshInScene(true);

			if (!lockMaleHeadPosition)
				UpdateNeckRotations();

		//	UpdateEyeRotations();
			UpdateCamera(povHead, new Vector3(eyeLocalPitch, eyeLocalYaw, 0f));
		//	UpdateEyeCamera();
		}

		// PoV exclusively for the player.
		public static void UpdatePoVFreeRoam()
		{
			// Refresh when switching PoV modes.
			RefreshInScene(false);

			PlayerActor player = Map.Instance.Player;
			if (player == null)
				return;

			if (!AI_PovX.RotateHead.Value || player.StateInfo.move.magnitude > 0f)
			{
				// Move entire body when moving.
				bodyWorldYaw = cameraWorldYaw;
				eyeLocalYaw = headLocalYaw = cameraLocalYaw = 0;
				player.ChaControl.objAnim.transform.localRotation = Quaternion.Euler(0f, bodyWorldYaw, 0f);
			}
			else
			{
				// Rotate head first. If head rotation is at the limit, rotate body.
				float angle = Tools.GetClosestAngle(bodyWorldYaw, cameraWorldYaw, out bool clockwise);

				angle -= (AI_PovX.HeadMaxYaw.Value + AI_PovX.EyeMaxYaw.Value);
				if (angle > 0)
				{
					if (clockwise)
						bodyWorldYaw = Tools.Mod2(bodyWorldYaw + angle, 360f);
					else
						bodyWorldYaw = Tools.Mod2(bodyWorldYaw - angle, 360f);
				}
			}

			player.Rotation = Quaternion.Euler(0f, bodyWorldYaw, 0f);
			UpdateNeckRotations();
		//	UpdateEyeRotations();

			if (AI_PovX.HeadBob.Value)
			{
				UpdateCamera(povHead, new Vector3(eyeLocalPitch, eyeLocalYaw, 0f));
		//		UpdateEyeCamera();
			}
			else
            {
				Camera.main.transform.rotation = Quaternion.Euler(cameraLocalPitch, cameraWorldYaw, 0f);
				UpdateCamera(povHead);
			}
		}

		public static void CheckHSceneHeadLock(string hMotion = null)
		{
			lockMaleHeadPosition = false;

			if (!inHScene || hScene == null)
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

		private static void RefreshInScene(bool isInScene)
		{
			if (inScene == isInScene)
				return;

			inScene = isInScene;
			SetPoVCharacter(GetCharacterFromFocus(Focus.Player));
		}

		public static void AdjustPoVHeadScale()
        {
			if (povCharacter == null)
				return;

			if (Tools.ShouldHideHead())
				povCharacter.objHeadBone.transform.localScale = new Vector3(povCharacter.objHeadBone.transform.localScale.x, povCharacter.objHeadBone.transform.localScale.y, AI_PovX.HideHeadScaleZ.Value);
			else
				povCharacter.objHeadBone.transform.localScale = normalHeadScale;
		}

		public static void CalculateEyesOffset()
        {
			if (povCharacter == null)
				return;

			eyeOffset = Tools.GetEyesOffset(povCharacter);
		}

		private static void UpdateAgentHeadRotation()
		{
			if (agentHeadRotationReached || agentHead == null || Tools.IsHScene())
				return;

			agentHead.localRotation = Quaternion.RotateTowards(agentHead.localRotation, Quaternion.Euler(agentHeadTargetRotation), 30f * Time.deltaTime);
			if (Tools.VectorsEqual(agentHead.localEulerAngles, agentHeadTargetRotation))
				agentHeadRotationReached = true;
		}

		private static void ResetAgentHeadRotation()
		{
			if (agentHead == null)
				return;

			agentHeadTargetRotation = Vector3.zero;
			agentHead.localRotation = Quaternion.identity;
			agentHeadRotationReached = true;
			agentHead = null;
		}

		private static void UpdateNeckRotations()
        {
			if (povUpperNeck == null || povLowerNeck == null)
				return;

			povLowerNeck.localRotation = Quaternion.Euler(headLocalPitch / 2, headLocalYaw / 2, 0);
			povUpperNeck.localRotation = Quaternion.Euler(headLocalPitch / 2, headLocalYaw / 2, 0);
		}
/*
		private static void UpdateEyeRotations()
		{
			if (povEyeLeft == null || povEyeRight == null)
				return;

			povEyeLeft.localRotation = Quaternion.Euler(eyeLocalPitch, eyeLocalYaw, 0);
			povEyeRight.localRotation = Quaternion.Euler(eyeLocalPitch, eyeLocalYaw, 0);
		}
*/
		private static void UpdatePlayerBodyRotation()
		{
			if (playerBodyRotationReached)
				return;

			var player = Map.Instance.Player;
			if (player == null)
				return;

			Vector3 targetRotation = new Vector3(0, bodyWorldYaw, 0);
			player.Rotation = Quaternion.RotateTowards(player.Rotation, Quaternion.Euler(targetRotation), 120f * Time.deltaTime);
			if (Tools.VectorsEqual(player.Rotation.eulerAngles, targetRotation))
				playerBodyRotationReached = true;
		}

		private static void UpdateMouseLook()
        {
			if (Cursor.lockState == CursorLockMode.None && !AI_PovX.CameraDragKey.Value.IsPressed())
				return;

			float sensitivity = AI_PovX.Sensitivity.Value;

            if (AI_PovX.ZoomKey.Value.IsPressed())
                sensitivity *= AI_PovX.ZoomFov.Value / AI_PovX.Fov.Value;

            float mouseY = UnityEngine.Input.GetAxis("Mouse Y") * sensitivity;
            float mouseX = UnityEngine.Input.GetAxis("Mouse X") * sensitivity;

			cameraWorldYaw = Tools.Mod2(cameraWorldYaw + mouseX, 360f);

			if (inHScene && lockMaleHeadPosition)
            {
                eyeLocalPitch = cameraLocalPitch = Mathf.Clamp(cameraLocalPitch - mouseY, -(AI_PovX.EyeMaxPitch.Value), (AI_PovX.EyeMaxPitch.Value));
                eyeLocalYaw = cameraLocalYaw = Mathf.Clamp(cameraLocalYaw + mouseX, -(AI_PovX.EyeMaxYaw.Value), (AI_PovX.EyeMaxYaw.Value));
                headLocalPitch = 0;
                headLocalYaw = 0;
            }
            else
            {
                cameraLocalPitch = Mathf.Clamp(cameraLocalPitch - mouseY, -(AI_PovX.EyeMaxPitch.Value + AI_PovX.HeadMaxPitch.Value), (AI_PovX.EyeMaxPitch.Value + AI_PovX.HeadMaxPitch.Value));
                cameraLocalYaw = Mathf.Clamp(cameraLocalYaw + mouseX, -(AI_PovX.EyeMaxYaw.Value + AI_PovX.HeadMaxYaw.Value), (AI_PovX.EyeMaxYaw.Value + AI_PovX.HeadMaxYaw.Value));
				headLocalPitch = cameraLocalPitch * AI_PovX.HeadMaxPitch.Value / (AI_PovX.EyeMaxPitch.Value + AI_PovX.HeadMaxPitch.Value);
				headLocalYaw = cameraLocalYaw * AI_PovX.HeadMaxYaw.Value / (AI_PovX.EyeMaxYaw.Value + AI_PovX.HeadMaxYaw.Value);
				eyeLocalPitch = cameraLocalPitch - headLocalPitch;
				eyeLocalYaw = cameraLocalYaw - headLocalYaw;
			}       
        }
    }
}
