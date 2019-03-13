using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ICKX.Apron {

	public abstract class SceneSetBase {

		[Disable]
		public string sceneSetName;

//		public string catalogPath = "SceneSetCatalog";

#if UNITY_EDITOR
		public abstract void OpenSceneInEditor ();
#endif
		
	}

	[System.Serializable]
	public class SceneSet : SceneSetBase{
#if UNITY_EDITOR
		public bool isBuildSceneSet;
#endif

#if UNITY_EDITOR
		[ObjectToStringField (typeof (SceneAsset))]
#endif
		public string landscapeSceneSetName;

#if UNITY_EDITOR
		[ObjectToStringField (typeof (SceneAsset))]
#endif
		public string[] sceneNames;
		
#if UNITY_EDITOR
		public override void OpenSceneInEditor () {

			var mode = OpenSceneMode.Single;

			if(!string.IsNullOrEmpty(landscapeSceneSetName)) {
				var setting = SceneSetCatalog.FindDefaultCatalog ();
				var landscapeSceneSet = setting.landscapeSceneSetTable[landscapeSceneSetName];
				landscapeSceneSet.OpenSceneInEditor ();
				mode = OpenSceneMode.Additive;
			}

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
