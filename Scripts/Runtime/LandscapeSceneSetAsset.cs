using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ICKX.Apron {

	[System.Serializable]
	public class LandscapeSceneSet : SceneSetBase {

//#if UNITY_EDITOR
//		[ObjectToStringField (typeof(SceneAsset))]
//#endif
//		public string activeScenNames;
#if UNITY_EDITOR
		[ObjectToStringField (typeof (SceneAsset))]
#endif
		public string[] subStaticSceneNames;
#if UNITY_EDITOR
		[ObjectToStringField (typeof (SceneAsset))]
#endif
		public string[] subDynamicSceneNames;

#if UNITY_EDITOR
		public override void OpenSceneInEditor () {
			var mainScenePath = SceneSetCatalog.FindSceneAssetPath (sceneSetName);

			if (string.IsNullOrEmpty (mainScenePath)) {
				Debug.LogError (sceneSetName + " シーンは見つかりませんでした");
				return;
			}

			EditorSceneManager.OpenScene (mainScenePath, OpenSceneMode.Single);

			foreach (var subSceneName in subStaticSceneNames) {
				var subScenePath = SceneSetCatalog.FindSceneAssetPath (subSceneName);
				if (!string.IsNullOrEmpty (subScenePath)) {
					EditorSceneManager.OpenScene (subScenePath, OpenSceneMode.Additive);
				}
			}
			foreach (var subSceneName in subDynamicSceneNames) {
				var subScenePath = SceneSetCatalog.FindSceneAssetPath (subSceneName);
				if (!string.IsNullOrEmpty (subScenePath)) {
					EditorSceneManager.OpenScene (subScenePath, OpenSceneMode.Additive);
				}
			}
		}
#endif
	}
}
