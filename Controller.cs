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
		public static int povFocus = 0;
		public static int targetFocus = 0;
		public static ChaControl[] characters = new ChaControl[0];
		public static ChaControl povCharacter;
		public static ChaControl targetCharacter;
		public static Vector3 eyeOffset = Vector3.zero;
		public static Vector3 normalHeadScale;
		public static float backupFoV;

		public static bool inScene;
		public static bool inHScene;
		public static bool lockHeadPosition;
		public static bool povSetThisFrame = false;

		public static HScene hScene;
		public static string currentHMotion;

		private static readonly List<string> maleLockHeadAllHPositions = new List<string>() { "aia_f_10", "h2h_f_03", "ait_f_00", "ait_f_07" };
		private static readonly List<string> maleLockHeadHPositions = new List<string>() { "aia_f_00", "aia_f_01", "aia_f_04", "aia_f_06", "aia_f_07", "aia_f_08", "aia_f_11", "aia_f_12", "aia_f_13", "aia_f_18", "aia_f_19", "aia_f_23", "aia_f_24", "aia_f_26", "ai3p", "h2a_f_00" };

		private static readonly List<string> firstFemaleLockHeadAllHPositions = new List<string>() { "ais_f_19", "aia_f_16", "ais_f_27" };
		private static readonly List<string> firstFemaleLockHeadHPositions = new List<string>() { "aia_f_00", "aia_f_01", "aia_f_07", "aia_f_11", "aia_f_12", "aih_f_00", "aih_f_04", "aih_f_05", "aih_f_09", "aih_f_10", "aih_f_12", "aih_f_13", "aih_f_14", "aih_f_16", "aih_f_17", "aih_f_19", "aih_f_21", "aih_f_23", "aih_f_25", "aih_f_26", "aih_f_27", "h2h_f_02", "h2h_f_03", "aih_f_06", "aih_f_07", "ail_f1_03", "ail_f1_04", "h2_mf2_f1_00", "h2_mf2_f2_03", "h2_m2f_f_01", "h2_m2f_f_04", "h2_m2f_f_05", "h2_m2f_f_06", "ait_f_07" };
		private static readonly List<string> secondFemaleLockHeadAllHPositions = new List<string>() { };
		private static readonly List<string> secondFemaleLockHeadHPositions = new List<string>() { "ail_f2_03", "ail_f2_04", "h2_mf2_f1_00", "h2_mf2_f2_03" };

		private static readonly List<List<string>> LockHeadAllHPositions = new List<List<string>>() { maleLockHeadAllHPositions, firstFemaleLockHeadAllHPositions, secondFemaleLockHeadAllHPositions };
		private static readonly List<List<string>> LockHeadHPositions = new List<List<string>>() { maleLockHeadHPositions, firstFemaleLockHeadHPositions, secondFemaleLockHeadHPositions };

		private static readonly List<string> lockHeadHMotionExceptions = new List<string>() { "Idle", "_A" };

		internal static readonly string lowerNeckBone = "cf_J_Neck";
		internal static readonly string upperNeckBone = "cf_J_Head";
		internal static readonly string headBone = "cf_J_Head_s";
		internal static readonly string lockBone = "N_Hitai";
		internal static readonly string leftEyeBone = "cf_J_eye_rs_L";
		internal static readonly string rightEyeBone = "cf_J_eye_rs_R";
		internal static readonly string leftEyePupil = "cf_J_pupil_s_L";
		internal static readonly string rightEyePupil = "cf_J_pupil_s_R";
	
		public static Transform agentHead;
		public static Vector3 agentHeadTargetRotation = Vector3.zero;
		public static bool agentHeadRotationReached = true;
		public static bool playerBodyRotationReached = true;

		internal static Transform povUpperNeck;
		internal static Transform povLowerNeck;
		internal static Transform povHead;
		internal static Transform lockTarget;
		
		internal const int Player = 0;

		public static void Update()
		{
			povSetThisFrame = false;

			if (AI_PovX.PovKey.Value.IsDown())
				EnablePoV(!povEnabled);

			if (!povEnabled)
				return;

			if (inHScene && AI_PovX.CharaCycleKey.Value.IsDown())
			{
				targetFocus = povFocus = GetValidFocus(povFocus + 1);
				SetPoVCharacter(GetValidCharacterFromFocus(ref povFocus));
				SetTargetCharacter(GetValidCharacterFromFocus(ref targetFocus));
			}
			else if (!inHScene && povFocus != Player)
			{
				targetFocus = povFocus = GetValidFocus(Player);
				SetPoVCharacter(GetValidCharacterFromFocus(ref povFocus));
				SetTargetCharacter(GetValidCharacterFromFocus(ref targetFocus));
			}	

			if (AI_PovX.HeadLockKey.Value.IsDown())
				LockPoVHead(!lockHeadPosition);

			if (AI_PovX.LockOnKey.Value.IsDown())
			{
				targetFocus = GetValidFocus(targetFocus + 1);
				SetTargetCharacter(GetValidCharacterFromFocus(ref targetFocus));
			}

			if (AI_PovX.CursorToggleKey.Value.IsDown())
			{
				Cursor.visible = !Cursor.visible;
				Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
			}
			else if (!AI_PovX.ZoomKey.Value.IsPressed() && !Cursor.visible && UnityEngine.Input.anyKeyDown)
			{
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
			}

			if (povFocus == targetFocus)
				UpdateMouseLook();
				
			UpdateAgentHeadRotation();		
		}

		public static void LateUpdate()
		{
			if (povEnabled)
			{
				if (inHScene != Tools.IsHScene())
				{
					inHScene = Tools.IsHScene();
					characters = GetSceneCharacters();
					targetFocus = povFocus = GetValidFocus(Player);
					SetPoVCharacter(GetValidCharacterFromFocus(ref povFocus));
					SetTargetCharacter(GetValidCharacterFromFocus(ref targetFocus));
				}

				// Make it so that the player doesn't go visible if they're not supposed to be in the scene.
				if (!inHScene || Map.Instance.Player.ChaControl.visibleAll)
					Map.Instance.Player.ChaControl.visibleAll = true;

				if (inHScene && agentHead != null)
					ResetAgentHeadRotation();
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

		public static void ResetPoVRotations()
        {
			ResetPoVPitch();
			ResetPoVYaw();
		}

		public static void ResetPoVPitch()
		{
			cameraLocalPitch = headLocalPitch = eyeLocalPitch = 0f;
		}

		public static void ResetPoVYaw()
		{
			cameraLocalYaw = headLocalYaw = eyeLocalYaw = 0f;
			cameraWorldYaw = povHead.eulerAngles.y;
			bodyWorldYaw = Map.Instance.Player.Rotation.eulerAngles.y;
		}

		public static void EnablePoV(bool enable)
		{
			if (povEnabled == enable)
				return;

			povEnabled = enable;
			if (enable)
			{
				characters = GetSceneCharacters();

				if (!FocusCharacterValid(povFocus))
					targetFocus = povFocus = GetValidFocus(povFocus + 1);

				if (!FocusCharacterValid(targetFocus))
					targetFocus = GetValidFocus(targetFocus + 1);

				SetPoVCharacter(GetValidCharacterFromFocus(ref povFocus));
				SetTargetCharacter(GetValidCharacterFromFocus(ref targetFocus));
				ResetPoVRotations();
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
				SetTargetCharacter(null);
			}
		}

		public static ChaControl[] GetSceneCharacters()
		{
			if (!Tools.IsMainGame())
				return UnityEngine.Object.FindObjectsOfType<ChaControl>();

			if (!Tools.IsHScene())
				return new ChaControl[1] { Map.Instance.Player.ChaControl };

			ChaControl[] characters = new ChaControl[3];
			characters[0] = Map.Instance.Player.ChaControl;
			characters[1] = HSceneManager.Instance.females[0]?.ChaControl;
			characters[2] = HSceneManager.Instance.females[1]?.ChaControl;

			return characters;
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
			normalHeadScale = povCharacter.objHeadBone.transform.localScale;

			CalculateEyesOffset();
			AdjustPoVHeadScale();
			CheckHSceneHeadLock();
		}

		public static void SetTargetCharacter(ChaControl character)
		{
			if (targetCharacter == character)
				return;
		
			targetCharacter = character;
			if (targetCharacter == null)
				return;

			lockTarget = targetCharacter.GetComponentsInChildren<Transform>().Where(x => x.name.Equals(lockBone)).FirstOrDefault();
		}

		public static int GetValidFocus(int focus)
		{
			if (focus >= characters.Length)
				focus %= characters.Length;

			for (int i = 0; i < characters.Length; i++)
			{
				if (FocusCharacterValid(focus))
					return focus;

				// Skip invisible or destroyed characters.
				focus = (focus + 1) % characters.Length;
			}

			return focus;
		}

		public static bool FocusCharacterValid(int focus)
		{
			if (focus >= characters.Length)
				return false;

			var focusCharacter = characters[focus];
			if (focusCharacter != null && focusCharacter.visibleAll)
				return true;

			return false;
		}

		public static ChaControl GetValidCharacterFromFocus(ref int focus)
		{
			if (characters.Length == 0)
				return null;

			focus = GetValidFocus(focus);
			return characters[focus];
		}

		public static void RotatePlayerTowardsCharacter(ChaControl character)
        {
			if (!povEnabled || povFocus != Player || povCharacter == null)
				return;

			PlayerActor player = Map.Instance.Player;
			if (player == null)
				return;

			if (!player.isActiveAndEnabled)
				player.enabled = true;

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

		public static void UpdateTargetLockedCamera(Transform head)
		{
			UpdateCamera(head);
			Camera.main.transform.LookAt(lockTarget.position, Vector3.up);
		}

		public static void UpdateCamera(Transform head, Vector3 offsetRotation)
		{
			UpdateCamera(head);

			if (AI_PovX.CameraNormalize.Value)
				Camera.main.transform.rotation = Quaternion.Euler(head.eulerAngles.x, head.eulerAngles.y, 0);
			else
				Camera.main.transform.rotation = head.rotation;

			Camera.main.transform.Rotate(offsetRotation);		
		}

		public static void UpdateCamera(Transform head)
		{
			Camera.main.fieldOfView =
				AI_PovX.ZoomKey.Value.IsPressed() ?
					AI_PovX.ZoomFov.Value :
					AI_PovX.Fov.Value;

			Camera.main.transform.position =
				head.position +
				(AI_PovX.EyeOffset.Value.x + eyeOffset.x) * head.right +
				(AI_PovX.EyeOffset.Value.y + eyeOffset.y) * head.up +
				(AI_PovX.EyeOffset.Value.z + eyeOffset.z) * head.forward;

			Camera.main.nearClipPlane = AI_PovX.NearClip.Value;
		}

		public static void UpdatePoVCamera()
		{
			if (Tools.IsHScene())
				UpdatePoVHScene();
			else if (Tools.IsFishingScene())
				UpdatePoVFishing();
			else if (Tools.IsFreeRoam())
				UpdatePoVFreeRoam();
			else
				UpdatePoVScene();
				
			povSetThisFrame = true;
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
		}

		// Used for scenes where the focused character cannot be controlled.
		public static void UpdatePoVScene()
		{
			// Refresh when switching PoV modes.
			RefreshInScene(true);

			UpdatePlayerBodyRotation();
			UpdateNeckRotations();

			UpdateCamera(povHead, new Vector3(eyeLocalPitch, eyeLocalYaw, 0f));
		}

		public static void UpdatePoVHScene()
		{
			// Refresh when switching PoV modes.
			RefreshInScene(true);

			if (povFocus != targetFocus)
			{
				UpdateTargetLockedCamera(povHead);
				return;
			}

			if (!lockHeadPosition)
				UpdateNeckRotations();

			UpdateCamera(povHead, new Vector3(eyeLocalPitch, eyeLocalYaw, 0f));
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

			if (AI_PovX.HeadBob.Value)
			{
				UpdateCamera(povHead, new Vector3(eyeLocalPitch, eyeLocalYaw, 0f));
			}
			else
            {
				Camera.main.transform.rotation = Quaternion.Euler(cameraLocalPitch, cameraWorldYaw, 0f);
				UpdateCamera(povHead);
			}
		}

		public static void CheckHSceneHeadLock(string hMotion = null)
		{
			if (!AI_PovX.HSceneAutoHeadLock.Value || !inHScene || hScene == null || povFocus >= LockHeadHPositions.Count)
				return;

			string currentHAnimation = hScene.ctrlFlag.nowAnimationInfo.fileFemale;

			if (hMotion != null)
				currentHMotion = hMotion;

			if (currentHAnimation == null || currentHMotion == null)
				return;

			if (LockHeadAllHPositions[povFocus].Contains(currentHAnimation) ||
				(LockHeadHPositions[povFocus].Contains(currentHAnimation) && !lockHeadHMotionExceptions.Contains(currentHMotion)))
				LockPoVHead(true);
			else
				LockPoVHead(false);
		}

		public static void LockPoVHead(bool locked)
        {
			lockHeadPosition = locked;

			if (locked)
				ResetPoVRotations();		
		}

		private static void RefreshInScene(bool isInScene)
		{
			if (inScene == isInScene)
				return;

			inScene = isInScene;

			characters = GetSceneCharacters();
			targetFocus = povFocus = GetValidFocus(Player);
			SetPoVCharacter(GetValidCharacterFromFocus(ref povFocus));
			SetTargetCharacter(GetValidCharacterFromFocus(ref targetFocus));

			if (!inScene)
				ResetPoVYaw();
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

			if (inHScene && lockHeadPosition)
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
