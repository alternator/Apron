using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ICKX.Apron {

	[CreateAssetMenu (menuName = "ICKX/SceneSetCatalog")]
	public class SceneSetCatalog : ScriptableObject, ISerializationCallbackReceiver {

		public const string DefaultCatalogName = "MainSceneSetCatalog";

		[SerializeField]
		private List<LandscapeSceneSet> landscapeSceneSetList;
		[SerializeField]
		private List<SceneSet> sceneSetList;

		public Dictionary<string, LandscapeSceneSet> landscapeSceneSetTable = null;

		public Dictionary<string, SceneSet> sceneSetTable = null;

		public void OnAfterDeserialize () {
			if (landscapeSceneSetList != null) {
				landscapeSceneSetTable = landscapeSceneSetList.ToDictionary (e => e.sceneSetName, e => e);
			}
			if (sceneSetList != null) {
				sceneSetTable = sceneSetList.ToDictionary (e => e.sceneSetName, e => e);
			}
		}

		public void OnBeforeSerialize () {
			if (landscapeSceneSetTable != null) {
				landscapeSceneSetList = landscapeSceneSetTable.Values.ToList ();
			}
			if (sceneSetTable != null) {
				sceneSetList = sceneSetTable.Values.ToList ();
			}
		}

		public static SceneSetCatalog FindDefaultCatalog () {
			return Resources.Load<SceneSetCatalog> (DefaultCatalogName);
		}

#if UNITY_EDITOR
		[InitializeOnLoadMethod]
		static void InitializeOnLoad () {
			if (Application.isPlaying) return;

			if(FindDefaultCatalog() == null) {
				if(!EditorPrefs.GetBool("ICKX/Apron/IgnoreCreateDefaultCatalogMessage", false)) {
					string title = "SceneSetCatalogの作成";
					string message = 
						"SceneSetManagerを利用するためには、デフォルトのSceneSetCatalogを作成する必要があります。\n" +
						"Assets/Resources/MainSceneSetCatalog.assetを作成してもよろしいですか？ \n" +
						"(MainSceneSetCatalog.assetは任意のResourcesフォルダに移動しても問題ありません。)";

					int id = EditorUtility.DisplayDialogComplex (title, message, "Yes", "No", "Don't show again");
					switch (id) {
						case 0:
							if(!AssetDatabase.IsValidFolder("Assets/Resources")) {
								AssetDatabase.CreateFolder ("Assets", "Resources");
							}
							AssetDatabase.CreateAsset (CreateInstance<SceneSetCatalog> (), $"Assets/Resources/{DefaultCatalogName}.asset");
							break;
						case 1:
							break;
						case 2:
							EditorPrefs.SetBool ("ICKX/Apron/IgnoreCreateDefaultCatalogMessage", true);
							break;
					}
				}
			}
		}
		
		public static string FindSceneAssetPath (string sceneName) {

			if(string.IsNullOrEmpty(sceneName)) {
				return null;
			}

			string[] guids = AssetDatabase.FindAssets (sceneName + " t:SceneAsset");

			if(guids.Length == 0) {
				return null;
			}else if(guids.Length > 2){
				Debug.LogWarning (sceneName + "は同名のシーンがあります");
			}
			return AssetDatabase.GUIDToAssetPath (guids[0]);
		}

		public void UpdateBuildSettings () {
			List<string> list = new List<string> ();

			foreach (var sceneSet in sceneSetList) {
				if (!sceneSet.isBuildSceneSet) continue;

				string path = FindSceneAssetPath (sceneSet.sceneSetName);
				if (!string.IsNullOrEmpty (path)) {
					list.Add (path);
				}

				if (!string.IsNullOrEmpty(sceneSet.landscapeSceneSetName)) {
					var landSceneSet = landscapeSceneSetTable[sceneSet.landscapeSceneSetName];


					path = FindSceneAssetPath (landSceneSet.sceneSetName);
					if (!string.IsNullOrEmpty (path)) {
						list.Add (path);
					}

					foreach (var subSceneName in landSceneSet.subStaticSceneNames) {
						path = FindSceneAssetPath (subSceneName);
						if (!string.IsNullOrEmpty (path)) {
							list.Add (path);
						}
					}

					foreach (var subSceneName in landSceneSet.subDynamicSceneNames) {
						path = FindSceneAssetPath (subSceneName);
						if (!string.IsNullOrEmpty (path)) {
							list.Add (path);
						}
					}

					path = FindSceneAssetPath (landSceneSet.sceneSetName);
					if (!string.IsNullOrEmpty (path)) {
						list.Add (path);
					}
				}

				foreach (var subSceneName in sceneSet.sceneNames) {
					path = FindSceneAssetPath (subSceneName);
					if (!string.IsNullOrEmpty (path)) {
						list.Add (path);
					}
				}
			}

			EditorBuildSettings.scenes = list
				.Distinct()
				.Select(s=>new EditorBuildSettingsScene( s, true))
				.ToArray ();
		}
#endif
	}
}