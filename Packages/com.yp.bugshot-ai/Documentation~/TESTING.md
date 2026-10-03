# テスト

BugShot AIのテストと検証用ツールは次の場所にあります。

```text
Packages/com.yp.bugshot-ai/Tests/Editor/
Packages/com.yp.bugshot-ai/Tools~/
```

## Unity EditModeテスト

パッケージを含む、または参照しているUnityプロジェクトから実行します。

```powershell
powershell -ExecutionPolicy Bypass -File Packages/com.yp.bugshot-ai/Tools~/RunEditModeTests.ps1
```

任意の引数：

```powershell
powershell -ExecutionPolicy Bypass -File Packages/com.yp.bugshot-ai/Tools~/RunEditModeTests.ps1 -UnityPath "<Unity.exe>" -ProjectPath "<project>"
```

出力：

```text
TestResults/BugShotAI_EditMode.xml
Logs/BugShotAI_EditMode.log
```

スクリプトが実行する処理：

```text
YP.BugShotAI.Tests.BugShotAICommandLineTestRunner.RunEditModeTests
```

## 実動作を含む検証

実行：

```powershell
powershell -ExecutionPolicy Bypass -File Packages/com.yp.bugshot-ai/Tools~/RunSubmissionValidation.ps1
```

任意の引数：

```powershell
powershell -ExecutionPolicy Bypass -File Packages/com.yp.bugshot-ai/Tools~/RunSubmissionValidation.ps1 -UnityPath "<Unity.exe>" -ProjectPath "<project>"
```

出力：

```text
Logs/BugShotAI_SubmissionValidation_RunAll.json
Logs/BugShotAI_SubmissionValidation_RunAll.md
Logs/BugShotAI_SubmissionValidation_PersistencePhase1.json
Logs/BugShotAI_SubmissionValidation_PersistencePhase2.json
```

スクリプトが実行する処理：

```text
YP.BugShotAI.Tests.BugShotAISubmissionValidation.RunAll
YP.BugShotAI.Tests.BugShotAISubmissionValidation.PersistencePhase1
YP.BugShotAI.Tests.BugShotAISubmissionValidation.PersistencePhase2
```

Unityの終了コードが0以外、結果ファイルがない、または結果JSONに`failedCount > 0`が含まれる場合は、0以外の終了コードを返します。

## プレイヤー向けWindowsビルドの確認

実行：

```powershell
powershell -ExecutionPolicy Bypass -File Packages/com.yp.bugshot-ai/Tools~/RunWindowsPlayerBuildSmoke.ps1
```

任意の引数：

```powershell
powershell -ExecutionPolicy Bypass -File Packages/com.yp.bugshot-ai/Tools~/RunWindowsPlayerBuildSmoke.ps1 -UnityPath "<Unity.exe>" -ProjectPath "<project>"
```

出力：

```text
Builds/BugShotAIPlayerSmoke/<timestamp>/BugShotAIPlayerSmoke.exe
Logs/BugShotAI_player_build_smoke.log
```

有効なビルド対象シーンがない場合は、一時的なシーンアセットを作成し、ビルド後に削除します。

## Unity EditModeテストの対象

- Windowsのユーザーパスをマスク
- macOSのユーザーパスをマスク
- Linuxのホームパスをマスク
- UNCパスをマスク
- Unicodeを含むユーザー名をマスク
- メールアドレスをマスク
- Authorization、Bearer、GitHub Token、API Keyに見える値をマスク
- 大文字・小文字が異なるAuthorizationをマスク
- 同じ行にある複数の機密情報をマスク
- URL Queryに含まれる機密情報をマスク
- URL Fragmentに含まれる機密情報をマスク
- 設定に応じてIPアドレスをマスク
- 空、Null、非常に長い入力を処理
- Stack Traceをマスク
- JSON出力前にレポート内の文字列を再帰的にマスク
- Markdownをマスク
- Promptをマスク
- 長いLogを上限で切り詰め
- File名を安全な文字列へ変換
- Fingerprintが同じ入力から同じ値になることを確認
- 空のStack Traceを処理
- 重複を抑制し、発生回数を記録
- JSONを生成・解析
- Markdownを生成
- 日本語Promptを生成
- 英語Promptを生成
- 保存数による削除対象を選択
- 保存容量による削除対象を選択
- 保存先Folderが存在しない場合にレポートを保存
- 出力先RootがFileだった場合に安全に失敗

## 実動作を含む検証の対象

- Settingsの初期値読み込みと検証
- 自動収集設定の動作
- Recorder経由で`Debug.LogError`を収集
- `Debug.LogException`経由で`NullReferenceException`を収集
- Report IDとFingerprintを生成
- `report.json`、`report.md`、`prompt_ja.txt`、`prompt_en.txt`を生成
- 生成したPrompt／ReportへPrivacy Sanitizerを適用
- 重複Reportの保存を抑制
- 重複発生回数を記録
- Report履歴を読み込み
- Reportを削除
- 壊れた`report.json`を読み飛ばす
- Report件数上限に応じて整理
- 保存容量上限に応じて削除対象を選択
- 出力Folderを作成
- 不正な出力先Rootで安全に失敗
- Domain Reloadを再現した場合にCallbackの重複を防止
- Editor WindowのMenu登録
- Demo Sampleの分離とTrigger Label
- batchmodeでスクリーンショットを取得できなくてもReport生成を継続
- 新しいUnity ProcessへSettingsを引き継ぐ
- 新しいUnity ProcessでReport履歴を読み込む

## 最新のGit URL導入検証（2026-09-29 / 2026-10-03）

Unity `6000.4.6f1` / Windowsで `-createProject` を使って新規プロジェクトを作成し、ローカル `file:` 参照ではなく次のURLを `Packages/manifest.json` のdependenciesに追加しました。

```text
https://github.com/ypkabu/bugshot-ai-unity.git?path=Packages/com.yp.bugshot-ai
```

解決されたGit revisionは `1fbae142fc69cb8a359af2f4011bd723c22c4711`、packageは `0.2.0` です。revision指定なしURLは将来mainの更新を解決するため、ここでは検証対象commitも併記します。

| 確認 | 結果 |
| --- | --- |
| Git package解決・Runtime / Editorコンパイル | 成功、Unity終了コード0 |
| EditMode | 30成功 / 0失敗 |
| SubmissionValidation.RunAll | 27成功 / 0失敗 |
| 再起動をまたぐPersistencePhase1 / Phase2 | 2成功 / 0失敗、3成功 / 0失敗 |
| Basic Setup実Import | Package Manager Sample APIで成功、両Demo scriptの配置を確認 |
| Import後のDemoコンパイル・Report生成（10月3日） | 成功、Unity終了コード0。JSON / Markdown / 日英Promptを確認 |

10月3日の確認はImport済み `BugShotAIDemoErrorPanel` の実際の `TriggerLogError` をbatchmodeで呼び、Recorderの `Application.logMessageReceived` から保存する経路を検証しました。期待するデモErrorと直前イベントがJSONに含まれます。スクリーンショットを無効にした検証で、GUIボタン・clipboard・通常Game Viewの撮影を再確認したという意味ではありません。意図的な `Debug.LogError` のログは、コンパイルエラーと区別しています。今回Windows Playerビルドは実施していません。

### 通常EditorのGUI・画像・clipboard（2026-10-03）

上のbatchmode確認に続き、同じGit導入済みConsumerを通常Editor / Play Modeで開き、Dark themeで実際にボタンを操作しました。Camera・Cube・Floorを置いた検証用Sceneに、Import済みの `BugShotAIDemoErrorPanel` を配置しています。検証用Scene・設定はConsumer内だけの変更です。

| 操作・確認 | 結果 |
| --- | --- |
| ToolsからWindowを開く | Recorder missingを表示 |
| Create Recorder In Scene | Recorderを1つ作成しReadyへ遷移 |
| Trigger Test Error | 新Reportを保存し、詳細を自動選択 |
| SampleのDebug.LogError / NullReferenceExceptionをクリック | Error / ExceptionのJSON・Markdown・日英Prompt・PNGを保存 |
| Sampleの直前イベント | `Clicked Debug.LogError button` をJSONで確認 |
| WindowのScreenshot欄 | 保存PNGのプレビューを表示 |
| PNG内容 | 1080×1920と1920×1080、Game ViewのSampleパネルと3D Sceneを目視確認、空画像ではない |
| Copy Markdown / Copy Prompt EN / Copy Prompt JP | 実際のclipboardを読み、対応する保存ファイルと全文一致 |
| Copy JSON Path | 区切り文字を正規化すると保存先と一致、実在するJSONへ解決 |

3件とも `screenshotError` は空でした。画像内にユーザー名・個人パス・メール・通知は見当たりませんでしたが、これはこの検証Sceneの確認であり、すべてのSceneの匿名性を保証するものではありません。最初のコピー対象はWindowのTest Errorです。

Light theme、全Sampleボタン、外部アプリへの貼り付け、今回のWindows Playerビルドは未検証です。データ削除ボタンは操作していません。

### パッケージテストを新規プロジェクトで実行する準備

通常の導入には不要ですが、パッケージのテストを実行する検証用プロジェクトでは `com.unity.test-framework`（今回1.6.0）をdependenciesに追加し、manifestのトップレベルに次を追加します。

```json
"testables": ["com.yp.bugshot-ai"]
```

既存dependenciesは削除せず、JSONに必要な区切りカンマを保ってください。最初の空プロジェクトではTest Frameworkなしでtestablesを追加するとNUnit参照が解決できませんでした。Test Frameworkとtestablesを揃えた後、コンパイルと上記テストが成功しています。`-runTests` やカスタムrunnerの開始直後に `-quit` を付けると結果保存前に終了し得るため、提供スクリプトの終了制御を使います。

結果ファイルは、この文書前半の `TestResults/` と `Logs/` の指定先に保存しました。端末固有のログや実ReportはGit管理せず、確認項目・対象revision・集計をここに残します。

## 開発時のローカル結果（最新のGit URL検証とは別）

最終実行：2026-08-02

環境：

```text
Unity 6000.4.6f1
Windows Editor
Package com.yp.bugshot-ai 0.2.0
```

元のプロジェクト：

```text
batchmodeのコンパイル：成功
EditModeテスト：30成功 / 0失敗
`SubmissionValidation.RunAll`：27成功 / 0失敗
再起動をまたぐ永続化の前半：2成功 / 0失敗
再起動をまたぐ永続化の後半：3成功 / 0失敗
```

クリーンな検証用プロジェクト：

```text
Unityの`-createProject`でProjectを作成
Local Package参照：file:<absolute-path-to-repo>/Packages/com.yp.bugshot-ai
Packageの解決とコンパイル：成功
Basic Setup SampleのImport相当コンパイル：成功
EditModeテスト：30成功 / 0失敗
`SubmissionValidation.RunAll`：27成功 / 0失敗
再起動をまたぐ永続化の前半：2成功 / 0失敗
再起動をまたぐ永続化の後半：3成功 / 0失敗
Windows Playerのビルド確認：成功
```

Unity 2022.3 LTS：未検証。

## スクリーンショットについて

batchmodeには表示中のGame Viewがありません。現在の検証では、Play Mode外の`ScreenCapture.CaptureScreenshotAsTexture()`がTextureを返さない場合でも、Recorderが`screenshotError`付きのレポートを保存することを確認しました。

通常のEditorで取得するスクリーンショットは、表示中のGame Viewを目視する必要があります。

## 手動確認が必要な項目

短い対話確認には[動作確認項目](QA_CHECKLIST.md)を使用します。GUI配置、スクリーンショット内容、clipboard動作、明暗themeでの読みやすさは、通常のEditor sessionで確認します。

Unity 2022.3 LTSを検証済みと記載するには、コンパイル、テスト、実動作検証、Playerビルドの一式を行う必要があります。
