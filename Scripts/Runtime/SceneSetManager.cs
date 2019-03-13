using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ICKX.Apron {

	public enum LoadSceneSetMode {
		Clean,
		Diff,
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

		public static SceneSet GetSceneSetByName (string name) {
			SceneSet sceneSet = null;
			foreach (var catalog in m_catalogs) {
				if (catalog.sceneSetTable.TryGetValue (name, out sceneSet)) {
					return sceneSet;
				}
			}
			return null;
		}

		public static LandscapeSceneSet GetLandscapeSceneSetByName (string name) {
			LandscapeSceneSet sceneSet = null;
			foreach (var catalog in m_catalogs) {
				if (catalog.landscapeSceneSetTable.TryGetValue (name, out sceneSet)) {
					return sceneSet;
				}
			}
			return null;
		}

		public static void LoadSceneSet (string sceneSetName, LoadSceneSetMode mode) {
			int i = 0;
			var sceneSet = GetSceneSetByName (sceneSetName);

			//読み込み済みシーンから次のシーンに必要ないシーンを削除
			if (mode == LoadSceneSetMode.Diff) {
				for (i = 0; i < SceneManager.sceneCount; i++) {
					var scene = SceneManager.GetSceneAt (i);
					if (!ContainSceneName (sceneSet, scene.name)) {
						SceneManager.UnloadSceneAsync (scene);
					}
				}
			}

			i = 0;
			foreach (string sceneName in GetSceneNameEnumerable (sceneSet)) {

				if (mode == LoadSceneSetMode.Clean) {
					if (i == 0) {
						SceneManager.LoadScene (sceneName, LoadSceneMode.Single);
					} else {
						SceneManager.LoadScene (sceneName, LoadSceneMode.Additive);
					}
				} else if (mode == LoadSceneSetMode.Diff) {
					//読み込み済みシーン以外をロード.
					if (!SceneManager.GetSceneByName (sceneName).isLoaded) {
						SceneManager.LoadScene (sceneName, LoadSceneMode.Additive);
					}

					if (i == 0) {
						if (SceneManager.GetActiveScene ().name != sceneName) {
							SceneManager.SetActiveScene (SceneManager.GetSceneByName (sceneName));
						}
					}
				}
				i++;
			}
		}

		public static LoadSceneSetAsyncOperation LoadSceneSetAsync (string sceneSetName, LoadSceneSetMode mode) {
			int i = 0;
			var sceneSet = GetSceneSetByName (sceneSetName);

			LoadSceneSetAsyncOperation op = new LoadSceneSetAsyncOperation ();
			op.loadSceneSetMode = mode;

			//読み込み済みシーンから次のシーンに必要ないシーンを削除
			if (mode == LoadSceneSetMode.Diff) {
				for (i = 0; i < SceneManager.sceneCount; i++) {
					var scene = SceneManager.GetSceneAt (i);
					if (!ContainSceneName (sceneSet, scene.name)) {
						op.unloadRequestScenes.Add (scene.name);
					}
				}
			}

			i = 0;

			foreach (string sceneName in GetSceneNameEnumerable (sceneSet)) {

				if (mode == LoadSceneSetMode.Clean) {
					if (i == 0) {
						op.asyncOperations[sceneName] = SceneManager.LoadSceneAsync (sceneName, LoadSceneMode.Single);
					} else {
						op.asyncOperations[sceneName] = SceneManager.LoadSceneAsync (sceneName, LoadSceneMode.Additive);
					}
				} else if (mode == LoadSceneSetMode.Diff) {
					//読み込み済みシーン以外をロード.
					if (!SceneManager.GetSceneByName (sceneName).isLoaded) {
						op.asyncOperations[sceneName] = SceneManager.LoadSceneAsync (sceneName, LoadSceneMode.Additive);
					}
					if (i == 0) {
						if (SceneManager.GetActiveScene ().name != sceneName) {
							op.activeSceneName = sceneName;
						}
					}
				}
				i++;
			}
			return op;
		}

		public static bool ContainSceneName (SceneSet sceneSet, string sceneName) {
			foreach (string sceneNameInSceneSet in GetSceneNameEnumerable (sceneSet)) {

				if (sceneName == sceneNameInSceneSet) {
					return true;
				}
			}
			return false;
		}

		public static string GetActiveSceneName (SceneSet sceneSet) {
			if (!string.IsNullOrEmpty (sceneSet.landscapeSceneSetName)) {
				var landscape = GetLandscapeSceneSetByName (sceneSet.landscapeSceneSetName);
				return landscape.sceneSetName;
			}
			return sceneSet.sceneSetName;
		}

		public static IEnumerable<string> GetSceneNameEnumerable (SceneSet sceneSet) {
			if (!string.IsNullOrEmpty (sceneSet.landscapeSceneSetName)) {
				var landscape = GetLandscapeSceneSetByName (sceneSet.landscapeSceneSetName);

				yield return landscape.sceneSetName;
				foreach (var subSceneName in landscape.subStaticSceneNames) {
					if (string.IsNullOrEmpty (subSceneName)) continue;
					yield return subSceneName;
				}
				foreach (var subSceneName in landscape.subDynamicSceneNames) {
					if (string.IsNullOrEmpty (subSceneName)) continue;
					yield return subSceneName;
				}
			}

			foreach (var subSceneName in sceneSet.sceneNames) {
				if (string.IsNullOrEmpty (subSceneName)) continue;
				yield return subSceneName;
			}

//			yield return sceneSet.sceneSetName;
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
			SceneManager.SetActiveScene (SceneManager.GetSceneByName (activeSceneName));
		}
	}
}
