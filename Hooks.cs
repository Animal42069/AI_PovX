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
				!Controller.toggled ||
				Controller.chaCtrl == null)
				return true;

			if (Controller.focus == 0 && !Tools.IsHScene())
				Controller.FreeRoamPoV();
			else
				Controller.ScenePoV();

			return __instance != Controller.chaCtrl.neckLookCtrl;
		}

		[HarmonyPrefix, HarmonyPatch(typeof(Manager.ADV), "ChangeADVCamera")]
		public static void Manager_ChangeADVCamera(Actor actor)
        {
			Console.WriteLine($"Manager_ChangeADVCamera {actor.ChaControl.name}");
			Controller.RotatePlayerTowardsCharacter(actor.ChaControl);
		}

		[HarmonyPrefix, HarmonyPatch(typeof(Manager.ADV), "ChangeADVCameraDiagonal")]
		public static void Manager_ChangeADVCameraDiagonal(Actor actor)
		{
			Console.WriteLine($"Manager_ChangeADVCameraDiagonal {actor.ChaControl.name}");
			Controller.RotatePlayerTowardsCharacter(actor.ChaControl);
		}

		[HarmonyPrefix, HarmonyPatch(typeof(Manager.ADV), "ChangeADVFixedAngleCamera")]
		public static void Manager_ChangeADVFixedAngleCamera(Actor actor, int attitudeID)
		{
			Console.WriteLine($"Manager_ChangeADVFixedAngleCamera {actor.ChaControl.name} attitudeID {attitudeID}");
			Controller.RotatePlayerTowardsCharacter(actor.ChaControl);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(AgentActor), "VanishCommands")]
		public static void AgentActor_VanishCommands(AgentActor __instance)
		{
			Console.WriteLine($"AgentActor_VanishCommands {__instance.ChaControl.name}");
			Controller.RotateCharacterHead(__instance.ChaControl, 0);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(AgentActor), "EndTutorialADV")]
		public static void AgentActor_EndTutorialADV(AgentActor __instance)
		{
			Console.WriteLine($"AgentActor_EndTutorialADV {__instance.ChaControl.name}");
			Controller.RotateCharacterHead(__instance.ChaControl, 0);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(MerchantActor), "VanishCommands")]
		public static void MerchantActor_VanishCommands(AgentActor __instance)
		{
			Console.WriteLine($"MerchantActor_VanishCommands {__instance.ChaControl.name}");
			Controller.RotateCharacterHead(__instance.ChaControl, 0);
		}

		/*	[HarmonyPrefix, HarmonyPatch(typeof(AgentActor), "StartCommunication")]
			public static void AgentActor_StartCommunication(AgentActor __instance)
			{
				Console.WriteLine($"AgentActor_StartCommunication {__instance.ChaControl.name} AttitudeID {__instance.AttitudeID} UseNeckLook {__instance.UseNeckLook}");
				Controller.RotatePlayerTowardsCharacter(__instance.ChaControl);
			}

			[HarmonyPostfix, HarmonyPatch(typeof(AgentActor), "EndCommunication")]
			public static void AgentActor_EndCommunication(AgentActor __instance)
			{
				Console.WriteLine($"AgentActor_EndCommunication {__instance.ChaControl.name}");
				Controller.RotateCharacterHead(__instance.ChaControl, 0);
			}

			[HarmonyPrefix, HarmonyPatch(typeof(MerchantActor), "StartCommunication")]
			public static void MerchantActor_StartCommunication(MerchantActor __instance)
			{
				Console.WriteLine($"MerchantActor_StartCommunication {__instance.ChaControl.name}");
				Controller.RotatePlayerTowardsCharacter(__instance.ChaControl);
			}

			[HarmonyPostfix, HarmonyPatch(typeof(MerchantActor), "EndCommunication")]
			public static void MerchantActor_EndCommunication(MerchantActor __instance)
			{
				Console.WriteLine($"MerchantActor_EndCommunication {__instance.ChaControl.name}");
				Controller.RotateCharacterHead(__instance.ChaControl, 0);
			}
		*/
		/*[HarmonyPostfix, HarmonyPatch(typeof(NeckLookControllerVer2), "LateUpdate")]
		public static void Postfix_NeckLookControllerVer2_LateUpdate(NeckLookControllerVer2 __instance)
		{
			if (!Controller.shouldStare)
				return;

			if (!Tools.IsHScene())
				return;

			Actor[] females = HSceneManager.Instance.females;
			ChaControl playerChaCtrl = Map.Instance.Player.ChaControl;

			if (__instance == playerChaCtrl.neckLookCtrl)
				Controller.Stare(females[0].ChaControl, playerChaCtrl);
			else
				foreach (Actor female in females)
					if (female != null)
					{
						ChaControl chaCtrl = female.ChaControl;

						if (__instance == chaCtrl.neckLookCtrl)
						{
							Controller.Stare(playerChaCtrl, chaCtrl);

							break;
						}
					}
		}*/
	}
}
