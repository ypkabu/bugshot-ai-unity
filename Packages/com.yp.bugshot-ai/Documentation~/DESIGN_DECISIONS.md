# 設計判断

初期実装と検証を通して確定した判断を記録します。今後の機能予定ではありません。

## Recorderを1つにまとめてUnityと接続する

最初のMVPでは、収集、文章生成、保存を`BugShotAIRecorder`が担当していました。短期間でデモを作れる一方、プライバシー処理や重複判定をコンポーネントなしでテストできませんでした。

現在のRecorderは、callback、FPS、Player Transform、スクリーンショット要求、処理順の調整というUnityのライフサイクルに関わる部分だけを担当します。決定的にテストできる規則や、異なる失敗条件を持つ処理は外へ分離しました。

## 出力形式を1クラスへまとめる

JSON、Markdown、Prompt生成は同じレポートを受け取って文章を返すため、`BugShotAIReportFormatter`へまとめました。ファイル作成、旧形式の読み込み、削除、容量整理は文字列生成と失敗条件が異なるため、保存処理として分離しています。

## レポート作成時のsnapshotをBuilderで収集する

`BugShotAIReportBuilder`は1つのレポートを作る際に、現在のシーン、`Application`、`SystemInfo`を読み取ります。マスク、文章生成、保存は行いません。代替実装や独立した復旧規則がない短いUnity情報収集処理のため、別interfaceを増やさずレポート作成と同じ場所に置いています。

## 書き込み前にマスクする

Unityログにはユーザーディレクトリ、メールアドレス、Authorization値、トークンに見える代入が含まれることがあります。JSON、Markdown、Promptを書き出す前にレポートモデルをマスクします。Editorの確認表示でも再度マスクし、編集済みデータや旧形式のデータを自動的に安全とは扱いません。

この処理は文字列パターンに基づくため、完全な保証ではありません。スクリーンショットとプロジェクト固有名は目視確認が必要です。

## 同じエラーをfingerprintで判定する

初期版にはcooldownがなく、毎フレーム出るログが多数のJSONとPNGを作る可能性がありました。

fingerprintにはログ種別、condition、有効なstack traceの1行を使います。時刻、シーン、FPS、プレイヤー位置は変化するため除外します。抑制した回数はメモリ上で加算しますが、cooldown中は最初のJSONを書き換えません。検証では、保存フォルダー数とtrackerの回数を別々に確認します。

## スクリーンショット取得に失敗してもレポートを残す

batchmodeでは`ScreenCapture.CaptureScreenshotAsTexture()`がTextureを返さないことがあります。スクリーンショットは補助情報のため、取得処理はbyte列またはエラー文字列を返し、保存処理は残りのファイルと`screenshotError`を記録します。

## 外部へ自動送信しない

GitHubやAIサービスへの自動送信は、トークン保存、scope管理、ネットワークエラー、未確認データ送信の危険を伴います。現在はローカル保存と明示的なコピー操作までに限定しています。

## 実行用とEditor用のAssemblyを分ける

Editor Windowと設定UIはEditor専用です。Recorderとレポート処理はPlay Modeで使いやすく、公開操作履歴APIを`UnityEditor`へ依存させないためRuntimeに置いています。

その結果、Runtime AssemblyはPlayerビルドにも含まれます。Unity 6000.4.6f1のWindows Playerビルド確認は成功しています。将来Editor専用へ移す場合は、asmdefの整理ではなくAPI変更として判断します。

## 検証済みバージョンを正確に記載する

Windows上のUnity 6000.4.6f1で、コンパイル、テスト、クリーンなUPMプロジェクトでの検証、Playerビルドを確認しました。Unity 2022.3 LTSでは同じ確認を実施していないため、READMEでも検証済みとは記載しません。
