using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ICKX.Apron;
using UnityEditorInternal;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

#if UNITY_EDITOR
namespace ICKX.Apron {

	[CustomEditor (typeof (SceneAsset))]
	public class SceneAssetInspector : Editor {

		SceneAsset sceneAsset;
		bool isSceneSet;
		string catalogName;

		SerializedProperty sceneInfoProperty = null;
		SerializedProperty sceneSetProperty = null;

		SceneSetCatalog catalog = null;
		SerializedObject sobjCatalog = null;

		ReorderableList sceneNameList = null;

		int currentCatalogIndex = 0;
		string[] catalogPaths;
		string[] catalogNames;

		private void OnEnable () {
			sceneAsset = target as SceneAsset;

			OnUpdateSceneAsset ();
		}

		private void OnChangeSceneSetProperty (SerializedObject sobj) {
			if (sobj == null || sceneSetProperty == null) return;
			var prop = sceneSetProperty.FindPropertyRelative ("sceneNames");
			sceneNameList = new ReorderableList (sobj, prop);

			//sceneNameList.onChangedCallback = (list) => {
			//	sobjCatalog.ApplyModifiedProperties ();
			//};
			sceneNameList.drawHeaderCallback = (rect) => {
				EditorGUI.LabelField (rect, "Scene Names (Index=0 is Active)");
			};

			sceneNameList.drawElementCallback = (rect, index, isActive, isFocused) => {
				var element = prop.GetArrayElementAtIndex (index);
				rect.height -= 4;
				rect.y += 2;
				EditorGUI.PropertyField (rect, element, new GUIContent ("Scene " + index));
			};
		}

		private void OnUpdateSceneAsset () {

			var guids = AssetDatabase.FindAssets ("t:SceneSetCatalog");

			catalogName = "";
			isSceneSet = false;
			sceneSetProperty = null;
			sobjCatalog = null;

			catalogPaths = new string[guids.Length + 1];
			catalogNames = new string[guids.Length + 1];
			currentCatalogIndex = guids.Length;

			for (int i=0;i<guids.Length;i++) {
				var path = AssetDatabase.GUIDToAssetPath (guids[i]);
				catalog = AssetDatabase.LoadAssetAtPath<SceneSetCatalog> (path);
				if (catalog == null) continue;

				catalogPaths[i] = path;
				catalogNames[i] = catalog.name;

				var sobj = new SerializedObject (catalog);
				if (ContainSceneInfoInCatalog (sobj, sceneAsset.name, out var sprop)) {
					sceneInfoProperty = sprop;
					currentCatalogIndex = i;
					catalogName = System.IO.Path.GetFileNameWithoutExtension (path);
					sobjCatalog = sobj;
					if (ContainSceneSetInCatalog (sobj, sceneAsset.name, out sceneSetProperty)) {
						isSceneSet = true;
						OnChangeSceneSetProperty (sobjCatalog);
					}
				} else {
					sobj.Dispose ();
				}
			}
			catalogPaths[guids.Length] = "";
			catalogNames[guids.Length] = "None";
		}

		private void OnDisable () {
			if (sobjCatalog != null) {
				sobjCatalog.Dispose ();
			}
		}

		SerializedObject FindCatalogByName (string name) {
			var guids = AssetDatabase.FindAssets (name + " t:SceneSetCatalog");

			if (guids.Length > 0) {
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

		bool ContainSceneInfoInCatalog (SerializedObject sobj, string sceneName, out SerializedProperty sprop) {
			var spropSceneInfoList = sobj.FindProperty ("sceneInfoList");

			for (int i = 0; i < spropSceneInfoList.arraySize; i++) {
				var element = spropSceneInfoList.GetArrayElementAtIndex (i);
				var spopSceneName = element.FindPropertyRelative ("sceneName");

				if (spopSceneName.stringValue == sceneName) {
					sprop = element;
					return true;
				}
			}
			sprop = null;
			return false;
		}

		bool ContainSceneSetInCatalog (SerializedObject sobj, string sceneSetName, out SerializedProperty sprop) {
			var spropSceneSetList = sobj.FindProperty ("sceneSetList");

			for (int i = 0; i < spropSceneSetList.arraySize; i++) {
				var element = spropSceneSetList.GetArrayElementAtIndex (i);
				var spopSceneSetName = element.FindPropertyRelative ("sceneSetName");

				if (spopSceneSetName.stringValue == sceneSetName) {
					sprop = element;
					return true;
				}
			}
			sprop = null;
			return false;
		}

		SerializedProperty AddSceneInfo (SerializedObject sobj, SerializedProperty copy = null) {
			if (sobj == null) return null;

			var spropSceneInfoList = sobj.FindProperty ("sceneInfoList");

			SerializedProperty element = null, spopSceneSetName = null;
			for (int i = 0; i < spropSceneInfoList.arraySize; i++) {
				var tempElement = spropSceneInfoList.GetArrayElementAtIndex (i);
				spopSceneSetName = tempElement.FindPropertyRelative ("sceneName");
				if (spopSceneSetName.stringValue == sceneAsset.name) {
					element = tempElement;
					break;
				}
			}

			if (element == null) {
				spropSceneInfoList.InsertArrayElementAtIndex (spropSceneInfoList.arraySize);
				element = spropSceneInfoList.GetArrayElementAtIndex (spropSceneInfoList.arraySize - 1);
				spopSceneSetName = element.FindPropertyRelative ("sceneName");
				spopSceneSetName.stringValue = sceneAsset.name;
			}

			if(copy != null) {
				element.FindPropertyRelative ("sceneName").stringValue = copy.FindPropertyRelative ("sceneName").stringValue;
				element.FindPropertyRelative ("sceneType").boolValue = copy.FindPropertyRelative ("sceneType").boolValue;
				element.FindPropertyRelative ("isBuild").boolValue = copy.FindPropertyRelative ("isBuild").boolValue;
			} else {
				var spopSceneInfo = element.FindPropertyRelative ("sceneName");
				spopSceneInfo.stringValue = sceneAsset.name;
			}
			return element;
		}

		SerializedProperty AddSceneSet (SerializedObject sobj, SerializedProperty copy = null) {
			if (sobj == null) return null;

			var spropSceneSetList = sobj.FindProperty ("sceneSetList");

			SerializedProperty element = null, spopSceneSetName = null;
			for (int i=0;i<spropSceneSetList.arraySize;i++) {
				var tempElement = spropSceneSetList.GetArrayElementAtIndex (i);
				spopSceneSetName = tempElement.FindPropertyRelative ("sceneSetName");
				if(spopSceneSetName.stringValue == sceneAsset.name) {
					element = tempElement;
					break;
				}
			}

			if (element == null) {
				spropSceneSetList.InsertArrayElementAtIndex (spropSceneSetList.arraySize);
				element = spropSceneSetList.GetArrayElementAtIndex (spropSceneSetList.arraySize - 1);
				spopSceneSetName = element.FindPropertyRelative ("sceneSetName");
				spopSceneSetName.stringValue = sceneAsset.name;
			}

			var spropSceneNames = element.FindPropertyRelative ("sceneNames");

			if (copy != null) {
				var copyNames = copy.FindPropertyRelative ("sceneNames");
				spropSceneNames.ClearArray ();
				for (int i=0;i< copyNames.arraySize;i++) {
					spropSceneNames.InsertArrayElementAtIndex (i);
					spropSceneNames.GetArrayElementAtIndex (i).stringValue = copyNames.GetArrayElementAtIndex(i).stringValue;
				}
			} else {
				spropSceneNames.ClearArray ();
				spropSceneNames.InsertArrayElementAtIndex (0);
				spropSceneNames.GetArrayElementAtIndex (0).stringValue = sceneAsset.name;
			}
			return element;
		}

		void RemoveSceneInfoProperty (SerializedObject sobj) {
			if (sobj == null) return;

			var spropSceneInfoList = sobj.FindProperty ("sceneInfoList");

			int index = 0;
			while (index < spropSceneInfoList.arraySize) {

				var element = spropSceneInfoList.GetArrayElementAtIndex (index);
				var spopSceneSetName = element.FindPropertyRelative ("sceneName");

				if (spopSceneSetName.stringValue == sceneAsset.name) {
					spropSceneInfoList.DeleteArrayElementAtIndex (index);
				} else {
					index++;
				}
			}
		}

		void RemoveSceneSetProperty (SerializedObject sobj) {
			if (sobj == null) return;

			var spropSceneSetList = sobj.FindProperty ("sceneSetList");

			int index = 0;
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

			base.OnInspectorGUI ();

			var tempSceneAsset = target as SceneAsset;

			if (tempSceneAsset.name != sceneAsset.name) {
				sceneAsset = tempSceneAsset;
				OnUpdateSceneAsset ();
			}

			int tempIndex = EditorGUILayout.Popup (new GUIContent ("Select Catalog"), currentCatalogIndex, catalogNames);

			if(tempIndex != currentCatalogIndex && tempIndex != catalogNames.Length - 1) {
				currentCatalogIndex = tempIndex;
				var newSobjCatalog = FindCatalogByName (catalogNames[tempIndex]);
				if (newSobjCatalog != null) {
					newSobjCatalog.Update ();
					sceneInfoProperty = AddSceneInfo (newSobjCatalog, sceneInfoProperty);
					if (isSceneSet) {
						sceneSetProperty = AddSceneSet (newSobjCatalog, sceneSetProperty);
						OnChangeSceneSetProperty (newSobjCatalog);
					}
					newSobjCatalog.ApplyModifiedProperties ();

					if (sobjCatalog != null) {
						sobjCatalog.Update ();
						RemoveSceneInfoProperty (sobjCatalog);
						RemoveSceneSetProperty (sobjCatalog);
						sobjCatalog.ApplyModifiedProperties ();
					}

					sobjCatalog = newSobjCatalog;
					OnUpdateSceneAsset ();
				}
				return;
			}

			if (sobjCatalog != null) {

				sobjCatalog.Update ();

				//SceneInfo 
				using (var scopeSceneInfo = new EditorGUILayout.VerticalScope ("box")) {
					EditorGUILayout.LabelField ("Scene Info");
					EditorGUI.BeginChangeCheck ();

					EditorGUI.indentLevel++;
					var propSceneType = sceneInfoProperty.FindPropertyRelative ("sceneType");
					EditorGUILayout.PropertyField (propSceneType);
					var propIsBuild = sceneInfoProperty.FindPropertyRelative ("isBuild");
					EditorGUILayout.PropertyField (propIsBuild);
					EditorGUI.indentLevel--;

					if (EditorGUI.EndChangeCheck()) {
						sobjCatalog.ApplyModifiedProperties ();
					}

					if (GUILayout.Button ("Update Build Settings")) {
						sobjCatalog.Update ();
						catalog = sobjCatalog.targetObject as SceneSetCatalog;
						catalog.UpdateBuildSettings ();
					}
				}

				EditorGUILayout.Space ();

				//SceneSet
				using (var scopeSceneSet = new EditorGUILayout.VerticalScope ("box")) {
					EditorGUILayout.LabelField ("Scene Set");

					if (isSceneSet && sobjCatalog != null) {

						EditorGUI.BeginChangeCheck ();

						EditorGUI.indentLevel++;
						EditorGUILayout.PropertyField (sceneSetProperty.FindPropertyRelative ("sceneSetName"));
						sceneNameList.DoLayoutList ();
						EditorGUI.indentLevel--;
						if (EditorGUI.EndChangeCheck ()) {
							sobjCatalog.ApplyModifiedProperties ();
						}

						EditorGUILayout.Space ();

						using (var scopeButton = new EditorGUILayout.HorizontalScope()) {
							if (GUILayout.Button ("Open Scene")) {
								var settingsObject = sobjCatalog.targetObject as SceneSetCatalog;

								SceneSet sceneSet = null;
								if (settingsObject.sceneSetTable.TryGetValue (sceneAsset.name, out sceneSet)) {
									sceneSet.OpenSceneInEditor ();
								}
							}

							GUILayout.Space (50);

							if (GUILayout.Button ("Remove", GUILayout.ExpandWidth(false))) {
								isSceneSet = false;
								RemoveSceneSetProperty (sobjCatalog);
								sobjCatalog.ApplyModifiedProperties ();
							}
						}
					} else {
						if (GUILayout.Button ("Create SceneSet")) {
							isSceneSet = true;
							sceneSetProperty = AddSceneSet (sobjCatalog);
							OnChangeSceneSetProperty (sobjCatalog);
							sobjCatalog.ApplyModifiedProperties ();
						}
					}
				}
			}


			GUI.enabled = false;
		}
	}
}
#endif
