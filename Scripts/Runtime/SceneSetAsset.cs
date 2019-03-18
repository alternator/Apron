using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ICKX.Apron {

	[System.Serializable]
	public class SceneInfo {

		public enum SceneType {
			Dynamic,
			Static,
			Permanent,
		}

		[Disable]
		public string sceneName;
		public SceneType sceneType;
#if UNITY_EDITOR
		public bool isBuild;
#endif
	}

	[System.Serializable]
	public class SceneSet{
		[Disable]
		public string sceneSetName;

#if UNITY_EDITOR
		[ObjectToStringField (typeof (SceneAsset))]
#endif
		public string[] sceneNames;
		
#if UNITY_EDITOR
		public void OpenSceneInEditor () {

			var mode = OpenSceneMode.Single;

			foreach (var sceneName in sceneNames) {
				var subScenePath = SceneSetCatalog.FindSceneAssetPath (sceneName);
				if (!string.IsNullOrEmpty (subScenePath)) {
					EditorSceneManager.OpenScene (subScenePath, mode);
					mode = OpenSceneMode.Additive;
				}
			}
		}
#endif
	}
}
