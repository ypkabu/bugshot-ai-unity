# 動作確認項目

パッケージ更新の公開前やデモ収録前に行う短い確認です。

## 自動確認

- [ ] Unity batchmodeのコンパイルが終了コード0で完了する
- [ ] EditModeテストが成功する
- [ ] `SubmissionValidation`と再起動をまたぐ2段階の検証が成功する
- [ ] Windows Playerのビルド確認が成功する
- [ ] READMEのリンクが解決できる
- [ ] パッケージに対する`git diff --check`が成功する
- [ ] 公開ファイルに実在するユーザーパス、メールアドレス、token、検証用プロジェクトパスがない

コマンドと出力ファイルは[テスト](TESTING.md)に記載しています。

## Unity Editor上の確認

- [ ] `Tools > BugShot AI > Open Window`がConsole Errorなしで開く
- [ ] Unity Personal／Proの両themeでWindowを読める
- [ ] `Create Recorder In Scene`でRecorderを1つ作成し、2回目は既存Recorderを選択する
- [ ] Play Modeのテスト用Errorでレポートフォルダーが1つ作られる
- [ ] Error、実行環境、スクリーンショット、再現情報、プライバシー、出力の各欄を操作できる
- [ ] スクリーンショットが空ではなく、公開できない情報を含まない
- [ ] JSON、Markdown、英語／日本語Promptがマスク済みである
- [ ] Duplicate Error Burstがログごとにフォルダーを作らない
- [ ] 完全なパスは明示的なコピー／open操作の後だけ表示される

## デモ

- [ ] Basic Setupサンプルをimportし、任意のテストシーンへ`BugShotAIDemoBugTrigger`または`BugShotAIDemoErrorPanel`を追加する
- [ ] `BugShotAIDemoBugTrigger`では`D -> LeftShift -> Space -> B`を実行し、`BugShotAIDemoErrorPanel`では`Debug.LogError`をクリックする
- [ ] デモ用Errorと直前のイベント列を確認する
- [ ] 生成したレポート、スクリーンショット、マスク表示、コピーしたMarkdownまたはPromptを確認する
- [ ] 収録した全フレームについて、個人パス、アカウント名、通知、メール、token、IPアドレスを確認する

確認環境：Unity 6000.4.6f1 / Windows Editor。Unity 2022.3 LTSは未検証です。
