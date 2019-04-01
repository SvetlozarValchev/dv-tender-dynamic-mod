using System.Collections.Generic;
using System.Reflection;
using UnityModManagerNet;
using Harmony12;
using UnityEngine;
using TMPro;

namespace TenderDynamicWeight
{
    public class Main
    {
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            return true;
        }
    }

    [HarmonyPatch(typeof(TrainCar), "Awake")]
    class TrainCar_Awake_Patch
    {
        static void Prefix(TrainCar __instance)
        {
            if (__instance.carType == TrainCarType.Tender)
            {
                __instance.gameObject.AddComponent<TenderDynamicWeight>();

                __instance.totalMass = 5000f;
            }
        }
    }

    [HarmonyPatch(typeof(TrainCarPlatesController), "CreateTrainCarPlates")]
    class TrainCarPlatesController_CreateTrainCarPlates_Patch
    {

        static void Postfix(TrainCarPlatesController __instance, DV.Logic.Job.Car logicCar, bool isLoco, float length, float mass)
        {
            if (logicCar.carType == TrainCarType.Tender)
            {
                Transform carPlateAnchor1 = __instance.transform?.Find("[car plate anchor1]");
                Transform carPlateAnchor2 = __instance.transform?.Find("[car plate anchor2]");

                Transform carPlateAnchor1Plate = carPlateAnchor1.transform?.Find("TrainCarPlate(Clone)");
                Transform carPlateAnchor2Plate = carPlateAnchor2.transform?.Find("TrainCarPlate(Clone)");

                TrainCarPlate plate1 = carPlateAnchor1Plate.GetComponent<TrainCarPlate>();
                TrainCarPlate plate2 = carPlateAnchor2Plate.GetComponent<TrainCarPlate>();

                var dynamicWeight = __instance.GetComponent<TenderDynamicWeight>();

                var cargoMass1 = GetPlateCargoMass(plate1);
                var cargoMass2 = GetPlateCargoMass(plate2);

                var rectTransform1 = cargoMass1.GetComponent<RectTransform>();
                var rectTransform2 = cargoMass2.GetComponent<RectTransform>();

                rectTransform1.offsetMax = new Vector2(0.65f, rectTransform1.offsetMax.y);
                rectTransform2.offsetMax = new Vector2(0.65f, rectTransform2.offsetMax.y);

                dynamicWeight.AddTextMeshMass(cargoMass1);
                dynamicWeight.AddTextMeshMass(cargoMass2);
                dynamicWeight.AddTextMeshCargo(GetPlateCurrentCargo(plate1));
                dynamicWeight.AddTextMeshCargo(GetPlateCurrentCargo(plate2));
            }
        }

        static TextMeshPro GetPlateCargoMass(TrainCarPlate plate)
        {
            return AccessTools.FieldRefAccess<TrainCarPlate, TextMeshPro>(plate, "cargoMass");
        }

        static TextMeshPro GetPlateCurrentCargo(TrainCarPlate plate)
        {
            return AccessTools.FieldRefAccess<TrainCarPlate, TextMeshPro>(plate, "currentCargo");
        }
    }

    class TenderDynamicWeight : MonoBehaviour
    {
        TrainCar trainCar;
        TenderSimulation sim;
        Bogie[] bogies;

        List<TextMeshPro> cargoMass = new List<TextMeshPro>();
        List<TextMeshPro> currentCargo = new List<TextMeshPro>();

        float baseMass = 5000f;

        void Awake()
        {
            trainCar = GetComponent<TrainCar>();
            sim = GetComponent<TenderSimulation>();
            bogies = trainCar.Bogies;

            StartCoroutine(UpdatePlates());
        }

        void FixedUpdate()
        {
            var cargoMass = sim.tenderWater.value + sim.tenderCoal.value;

            trainCar.rb.mass = (baseMass + cargoMass) * (1.0f - trainCar.bogieMassRatio);

            for (int index = 0; index < bogies.Length; ++index)
            {
                var rb = bogies[index].rb;

                if (rb != null) rb.mass = ((baseMass + cargoMass) * (trainCar.bogieMassRatio / bogies.Length));
            }
        }

        IEnumerator<object> UpdatePlates()
        {
            yield return WaitFor.SecondsRealtime(3f);

            var tenderWater = Mathf.RoundToInt(sim.tenderWater.value);
            var tenderCoal = Mathf.RoundToInt(sim.tenderCoal.value);

            for (var i = 0; i < cargoMass.Count; i++)
            {
                currentCargo[i].text = "Water/Coal";
                cargoMass[i].text = tenderWater + "L/" + tenderCoal + "kg";
            }

            StartCoroutine(UpdatePlates());
        }

        public void AddTextMeshMass(TextMeshPro cargoMass)
        {
            this.cargoMass.Add(cargoMass);
        }

        public void AddTextMeshCargo(TextMeshPro currentCargo)
        {
            this.currentCargo.Add(currentCargo);
        }
    }
}