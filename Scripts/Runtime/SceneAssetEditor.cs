using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

#if UNITY_EDITOR
namespace ICKX.Apron {

	[CustomEditor (typeof (SceneAsset))]
	public class SceneAssetInspector : Editor {

		public enum SceneSetMode {
			None = 0,
			RootScene,
			Landscape,
		}

		SceneAsset sceneAsset;
		SceneSetMode sceneSetMode = SceneSetMode.None;
		string catalogName;
		string[] modeToolBarNames;

		bool isRootScene { get { return (sceneSetMode != SceneSetMode.None); } }
		bool isLandscapeSceneSet { get { return (sceneSetMode == SceneSetMode.Landscape); } }
		
		SerializedProperty sceneSetProperty = null;

		SceneSetCatalog catalog = null;
		SerializedObject sobjCatalog = null;

		ReorderableList sceneNameList = null;
		ReorderableList subStaticSceneNameList = null;
		ReorderableList subDynamicSceneNameList = null;

		private void OnEnable () {
			sceneAsset = target as SceneAsset;
			modeToolBarNames = System.Enum.GetNames (typeof (SceneSetMode));

			OnUpdateSceneAsset ();
		}

		private void OnChangeSceneSetProperty () {
			if (sobjCatalog == null || sceneSetProperty == null) return;
			var prop = sceneSetProperty.FindPropertyRelative ("sceneNames");
			sceneNameList = new ReorderableList (sobjCatalog, prop);

			sceneNameList.drawElementCallback = (rect, index, isActive, isFocused) => {
				var element = prop.GetArrayElementAtIndex (index);
				rect.height -= 4;
				rect.y += 2;
				EditorGUI.PropertyField (rect, element, new GUIContent("Scene " + index));
			};
		}

		private void OnChangeLandscapeSceneSetProperty () {
			if (sobjCatalog == null || sceneSetProperty == null) return;

			var prop = sceneSetProperty.FindPropertyRelative ("subStaticSceneNames");
			subStaticSceneNameList = new ReorderableList (sobjCatalog, prop);

			subStaticSceneNameList.drawElementCallback = (rect, index, isActive, isFocused) => {
				var element = prop.GetArrayElementAtIndex (index);
				rect.height -= 4;
				rect.y += 2;
				EditorGUI.PropertyField (rect, element, new GUIContent ("SubStaticScene " + index));
			};

			prop = sceneSetProperty.FindPropertyRelative ("subDynamicSceneNames");
			subDynamicSceneNameList = new ReorderableList (sobjCatalog, prop);

			subDynamicSceneNameList.drawElementCallback = (rect, index, isActive, isFocused) => {
				var element = prop.GetArrayElementAtIndex (index);
				rect.height -= 4;
				rect.y += 2;
				EditorGUI.PropertyField (rect, element, new GUIContent ("SubDynamicScene " + index));
			};
		}

		private void OnUpdateSceneAsset () {

			var guids = AssetDatabase.FindAssets ("t:SceneSetCatalog");

			foreach (var guid in guids) {
				var path = AssetDatabase.GUIDToAssetPath (guid);
				catalog = AssetDatabase.LoadAssetAtPath<SceneSetCatalog> (path);
				if (catalog == null) continue;

				var sobj = new SerializedObject (catalog);

				if (ContainInCatalog (sobj, sceneAsset.name, out sceneSetMode, out sceneSetProperty)) {
					sobjCatalog = sobj;
					catalogName = System.IO.Path.GetFileNameWithoutExtension(path);
					if (sceneSetMode == SceneSetMode.RootScene) {
						OnChangeSceneSetProperty ();
					}
					if (sceneSetMode == SceneSetMode.Landscape) {
						OnChangeLandscapeSceneSetProperty ();
					}
					return;
				} else {
					sobj.Dispose ();
				}
			}

			catalogName = "";
			sceneSetMode = SceneSetMode.None;
			sceneSetProperty = null;
			sobjCatalog = null;
		}

		private void OnDisable () {
			if (sobjCatalog != null) {
				sobjCatalog.Dispose ();
			}
		}

		SerializedObject FindCatalogByName (string name) {
			var guids = AssetDatabase.FindAssets (name + " t:SceneSetCatalog");

			if(guids.Length > 0) {
				foreach (var guid in guids) {
					var path = AssetDatabase.GUIDToAssetPath (guid);
					if (System.IO.Path.GetFileNameWithoutExtension (path) != name) continue;
					catalog = AssetDatabase.LoadAssetAtPath<SceneSetCatalog> (path);
					if (catalog == null) continue;
					return new SerializedObject (catalog);
				}
			}
			return null;
		}

		bool ContainInCatalog (SerializedObject sobj, string sceneSetName, out SceneSetMode mode, out SerializedProperty sprop) {
			var spropLandscapeSceneSet = sobj.FindProperty ("landscapeSceneSetList");
			var spropSceneSetList = sobj.FindProperty ("sceneSetList");

			for (int i = 0; i < spropLandscapeSceneSet.arraySize; i++) {
				var element = spropLandscapeSceneSet.GetArrayElementAtIndex (i);
				var spopSceneSetName = element.FindPropertyRelative ("sceneSetName");

				if (spopSceneSetName.stringValue == sceneSetName) {
					mode = SceneSetMode.Landscape;
					sprop = element;
					return true;
				}
			}

			for (int i = 0; i < spropSceneSetList.arraySize; i++) {
				var element = spropSceneSetList.GetArrayElementAtIndex (i);
				var spopSceneSetName = element.FindPropertyRelative ("sceneSetName");

				if (spopSceneSetName.stringValue == sceneSetName) {
					mode = SceneSetMode.RootScene;
					sprop = element;
					return true;
				}
			}
			mode = SceneSetMode.None;
			sprop = null;
			return false;
		}

		SerializedProperty AddLandscapeSceneSet () {
			if (sobjCatalog == null) return null;

			var spropLandscapeSceneSet = sobjCatalog.FindProperty ("landscapeSceneSetList");
			spropLandscapeSceneSet.InsertArrayElementAtIndex (spropLandscapeSceneSet.arraySize);
			var element = spropLandscapeSceneSet.GetArrayElementAtIndex (spropLandscapeSceneSet.arraySize - 1);
			var spopSceneSetName = element.FindPropertyRelative ("sceneSetName");
			spopSceneSetName.stringValue = sceneAsset.name;
			return element;
		}

		SerializedProperty AddSceneSet () {
			if (sobjCatalog == null) return null;

			var spropLandscapeSceneSet = sobjCatalog.FindProperty ("sceneSetList");
			spropLandscapeSceneSet.InsertArrayElementAtIndex (spropLandscapeSceneSet.arraySize);
			var element = spropLandscapeSceneSet.GetArrayElementAtIndex (spropLandscapeSceneSet.arraySize - 1);
			var spopSceneSetName = element.FindPropertyRelative ("sceneSetName");
			spopSceneSetName.stringValue = sceneAsset.name;
			var spropSceneNames = element.FindPropertyRelative ("sceneNames");
			spropSceneNames.ClearArray ();
			spropSceneNames.InsertArrayElementAtIndex (0);
			spropSceneNames.GetArrayElementAtIndex (0).stringValue = sceneAsset.name;

			return element;
		}

		void RemoveProperty () {
			if (sobjCatalog == null) return;

			var spropLandscapeSceneSet = sobjCatalog.FindProperty ("landscapeSceneSetList");
			var spropSceneSetList = sobjCatalog.FindProperty ("sceneSetList");

			int index = 0;
			while (index < spropLandscapeSceneSet.arraySize) {

				var element = spropLandscapeSceneSet.GetArrayElementAtIndex (index);
				var spopSceneSetName = element.FindPropertyRelative ("sceneSetName");

				if (spopSceneSetName.stringValue == sceneAsset.name) {
					spropLandscapeSceneSet.DeleteArrayElementAtIndex (index);
				} else {
					index++;
				}
			}

			index = 0;
			while (index < spropSceneSetList.arraySize) {

				var element = spropSceneSetList.GetArrayElementAtIndex (index);
				var spopSceneSetName = element.FindPropertyRelative ("sceneSetName");

				if (spopSceneSetName.stringValue == sceneAsset.name) {
					spropSceneSetList.DeleteArrayElementAtIndex (index);
				} else {
					index++;
				}
			}
		}

		public override void OnInspectorGUI () {

			GUI.enabled = true;

			var tempSceneAsset = target as SceneAsset;

			if(tempSceneAsset.name != sceneAsset.name) {
				sceneAsset = tempSceneAsset;
				OnUpdateSceneAsset ();
			}

			int tempMode = GUILayout.Toolbar ((int)sceneSetMode, modeToolBarNames);

			if (tempMode != (int)sceneSetMode) {

				if (sobjCatalog != null) {
					sobjCatalog.Update ();
					RemoveProperty ();
					sobjCatalog.ApplyModifiedProperties ();
				}

				if(string.IsNullOrEmpty(catalogName)) {
					catalogName = SceneSetCatalog.DefaultCatalogName;
				}
				sobjCatalog = FindCatalogByName (catalogName);

				if (sobjCatalog == null) {
					Debug.LogWarning ($"catalogName={catalogName} ‚ª‘¶Ý‚µ‚Ä‚¢‚Ü‚¹‚ñ");
					return;
				}

				sceneSetMode = (SceneSetMode)tempMode;

				sobjCatalog.Update ();
				if (sceneSetMode == SceneSetMode.RootScene) {
					sceneSetProperty = AddSceneSet ();
					OnChangeSceneSetProperty ();
				}
				if (sceneSetMode == SceneSetMode.Landscape) {
					sceneSetProperty = AddLandscapeSceneSet ();
					OnChangeLandscapeSceneSetProperty ();
				}
				sobjCatalog.ApplyModifiedProperties ();
			}

			if (isRootScene) {
				string tempCatalogName = EditorGUILayout.TextField ("catalogName", catalogName);

				if (tempCatalogName != catalogName) {
					var newSobjCatalog = FindCatalogByName (tempCatalogName);
					if (newSobjCatalog != null) {
						sobjCatalog.Update ();
						RemoveProperty ();
						sobjCatalog.ApplyModifiedProperties ();
						sobjCatalog = newSobjCatalog;

						sobjCatalog.Update ();
						if (sceneSetMode == SceneSetMode.RootScene) {
							sceneSetProperty = AddSceneSet ();
							OnChangeSceneSetProperty ();
						}
						if (sceneSetMode == SceneSetMode.Landscape) {
							sceneSetProperty = AddLandscapeSceneSet ();
							OnChangeLandscapeSceneSetProperty ();
						}
						sobjCatalog.ApplyModifiedProperties ();
					}
					catalogName = tempCatalogName;
				}

				if (sobjCatalog != null) {

					sobjCatalog.Update ();

					EditorGUILayout.PropertyField (sceneSetProperty.FindPropertyRelative ("sceneSetName"));

					if (sceneSetMode == SceneSetMode.RootScene) {
						var propIsBuildSceneSet = sceneSetProperty.FindPropertyRelative ("isBuildSceneSet");
						var tempToggle = EditorGUILayout.Toggle ("Is Build SceneSet", propIsBuildSceneSet.boolValue);
						EditorGUILayout.PropertyField (sceneSetProperty.FindPropertyRelative ("landscapeSceneSetName"), true);
						
						//EditorGUILayout.PropertyField (sceneSetProperty.FindPropertyRelative ("sceneNames"), true);
						if(sceneNameList != null) sceneNameList.DoLayoutList ();

						if (propIsBuildSceneSet.boolValue != tempToggle) {
							propIsBuildSceneSet.boolValue = tempToggle;

							EditorApplication.delayCall += () => {
								catalog.UpdateBuildSettings ();
							};
						}
					}
					if (sceneSetMode == SceneSetMode.Landscape) {
						subStaticSceneNameList.DoLayoutList ();
						subDynamicSceneNameList.DoLayoutList ();
					}

					sobjCatalog.ApplyModifiedProperties ();

					EditorGUILayout.Space ();

					if (GUILayout.Button ("Open Scene")) {
						var settingsObject = sobjCatalog.targetObject as SceneSetCatalog;


						if (sceneSetMode == SceneSetMode.None) {
							string path = AssetDatabase.GetAssetPath (sceneAsset);
							EditorSceneManager.OpenScene (path, OpenSceneMode.Single);
						}
						if (sceneSetMode == SceneSetMode.RootScene) {
							SceneSet sceneSet = null;
							if (settingsObject.sceneSetTable.TryGetValue (sceneAsset.name, out sceneSet)) {
								sceneSet.OpenSceneInEditor ();
							}
						}
						if (sceneSetMode == SceneSetMode.Landscape) {
							LandscapeSceneSet sceneSet = null;
							if (settingsObject.landscapeSceneSetTable.TryGetValue (sceneAsset.name, out sceneSet)) {
								sceneSet.OpenSceneInEditor ();
							}
						}
					}
				}
			}

			GUI.enabled = false;
		}
	}
}
#endif
