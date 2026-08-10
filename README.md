# BugShot AI for Unity

BugShot AIは、Unity Editor上でエラーや例外が発生したときに、ログ、シーン情報、直前のイベント、任意のスクリーンショットをまとめてレポートとして保存する、ローカル動作のEditor拡張です。

バグの検出や修正は行わず、レポートを外部へ送信することもありません。デバッグやGitHub Issueの作成に役立つ情報を、手元に残すことを目的としています。

## 関連資料

- [パッケージの説明・デモ](Packages/com.yp.bugshot-ai/README.md)
- [設計資料](Packages/com.yp.bugshot-ai/Documentation~/ARCHITECTURE.md)
- [テスト](Packages/com.yp.bugshot-ai/Documentation~/TESTING.md)
- [セキュリティ・プライバシー](Packages/com.yp.bugshot-ai/Documentation~/SECURITY_AND_PRIVACY.md)

インストール可能なUPMパッケージは`Packages/com.yp.bugshot-ai/`にあります。

## 検証状況

- Windows Editor上のUnity `6000.4.6f1`で検証済み
- Unity `2022.3 LTS`は未検証
- RuntimeとEditorのAssemblyを分離
- Runtimeコードから`UnityEditor`への参照なし
