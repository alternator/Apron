using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ICKX.Apron {

	public enum LoadSceneSetMode {
		Clean,
		CleanDynamic,
		Diff,
		Add,
	}

	public static class SceneSetManager {

		public static LoadSceneSetAsyncOperation currentLoadOperation { get; private set; }

		private static List<SceneSetCatalog> m_catalogs = null;

		public static IReadOnlyList<SceneSetCatalog> catalogs { get { return m_catalogs; } }

		public static void Initialize () {
			m_catalogs = new List<SceneSetCatalog> ();
			m_catalogs.Add (SceneSetCatalog.FindDefaultCatalog ());
		}

		public static void AddCatalog (SceneSetCatalog catalog) {
			m_catalogs.Add (catalog);
		}

		public static SceneInfo GetSceneInfoByName (string name) {
			SceneInfo sceneInfo = null;
			foreach (var catalog in m_catalogs) {;
				if (catalog.sceneInfoTable.TryGetValue (name, out sceneInfo)) {
					return sceneInfo;
				}
			}
			return null;
		}

		public static SceneSet GetSceneSetByName (string name) {
			SceneSet sceneSet = null;
			foreach (var catalog in m_catalogs) {
				if (catalog.sceneSetTable.TryGetValue (name, out sceneSet)) {
					return sceneSet;
				}
			}
			return null;
		}

		public static LoadSceneSetAsyncOperation LoadSceneSetAsync (string sceneSetName, LoadSceneSetMode mode) {
			int i = 0;
			var sceneSet = GetSceneSetByName (sceneSetName);

			LoadSceneSetAsyncOperation op = new LoadSceneSetAsyncOperation ();
			op.loadSceneSetMode = mode;

			//読み込み済みシーンから次のシーンに必要ないシーンを削除
			for (i = 0; i < SceneManager.sceneCount; i++) {
				var scene = SceneManager.GetSceneAt (i);
				var info = GetSceneInfoByName (scene.name);

				if (info == null) {
					info = new SceneInfo () { sceneName = scene.name, isBuild = true, sceneType = SceneInfo.SceneType.Dynamic };
				}
				if (info.sceneType == SceneInfo.SceneType.Permanent) continue;

				switch (mode) {
					case LoadSceneSetMode.Clean:
						op.unloadRequestScenes.Add (scene.name);
						break;
					case LoadSceneSetMode.CleanDynamic:
						if (info.sceneType == SceneInfo.SceneType.Static) {
							if (!ContainSceneName (sceneSet, scene.name)) {
								op.unloadRequestScenes.Add (scene.name);
							}
						} else if (info.sceneType == SceneInfo.SceneType.Dynamic) {
								op.unloadRequestScenes.Add (scene.name);
						}
						break;
					case LoadSceneSetMode.Diff:
						if (!ContainSceneName (sceneSet, scene.name)) {
							op.unloadRequestScenes.Add (scene.name);
						}
						break;
					case LoadSceneSetMode.Add:
						break;
				}
			}

			foreach (string sceneName in sceneSet.sceneNames) {
				var info = GetSceneInfoByName (sceneName);

				switch (info.sceneType) {
					case SceneInfo.SceneType.Permanent:
						if (!SceneManager.GetSceneByName (sceneName).isLoaded) {
							op.asyncOperations[sceneName] = SceneManager.LoadSceneAsync (sceneName, LoadSceneMode.Additive);
						}
						break;
					case SceneInfo.SceneType.Static:
						if (SceneManager.GetSceneByName (sceneName).isLoaded) {
							if (mode == LoadSceneSetMode.Clean) {
								op.asyncOperations[sceneName] = SceneManager.LoadSceneAsync (sceneName, LoadSceneMode.Additive);
							}
						}else {
							op.asyncOperations[sceneName] = SceneManager.LoadSceneAsync (sceneName, LoadSceneMode.Additive);
						}
						break;
					case SceneInfo.SceneType.Dynamic:
						if (SceneManager.GetSceneByName (sceneName).isLoaded) {
							if (mode == LoadSceneSetMode.Clean || mode == LoadSceneSetMode.CleanDynamic) {
								op.asyncOperations[sceneName] = SceneManager.LoadSceneAsync (sceneName, LoadSceneMode.Additive);
							}
						} else {
							op.asyncOperations[sceneName] = SceneManager.LoadSceneAsync (sceneName, LoadSceneMode.Additive);
						}
						break;
				}
			}
			op.activeSceneName = sceneSet.sceneNames[0];
			return op;
		}

		public static UnloadSceneSetAsyncOperation UnloadSceneDynamicAsync () {
			var unloadOp = new UnloadSceneSetAsyncOperation ();
			for (int i = 0; i < SceneManager.sceneCount; i++) {
				var scene = SceneManager.GetSceneAt (i);
				var info = GetSceneInfoByName (scene.name);

				if(info == null || info.sceneType == SceneInfo.SceneType.Dynamic) {
					var op = SceneManager.UnloadSceneAsync (scene.name);
					unloadOp.asyncOperations[scene.name] = op;
				}
			}
			return unloadOp;
		}

		public static bool ContainSceneName (SceneSet sceneSet, string sceneName) {
			foreach (string sceneNameInSceneSet in sceneSet.sceneNames) {
				if (sceneName == sceneNameInSceneSet) {
					return true;
				}
			}
			return false;
		}

		public static string GetActiveSceneName (SceneSet sceneSet) {
			if (sceneSet == null) return null;
			return (sceneSet.sceneNames.Length == 0) ? null : sceneSet.sceneNames[0];
		}
	}

	public class UnloadSceneSetAsyncOperation : CustomYieldInstruction {

		public Dictionary<string, AsyncOperation> asyncOperations { get; private set; }

		public UnloadSceneSetAsyncOperation () {
			asyncOperations = new Dictionary<string, AsyncOperation> ();
		}

		public override bool keepWaiting {
			get {
				return !isDone;
			}
		}

		public bool isDone {
			get {
				bool isAllDone = true;
				foreach (var pair in asyncOperations) {
					if (!pair.Value.isDone) {
						isAllDone = false;
						break;
					}
				}
				return isAllDone;
			}
		}

		public float progress {
			get {
				float rate = 0.0f;
				foreach (var pair in asyncOperations) {
					rate += pair.Value.progress;
				}
				rate /= asyncOperations.Count;
				return rate;
			}
		}

	}

	public class LoadSceneSetAsyncOperation : CustomYieldInstruction {

		public string m_activeSceneName;
		private bool m_allowSceneActivation = false;

		public List<string> unloadRequestScenes { get; private set; }
		public Dictionary<string, AsyncOperation> asyncOperations { get; set; }
		public LoadSceneSetMode loadSceneSetMode { get; set; }

		public string activeSceneName {
			get { return m_activeSceneName; }
			set {
				m_activeSceneName = value;
				if (!asyncOperations.ContainsKey (activeSceneName)) return;
				asyncOperations[m_activeSceneName].completed += OnLoadCompletedActiveScene;
			}
		}

		public bool allowSceneActivation {
			get {
				return m_allowSceneActivation;
			}
			set {
				m_allowSceneActivation = value;

				foreach (var op in asyncOperations.Values) {
					op.allowSceneActivation = value;
				}
			}
		}

		public LoadSceneSetAsyncOperation () {
			unloadRequestScenes = new List<string> ();
			asyncOperations = new Dictionary<string, AsyncOperation> ();
		}

		public override bool keepWaiting {
			get {
				if (allowSceneActivation) {
					return isDone;
				} else {
					bool isAnyProgress = false;

					foreach (var pair in asyncOperations) {
						if (pair.Value.progress < 0.9f) {
							isAnyProgress = true;
							break;
						}
					}
					return isAnyProgress;
				}
			}
		}

		public bool isDone {
			get {
				bool isAllDone = true;
				foreach (var pair in asyncOperations) {
					if (!pair.Value.isDone) {
						isAllDone = false;
						break;
					}
				}
				return isAllDone;
			}
		}

		public float progress {
			get {
				float rate = 0.0f;
				foreach (var pair in asyncOperations) {
					rate += pair.Value.progress;
				}
				rate /= asyncOperations.Count;
				return rate;
			}
		}

		public AsyncOperation activeSceneAsyncOperation {
			get {
				if (asyncOperations.ContainsKey (activeSceneName)) return null;
				return asyncOperations[activeSceneName];
			}
		}

		public UnloadSceneSetAsyncOperation UnloadAllAsync () {
			var unloadOp = new UnloadSceneSetAsyncOperation ();
			for (int i = 0; i < unloadRequestScenes.Count; i++) {
				var op = SceneManager.UnloadSceneAsync (unloadRequestScenes[i]);
				unloadOp.asyncOperations[unloadRequestScenes[i]] = op;
			}
			return unloadOp;
		}

		private void OnLoadCompletedActiveScene (AsyncOperation op) {
			CoroutineManager.Start (SetActiveSceneDelay());
		}

		IEnumerator SetActiveSceneDelay () {
			Scene scene;
			do {
				yield return null;
				scene = SceneManager.GetSceneByName (activeSceneName);
			} while (!scene.isLoaded);
			SceneManager.SetActiveScene (scene);
		}
	}
}
