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

## 最新のローカル結果

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
