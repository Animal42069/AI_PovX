using AIChara;
using AIProject;
using HarmonyLib;
using Manager;
using System;
using UnityEngine;

namespace AI_PovX
{
	public partial class AI_PovX
	{
		[HarmonyPrefix, HarmonyPatch(typeof(NeckLookControllerVer2), "LateUpdate")]
		public static bool Prefix_NeckLookControllerVer2_LateUpdate(NeckLookControllerVer2 __instance)
		{
			if (Manager.Housing.Instance.IsCraft ||
				!Controller.povEnabled ||
				Controller.povCharacter == null ||
				__instance != Controller.povCharacter.neckLookCtrl)
				return true;

				Controller.UpdatePoVNeck();
			return false;
		}

		[HarmonyPostfix, HarmonyPatch(typeof(Manager.ADV), "ChangeADVCamera")]
		public static void Manager_ChangeADVCamera(Actor actor)
        {
			if (Map.Instance.Player.CameraControl.Mode != CameraMode.ADVExceptStand)
				Controller.RotatePlayerTowardsCharacter(actor.ChaControl);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(Manager.ADV), "ChangeADVCameraDiagonal")]
		public static void Manager_ChangeADVCameraDiagonal(Actor actor)
		{
			if (Map.Instance.Player.CameraControl.Mode != CameraMode.ADVExceptStand)
				Controller.RotatePlayerTowardsCharacter(actor.ChaControl);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(Manager.ADV), "ChangeADVFixedAngleCamera")]
		public static void Manager_ChangeADVFixedAngleCamera(Actor actor, int attitudeID)
		{
			if (Map.Instance.Player.CameraControl.Mode != CameraMode.ADVExceptStand)
				Controller.RotatePlayerTowardsCharacter(actor.ChaControl);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(AgentActor), "VanishCommands")]
		public static void AgentActor_VanishCommands(AgentActor __instance)
		{
			Controller.RotateCharacterHead(__instance.ChaControl, 0);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(AgentActor), "EndTutorialADV")]
		public static void AgentActor_EndTutorialADV(AgentActor __instance)
		{
			Controller.RotateCharacterHead(__instance.ChaControl, 0);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(MerchantActor), "VanishCommands")]
		public static void MerchantActor_VanishCommands(AgentActor __instance)
		{
			Controller.RotateCharacterHead(__instance.ChaControl, 0);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartVoice")]
		public static void HScene_Post_SetStartVoice(HScene __instance)
        {
			Controller.hScene = __instance;
		}

		[HarmonyPostfix, HarmonyPatch(typeof(HScene), "ChangeAnimation")]
		public static void HScene_Post_ChangeAnimation()
		{
			Controller.CheckHSceneHeadLock();
		}

		[HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetMovePositionPoint")]
		public static void HScene_Post_SetMovePositionPoint()
        {
			Controller.CheckHSceneHeadLock();
		}

		[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "setPlay")]
		public static void ChaControl_Post_SetPlay(ChaControl __instance, string _strAnmName)
		{
			if (__instance == null || _strAnmName.IsNullOrEmpty())
				return;

			Controller.CheckHSceneHeadLock(_strAnmName);
		}
	}
}
