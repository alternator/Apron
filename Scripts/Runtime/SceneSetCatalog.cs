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
		private List<SceneInfo> sceneInfoList;
		[SerializeField]
		private List<SceneSet> sceneSetList;

		public Dictionary<string, SceneInfo> sceneInfoTable = null;

		public Dictionary<string, SceneSet> sceneSetTable = null;

		public void OnAfterDeserialize () {
			if (sceneInfoList != null) {
				sceneInfoTable = sceneInfoList.ToDictionary (e => e.sceneName, e => e);
			}
			if (sceneSetList != null) {
				sceneSetTable = sceneSetList.ToDictionary (e => e.sceneSetName, e => e);
			}
		}

		public void OnBeforeSerialize () {
			if (sceneInfoTable != null) {
				sceneInfoList = sceneInfoTable.Values.ToList ();
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
			}

			foreach(var guid in guids) {
				string path = AssetDatabase.GUIDToAssetPath (guid);
				if(System.IO.Path.GetFileName(path) == $"{sceneName}.unity") {
					return path;
				}
			}
			return null;
		}

		public void UpdateBuildSettings () {
			var list = EditorBuildSettings.scenes.ToList();

			foreach (var sceneInfo in sceneInfoList) {
				if (!sceneInfo.isBuild) continue;
				string path = FindSceneAssetPath (sceneInfo.sceneName);

				if (!string.IsNullOrEmpty (path)) {
					var setting = list.FirstOrDefault (b=>b.path == path);

					if (setting != null) {
						setting.enabled = true;
					} else {
						list.Add (new EditorBuildSettingsScene (path, true));
					}
				}
			}

			EditorBuildSettings.scenes = list
				.Distinct()
				.ToArray ();

			Debug.Log ("UpdateBuildSettings Complete");
		}
#endif
	}
}