# 更新履歴

## 未公開

## 0.2.0 - 2026-08-09

- 設定、プライバシーマスク、fingerprint、表示形式、レポート保存をRecorderから分離し、それぞれに独立したテストまたは失敗境界を設定
- `BugShotReports/<report-id>/`以下へ、1レポート1フォルダーのマスク済み出力を追加
- report ID、fingerprint、発生回数、Editor状態、ユーザーメモ、直近のConsoleログを追加
- `ProjectSettings/BugShotAISettings.json`と`Project > BugShot AI`からプロジェクト設定を利用可能に変更
- Editor Windowへレポート履歴、詳細、スクリーンショット確認、メモ編集、Markdownコピー、削除を追加
- 最大レポート数と概算フォルダー容量による保存上限を追加
- プライバシーマスク、fingerprint、重複抑制、JSON、Markdown、Prompt、保存方針、ファイル名、保存エラーのEditModeテストを追加
- NullReferenceException、IndexOutOfRangeException、Debug.LogError、長いstack trace、重複発生を確認するDemo Error Panelを追加
- 再利用可能なEditModeコマンドラインrunnerと、固定XML出力を持つWindows PowerShellスクリプトを追加
- 収集、保存、プライバシー、重複、スクリーンショット失敗、再起動後の永続化を確認するコマンドライン検証を追加
- Windows Playerのビルド確認用コマンドを追加
- Linuxホームパス、UNCパス、Unicodeユーザー名、URL fragment内secret、入れ子出力のマスクを追加
- 短い動作確認項目と、生成可能な公開レポート例を追加
- 現在のUIとレポート形式に合わないMVP時点の画像、動画、デモ文章を削除
- package manifestの最小バージョンをUnity 6000.4に設定。Unity 6000.4.6f1 / Windows Editorで検証し、Unity 2022.3 LTSは未検証
- JSON、Markdown、Prompt生成を`BugShotAIReportFormatter`へ統合し、実行環境の収集をreport builderへ移動
- 標準Unity IMGUI styleを使い、記録状態、履歴、詳細を中心とするEditor Window構成へ整理
- 未使用の出力言語設定と自動コピー設定を削除
- 公開資料を、元の課題、初期版の問題、設計判断、検証結果が分かる構成へ整理
- レポート一覧へ時刻、標準IMGUI詳細表示へStack Traceを追加
- プライバシー処理順、fingerprint、重複cooldown、スクリーンショット失敗、破損レポート処理へ理由コメントを追加
- Play Modeのスクリーンショット取得をフレーム描画後まで待ち、同時要求を直列化
- 待機中のスクリーンショット要求数を制限し、上限超過時も省略理由付きレポートを保存
- テスト用Errorのレポート監視を、成功、timeout、Window終了、別テスト開始時に停止
- PowerShell検証スクリプトがUnity終了後に結果を判定するよう変更
- 公開README向けに、Editor Window、生成レポート、プライバシー確認、短いデモ動画を追加

## 0.1.0

- `BugShotAIRecorder` Runtime componentを追加
- `BugShotAIEventLogger` Runtime操作履歴loggerを追加
- Unityの`Error`、`Exception`、`Assert`を収集
- `Application.persistentDataPath/BugShotAI/`へJSONとPNGを保存
- シーン名、シーンパス、FPS、任意のプレイヤー位置、実行環境、スクリーンショット名、直前のイベントを保存
- `Tools > BugShot AI > Open Window`を追加
- レポートを開く、Recorder作成、テスト用Error発生、最新JSONのopen／copy、GitHub Issue用Promptのcopy／saveを追加
- 日本語／英語のGitHub Issue用Prompt生成を追加
- 床抜けデモを含むBasic Setupサンプルを追加
- Runtime用とEditor用のasmdefを分離
- Unity 6000.4.6f1 / Windows Editorで検証
- Unity 2022.3 LTSは未検証
