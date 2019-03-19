# 概要
Apronはマルチシーン管理を行いやすくする機能を提供します。
複数のSceneをまとめて**SceneSet**として扱い、**SceneSet**単位でロード/アンロードを行うことができます。

# Package Managerによるインストール
Packageフォルダにあるmanifest.jsonのdependenciesにあるパッケージに以下のように記載してください。
(jp.ickx.commonはProjectICKXのUnityライブラリで、汎用的に利用する機能をまとめたパッケージです。)

```
{
  "dependencies": {
    ...
    "com.unity.mathematics": "0.0.12-preview.19",
    "jp.ickx.common": "https://github.com/alternator/Common.git",
    "jp.ickx.apron": "https://github.com/alternator/Apron.git",
    ...
  }
}
```

# How To Use

## SceneSetCatalogの準備
SceneSetはSceneSetCatalogというScriptableObjectに保存されます。  
Apron導入後、ダイアログに従って"MainSceneSetCatalog.asset"を作成してください。これはResourcesフォルダ以下にあれば移動しても問題ありません。`SceneSetManager.Initialize();`の際に読み込まれます。

また、Proejct Viewで右クリックのメニューから "Create/ICKX/SceneSetCatalog" を選択すると作成することができます。   
![SceneSetCatalogの作成](wiki/Image/Apron_Doc_HowToUse00.PNG)

この方法で作った追加のSceneSetCatalogは明示的に読み込む必要があります。
```
SceneSetCatalog addonCatalog = assetBundle.LoadAsset<SceneSetCatalog>("AddOnSceneSetCatalog");
ICKX.Apron.SceneSetManager.AddCatalog (addonCatalog);
```

SceneをAssetBundleに含めて配信したい場合に、SceneSetCatalogもAssetBundleに含めて動的に更新するなどの利用が可能です。

## SceneInfoの登録と設定
SceneSetを登録する前に、個々のSceneの追加情報のSceneInfoを設定する必要があります。

Sceneアセットを選択すると拡張されたInspectorに"Select Catalog"メニューが追加されています。
ここで任意のSceneSetCatalogを選択し、そのCatalogにSceneInfoが追加されます。

![SceneSetCatalogの選択](wiki/Image/Apron_Doc_HowToUse01.PNG)

Catalogを選択すると、SceneInfoを設定するフィールドが表示されるので、SceneTypeを以下の3つから選択してください。
SceneSet単位でロード/アンロードする際に必要な情報になります。

![SceneInfoの設定](wiki/Image/Apron_Doc_HowToUse02.PNG)

| SceneType | 説明 |
----|---- 
| Dynamic | 一般的なSceneです |
| Static | Staticなオブジェクトだけで構成されたSceneです 地形などが該当します |
| Permanent | アプリの起動中ずっと破棄しないSceneです |

また、**IsBuild** のチェックボックスをONにし、UpdateBuildSettingsボタンを押すことで、BuildSettingsにチェックしたシーンを登録が可能です。

## SceneSetの登録と設定
**Create SceneSet** ボタンを押すことでSceneSetを登録できます。このボタンを押したときにSceneの名前がSceneSetの名前になります。

![SceneSetの設定](wiki/Image/Apron_Doc_HowToUse03.PNG)

SceneNamesに同時に展開したいシーンを登録していきます。
Scene名を文字で入力することもできますが、Sceneアセットをドラッグすることでも登録できます。

ロードする順番は上から順番に行われ、Scene0に登録されたシーンはActiveSceneとして扱われます。

## EditorでのSceneSetのロード
**Open Scene** ボタンを押すことで、SceneSetに登録されたSceneNamesのシーンをすべてHierarchyに展開できます。

## RuntimeでのSceneSetのロード
まずは`SceneSetManager.Initialize ();`で初期化を行ってください。

`SceneSetManager.LoadSceneSetAsync` メソッドを利用することで複数シーンのロード/アンロードを実現できます。
Unity標準のSceneManager.LoadSceneAsyncと同じように、非同期でのシーン読み込みと`allowSceneActivation`変数によるActive化を待つことが可能です。

```
public string m_loadSceneSet = "Mission01Phase02";
public LoadSceneSetMode mode = LoadSceneSetMode.Diff;
private LoadSceneSetAsyncOperation loadOp = null;

private void Awake () {
    SceneSetManager.Initialize ();
}

private void OnGUI () {
    if(loadOp == null) {
        if (GUILayout.Button ("Load SceneSet")) {
            loadOp = SceneSetManager.LoadSceneSetAsync (m_loadSceneSet, mode);
            loadOp.allowSceneActivation = false;　//ロード完了してもすぐActivationしない
        }
    }else {
        //readyForActivation==trueならActivation準備完了
        GUILayout.Label ($"readyForActivation={loadOp.readyForActivation}, Progress={loadOp.progress}");

        if (GUILayout.Button("allowSceneActivation")) {
            loadOp.UnloadAllAsync ();　//不要なシーンをUnload
            loadOp.allowSceneActivation = true;
        } 
    }
}
```
SceneManagerと異なる点としては`UnloadAllAsync`メソッドが追加されています。

SceneSetManagerではLoadSceneSetModeに応じて、現在読み込まれている複数のSceneとSceneSetに登録された複数のSceneを比較して、
* 共通で利用するシーンはそのまま
* 必要なシーンをロード
* 不要なシーンをアンロード

といった一連の処理を実行することができます。

LoadSceneSetModeの一覧

| LoadSceneSetMode | 機能 |
|:---|:---|
| Clean | SceneをすべてUnloadし、指定したSceneSetをすべてLoadします |
| CleanDynamic | SceneTypeがDynamicのSceneはすべてUnloadし、指定したSceneSetをすべてLoadします |
| Diff | 指定したSceneSetにはないが、現在読み込まれているSceneをUnloadし、現在読み込まれているSceneにはないが、指定したSceneSetにはあるSceneをLoadする |
| Add | 現在読み込まれているSceneにはないが、指定したSceneSetにはあるSceneをLoadするのみでUnloadはしない |

**(SceneTypeがPermanentのSceneはUnloadされず、2重にロードされることはない)**

### 動作例

例えば、LoadSceneSetMode.Diff を指定した場合、このような動作になる。

| 現在のScene | SceneSet | 動作 | 実行後 |
|:---:|:---:|:---:|:---:| 
| Permanent01 | Permanent01 | そのまま | Permanent01 |
| LandScape01 |  | Unload |  |
| LandScape02 | LandScape02 | そのまま | LandScape02 |
|  | LandScape03 | Load | LandScape03 |
| Mission01Phase01 |  | Unload |  |
|  | Mission01Phase02 | Load | Mission01Phase02 |


LoadSceneSetMode.Add を指定した場合、このような動作になる。

| 現在のScene | SceneSet | 動作 | 実行後 |
|:---:|:---:|:---:|:---:| 
| Permanent01 | Permanent01 | そのまま | Permanent01 |
| LandScape01 |  | そのまま | LandScape01 |
| LandScape02 | LandScape02 | そのまま | LandScape02 |
|  | LandScape03 | Load | LandScape03 |
| Mission01Phase01 |  | そのまま | Mission01Phase01 |
|  | Mission01Phase02 | Load | Mission01Phase02 |



