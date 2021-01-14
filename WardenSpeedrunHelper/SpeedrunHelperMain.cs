using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;
using System.IO;
using System.Collections.Generic;


namespace WardenSpeedrunHelper {

	public static class SpeedrunHelperMain {

		public static bool enabled;
		public static UnityModManager.ModEntry mod;

		static SpeedrunHelperUI speedUI = null;
		static Player currentPlayer = null;

		static List<GameObject> allColliderDisplays = new List<GameObject>();

		public static bool Load(UnityModManager.ModEntry modEntry) {

			var harmony = new Harmony("com.aroymart.wardenmod");
			harmony.PatchAll();
			mod = modEntry;
			modEntry.OnToggle = OnToggle;

			return true;
		}

		static bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {

			if (value) {
				if (speedUI == null) {
					var b = new GameObject("SpeedrunHelperMenu");
					speedUI = b.AddComponent<SpeedrunHelperUI>();
					GameObject.DontDestroyOnLoad(b);
				}
			}
			enabled = value;
			return true;
		}

		public static void DestroyHitboxDisplays() {
			foreach (var h in allColliderDisplays) {
				GameObject.Destroy(h);
			}
		}
		public static void DisplayHitboxes(bool triggerOnly, bool[] colliderTypes) { // capsule, sphere, cube, mesh
			mod.Logger.Log(colliderTypes[0] + ", " + colliderTypes[1] + ", " + colliderTypes[2] + ", " + colliderTypes[3]);
			var objects = GameObject.FindObjectsOfType<Collider>();
			mod.Logger.Log(objects.Length.ToString());
			foreach (var o in objects) {
				if (o.isTrigger || !triggerOnly)
					DisplayHitbox(o, colliderTypes);
			}
		}
		static void DisplayHitbox(Collider o, bool[] colliderTypes) { // capsule, sphere, cube, mesh
			GameObject viz = null;
			if (o is MeshCollider m) {
				if (colliderTypes[3]) {
					viz = new GameObject("ColliderVisualizer");
					viz.transform.position = m.transform.position;
					viz.transform.rotation = m.transform.rotation;
					if (m.transform.lossyScale != m.transform.localScale) {
						mod.Logger.Log("Warning: This collider's lossyScale does not match its localScale and may not look right");
					}
					MeshFilter mesh = viz.AddComponent<MeshFilter>();
					viz.transform.localScale = o.transform.lossyScale; // this is weird but hopefully won't be a problem. Some constraint of 3d space blah blah blah
					if (m.convex) {
						mod.Logger.Log("Warning: This collider is convex and will probably not look right");
					}
					viz.AddComponent<MeshRenderer>();
					mesh.mesh = m.sharedMesh;
				}
			} else if (o is BoxCollider b) {
				if (colliderTypes[2]) {
					viz = GameObject.CreatePrimitive(PrimitiveType.Cube);
					viz.transform.position = b.bounds.center;
					viz.transform.localScale = b.bounds.size;
					viz.transform.rotation = b.transform.rotation;
					viz.name = "ColliderVisualizer";
					GameObject.DestroyImmediate(viz.GetComponent<BoxCollider>());
				}
			} else if (o is SphereCollider s) {
				if (colliderTypes[1]) {
					viz = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					viz.transform.position = s.bounds.center;
					viz.transform.localScale = s.bounds.size;
					viz.transform.rotation = s.transform.rotation;
					viz.name = "ColliderVisualizer";
					GameObject.DestroyImmediate(viz.GetComponent<SphereCollider>());
				}
			} else if (o is CapsuleCollider c) {
				if (colliderTypes[0]) {
					mod.Logger.Log("Warning: This collider is a capsule and will most likely not look perfect");
					viz = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					viz.transform.position = c.bounds.center;
					viz.transform.localScale = c.bounds.size;
					viz.transform.rotation = c.transform.rotation;
					viz.name = "ColliderVisualizer";
					GameObject.DestroyImmediate(viz.GetComponent<CapsuleCollider>());
				}
			} else {
				mod.Logger.Log("Error: This collider type is not supported");
			}

			if (viz != null) { // if we didn't skip viz for some reason or another
				allColliderDisplays.Add(viz);
				/*MeshRenderer rend = viz.GetComponent<MeshRenderer>();
				Color matCol = rend.material.color;
				matCol.a = .5f;
				rend.material.color = matCol;*/
			}
		}
		public static Vector3 GetPlayerPosition() {
			if(GetPlayerSingleton() == null) {
				return new Vector3(-1, -1, -1);
			}
			return GetPlayerSingleton().transform.position;
		}
		public static void SetPlayerPosition(Vector3 pos) {
			if(GetPlayerSingleton() == null) {
				return;
			}
			GetPlayerSingleton().transform.position = pos;
		}
		static Player GetPlayerSingleton() {
			if (currentPlayer == null)
				currentPlayer = GameObject.FindObjectOfType<Player>();
			return currentPlayer;
		}
	}


	public class SpeedrunHelperUI : MonoBehaviour {

		bool displayOn = true;
		string savedX = "", savedY = "", savedZ = "";
		bool tpX = true, tpY = false, tpZ = true, triggerOnly = true;
		bool capsuleCollider = true, sphereCollider = true, cubeCollider = true, meshCollider = false;
		int currentPanel = 0;
		void OnGUI() {
			if (!displayOn) {
				return;
			}
			GUI.Box(new Rect(10, 10, 250, 200), "Warden Speedrun Helper");
			if (currentPanel == 0) {
				// Position Hax
				GUI.Label(new Rect(15, 40, 2222, 2222), "Current Pos:");

				Vector3 currPos = SpeedrunHelperMain.GetPlayerPosition();
				GUI.Label(new Rect(95, 40, 200, 20), currPos.ToString());
				/*GUI.Label(new Rect(95, 40, 50, 20), "x: " + currPos.x.ToString());
				GUI.Label(new Rect(150, 40, 50, 20), "y: " + currPos.y.ToString());
				GUI.Label(new Rect(205, 40, 50, 20), "z: " + currPos.z.ToString());*/

				GUI.Label(new Rect(15, 65, 2222, 2222), "Saved Pos:");
				savedX = GUI.TextField(new Rect(95, 65, 50, 20), savedX);
				savedY = GUI.TextField(new Rect(150, 65, 50, 20), savedY);
				savedZ = GUI.TextField(new Rect(205, 65, 50, 20), savedZ);
				if (GUI.Button(new Rect(15, 90, 100, 28), "Save Pos")) {
					savedX = currPos.x.ToString();
					savedY = currPos.y.ToString();
					savedZ = currPos.z.ToString();
				}
				if (GUI.Button(new Rect(120, 90, 130, 28), "Teleport to Saved")) {
					Vector3 newPos = Vector3.zero;
					newPos.x = float.Parse(savedX);
					newPos.y = float.Parse(savedY);
					newPos.z = float.Parse(savedZ);
					SpeedrunHelperMain.SetPlayerPosition(newPos);
				}
				if (GUI.Button(new Rect(15, 125, 100, 28), "Teleport to:")) {
					Vector3 newPos = currPos;
					if(tpX)
						newPos.x = float.Parse(savedX);
					if (tpY)
						newPos.y = float.Parse(savedY);
					if (tpZ)
						newPos.z = float.Parse(savedZ);
					SpeedrunHelperMain.SetPlayerPosition(newPos);
				}
				tpX = GUI.Toggle(new Rect(120, 130, 25, 28), tpX, "X");
				tpY = GUI.Toggle(new Rect(150, 130, 25, 28), tpY, "Y");
				tpZ = GUI.Toggle(new Rect(180, 130, 25, 28), tpZ, "Z");
			} else if (currentPanel == 1) {
				// Visualizer Hax
				if (GUI.Button(new Rect(15, 40, 125, 28), "Visualize Colliders")) {
					bool[] colliderDisplayTypes = {capsuleCollider,sphereCollider, cubeCollider,meshCollider};
					SpeedrunHelperMain.DisplayHitboxes(triggerOnly, colliderDisplayTypes);
				}
				triggerOnly = GUI.Toggle(new Rect(150, 45, 100, 20), triggerOnly, "Triggers Only");
				if (GUI.Button(new Rect(15, 70, 240, 28), "Delete Visualized Colliders")) {
					SpeedrunHelperMain.DestroyHitboxDisplays();
				}

				capsuleCollider = GUI.Toggle(new Rect(15, 100, 80, 28), capsuleCollider, "Capsule");
				sphereCollider = GUI.Toggle(new Rect(147, 100, 80, 28), sphereCollider, "Sphere");
				cubeCollider = GUI.Toggle(new Rect(15, 130, 80, 28), cubeCollider, "Cube");
				meshCollider = GUI.Toggle(new Rect(147, 130, 80, 28), meshCollider, "Mesh");

			} else if (currentPanel == 2) {
				// Other
				GUI.Label(new Rect(15, 40, 2222, 2222), "Map: " + Application.loadedLevelName);
			}

			int[] offs = { 0, 0, 0 };

			offs[currentPanel] = 5;
			if (GUI.Button(new Rect(10, 181 - 2 * offs[0], 80, 28 + 2 * offs[0]), "Location")) {
				currentPanel = 0;
			}
			if (GUI.Button(new Rect(90, 181 - 2 * offs[1], 80, 28 + 2 * offs[1]), "Hitboxes")) {
				currentPanel = 1;
			}
			if (GUI.Button(new Rect(170, 181 - 2 * offs[2], 90, 28 + 2 * offs[2]), "Info")) {
				currentPanel = 2;
			}

		}


		void Update() {
			if (Input.GetKeyDown(KeyCode.LeftAlt)) {
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
			}
			if (Input.GetKeyDown(KeyCode.F4)) {
				displayOn = !displayOn;
			}
		}
	}

}
