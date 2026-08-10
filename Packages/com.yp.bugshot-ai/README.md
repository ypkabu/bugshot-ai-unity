# BugShot AI for Unity

BugShot AIは、UnityでErrorやExceptionが発生した際の状況を保存し、開発者がGitHub Issueを作成したり、AIツールへ相談したりする前に内容を確認できるEditor拡張です。

バグを自動で検出・修正するものではなく、レポートを外部へ自動送信することもありません。

## 解決したかった課題

Unityで不具合を再現しても、Consoleのメッセージだけでは、開いていたシーン、エラー直前の操作、実行環境、Game Viewの状態などが残らないことがありました。また、stack traceをそのまま共有すると、ローカルのユーザーパスやトークンに見える値まで含まれる可能性があります。

このパッケージは、Unityがエラーを記録した時点で周辺情報を収集し、代表的な機密情報をマスクしたうえで、開発者が確認できるレポートとして保存します。

## デモ

Basic Setupサンプルには、任意のシーンで収集処理を確認するためのスクリプトが2つあります。

1. Basic Setupサンプルをインポートします。
2. シーン内のGameObjectへ`BugShotAIDemoBugTrigger`または`BugShotAIDemoErrorPanel`を追加します。
3. `Tools > BugShot AI > Open Window`を開き、シーンにRecorderがなければ作成します。
4. Play Modeに入ります。
5. `BugShotAIDemoBugTrigger`では`D -> LeftShift -> Space -> B`を順に押します。`BugShotAIDemoErrorPanel`では`Debug.LogError`をクリックします。
6. Unityがデモ用Errorを記録し、レポートを作成します。
7. 新しいレポートを選択し、Error、スクリーンショット、直前のイベント、プライバシー確認、Markdown、Promptを確認します。

[56秒のデモ動画](Documentation~/demo/bugshot-ai-demo.mp4)（1920x1080、音声なし）

![レポート履歴と詳細を表示するBugShot AI Editor Window](Documentation~/images/bugshot-main-window.png)

![パスをマスクし、再現情報をまとめたレポート](Documentation~/images/generated-report.png)

![プライバシー処理の適用前・適用後](Documentation~/images/privacy-preview.png)

[Documentation~/ExampleReport/](Documentation~/ExampleReport/)には、機密情報を含まない再現可能な出力例があります。

## 初期版からの改善

最初のMVPでは収集処理は完成していましたが、次の問題がありました。

- `BugShotAIRecorder`がスクリーンショット要求、JSON生成、ファイル保存まで担当していた
- Promptの文章をEditor Window内で生成していたため、UIなしではテストしにくかった
- 同じエラーが繰り返されると、多数のJSONとPNGが作成される可能性があった
- プライバシー処理はPrompt作成時のレポートパス置換に限られていた
- batchmodeではスクリーンショット用Textureを取得できなかった

現在はRecorderをUnity callbackの調整役に限定し、決定的に処理できる部分や失敗し得る部分を個別のクラスへ分離しています。

- 1レポートを固定ファイル名の1フォルダーへ保存
- 表示形式の生成をEditor Windowから分離
- fingerprintのcooldownで、同じエラーによるスクリーンショット取得とディスク書き込みを抑制
- JSON、Markdown、Promptを書き出す前にレポート全体をマスク
- スクリーンショット取得に失敗しても、理由をデータとして記録し、テキストのレポートは保存

## インストールと初期設定

Unity Package Managerから次のGit URLを指定します。

```text
https://github.com/ypkabu/bugshot-ai-unity.git?path=Packages/com.yp.bugshot-ai
```

package manifestが要求する最小バージョンはUnity 6000.4です。

1. `Tools > BugShot AI > Open Window`を開きます。
2. `Create Recorder In Scene`をクリックします。
3. 必要に応じてRecorderの`Player Transform`を設定します。
4. `Project Settings > BugShot AI`で収集、プライバシー、保存数を設定します。
5. Play ModeでErrorまたはExceptionを発生させます。
6. レポートを選択し、コピーや共有の前に内容を確認します。

## 主な機能

- 設定したUnityの`Error`、`Exception`、`Assert`、`Warning`を収集
- シーン情報、実行環境、FPS、任意のプレイヤー位置、操作履歴、直近のConsoleログを保存
- JSON、Markdown、日本語／英語のPrompt、任意のPNGを1つのレポートフォルダーへ出力
- Editor Windowでレポート履歴とError、実行環境、スクリーンショット、再現情報、プライバシー、出力内容を表示
- fingerprintのcooldownによる重複収集の抑制
- 設定した件数または概算容量を超えた場合、古いレポートフォルダーから削除
- Runtimeコードから`UnityEditor`への参照を分離

ゲーム側から短い操作履歴を記録できます。

```csharp
BugShotAIEventLogger.Record("Player", "Jumped near platform edge");
```

## 出力例

```text
BugShotReports/
  20260731_071500_123_ab12cd34/
    report.json
    report.md
    screenshot.png
    prompt_ja.txt
    prompt_en.txt
```

UnityがTextureを取得できなかった場合は`screenshot.png`を省略します。その場合も`report.json`へ`screenshotError`を記録し、残りのファイルは保存します。

JSONには、エラーの識別情報、シーン、ユーザー入力、任意のプレイヤー位置、実行環境、直前のイベント、直近のログが含まれます。[機密情報を除いた出力例](Documentation~/ExampleReport/)も参照できます。

## 設計上の判断

### 収集処理ではRecorderを調整役に限定

`BugShotAIRecorder`はUnity callback、FPSの計測、任意のPlayer Transform、収集順序を担当します。マスク、重複判定、表示形式、保存処理は、直接テストできる処理や異なる失敗条件を持つ処理として外へ分離しています。

### 重複収集をfingerprintのcooldownで抑制

fingerprintにはログ種別、condition、stack trace内の最初の有効な行を使用します。時刻、シーン、FPS、プレイヤー位置を含めると、同じエラーでも一致しなくなるため除外しています。

抑制した発生回数はメモリ上で数えますが、cooldown中はスクリーンショット取得とディスク書き込みを行いません。

### プライバシー処理の順序

既知のプロジェクト／ホームパス、一般的なユーザーパス、任意のメールアドレス、Authorizationやトークンに見える文字列、任意のIPアドレスの順にマスクします。具体的なパスを先に処理することで、可能な場合は`<PROJECT_ROOT>`という情報量の多い表示を残します。

### スクリーンショット失敗時の継続

Play Modeではフレームの描画終了を待ってから`ScreenCapture.CaptureScreenshotAsTexture()`を呼びます。それでもbatchmodeや有効な描画対象がない場合はTextureを取得できません。PNGは補助情報として扱い、取得に失敗してもError、stack trace、実行環境、操作履歴は保存します。

### 保存数と容量の上限

初期値は50フォルダー、概算256 MBです。上限を超えた場合は古いフォルダーから削除します。削除対象の選択規則は、実際のファイル削除とは分けてテストしています。

### 実行用とEditor用のAssemblyを分離

Play Modeでの収集と公開操作履歴APIはRuntime Assemblyに置き、WindowとProject Settings UIはRuntimeを参照するEditor専用Assemblyに置いています。Runtimeから`UnityEditor`は参照しません。

詳細は[設計](Documentation~/ARCHITECTURE.md)と[設計判断](Documentation~/DESIGN_DECISIONS.md)に記載しています。

## 採用しなかった方法

GitHub Issueへの自動投稿や外部AIへの自動送信は実装していません。これらには認証情報の保存、権限管理、ネットワークエラー処理、より明確な確認操作が必要です。また、開発者がマスク後の文章とスクリーンショットを確認する前に、プロジェクト情報を送信してしまう危険があります。

このパッケージは、ローカルファイルの作成と明示的なコピー操作までを担当します。

## プライバシー

マスク処理は文字列パターンに基づきます。現在の出力は、次のテスト用データのようになります。

変更前：

```text
C:\Users\alice\Project\Assets\Test.cs
Authorization: Bearer demo-token
```

変更後：

```text
<USER_HOME>\Project\Assets\Test.cs
Authorization: <REDACTED>
```

macOS／Linuxのホームパス、UNCユーザーパス、メールアドレス、GitHub token形式の文字列、secret代入、URL内のsecret、任意のIPアドレスにも対応しています。ただし、プロジェクト固有の名前をすべて判定することはできず、スクリーンショットは必ず目視確認が必要です。詳細は[セキュリティとプライバシー](Documentation~/SECURITY_AND_PRIVACY.md)を参照してください。

## 重複収集の抑制

`SubmissionValidation`では、60秒のcooldown中に同じ`Debug.LogError`を5回発生させ、レポートフォルダーが1つだけ作成されることを確認します。別のtracker検証で、抑制中もメモリ上の発生回数が増えることを確認します。

最初に保存したJSONは、抑制のたびに書き換えません。そのため`occurrenceCount`は、そのファイルを作成した収集時点の値です。現在のテストも、それ以上の内容は保証していません。

## 検証

EditModeテストでは、マスク、文字数制限、fingerprintの安定性、重複規則、表示形式、保存方針、保存エラー処理の30項目を確認しています。

`SubmissionValidation`は、実際のUnity callbackとファイルシステムを使用します。`RunAll`では、収集、保存、プライバシー、重複、Editor登録、サンプル、スクリーンショット失敗時の継続を含む27項目を確認します。設定とレポート状態の永続化では、最初のUnity processで2項目を保存し、別のUnity processで再起動後の3項目を検証します。

クリーンなUPM検証では、別のUnityプロジェクトを作成してローカルパッケージを解決し、同じテストと検証を実行した後、Windows Playerのビルド確認を行います。

コマンドと結果ファイルは[テスト](Documentation~/TESTING.md)、短い対話確認は[確認項目](Documentation~/QA_CHECKLIST.md)に記載しています。

最新の確認環境：

- Unity 6000.4.6f1 / Windows Editor
- パッケージのコンパイル：成功
- 元プロジェクトのEditMode：30成功 / 0失敗
- 元プロジェクトの`SubmissionValidation`：27/27、永続化 2/2・3/3
- クリーンなUPMプロジェクトのEditMode：30成功 / 0失敗
- クリーンなUPMプロジェクトの`SubmissionValidation`：27/27、永続化 2/2・3/3
- クリーンなWindows Playerビルド確認：成功

## 制限事項

- `package.json`はUnity 6000.4以降を要求し、Unity 6000.4.6f1 / Windowsで検証しています
- Unity 2022.3 LTSは未検証です
- batchmode、Play Mode外、有効な描画対象がない場合はスクリーンショット取得に失敗することがあります
- スクリーンショットがなくてもレポートは保存します
- マスクには誤検出があり、プロジェクト固有の値をすべて除去できる保証はありません
- スクリーンショット内の情報は文字列マスクでは確認できません
- 抑制した重複発生は、最初に保存したレポートの`occurrenceCount`を書き換えません
- GitHubや外部AIサービスへレポートを自動送信しません
