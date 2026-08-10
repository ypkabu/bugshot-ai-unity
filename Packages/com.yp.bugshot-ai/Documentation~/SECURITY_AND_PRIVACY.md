# セキュリティとプライバシー

BugShot AIは、有用なデバッグ情報を共有する際に、ローカルパスや機密情報を誤って含める危険を減らすよう設計しています。

## マスクする情報

- `C:\Users\name`のようなWindowsユーザーディレクトリ
- `/Users/name`のようなmacOSユーザーディレクトリ
- `/home/name`のようなLinuxホームディレクトリ
- `\\BuildShare\Users\name`のようなUNCユーザーパス
- 現在のUnityプロジェクトの絶対パス
- メールアドレス
- `Authorization` header
- `Bearer` token
- GitHub token形式の文字列
- API key、access token、client secret、secret、tokenの代入
- `token`、`key`、`secret`、`api_key`、`access_token`、`client_secret`という名前のURL query／fragment値
- 設定で有効にした場合のIPアドレス

具体的なプロジェクトパスとホームパスを、一般的なパスパターンより先に置き換えます。これにより、可能な場合はすべてを`<USER_HOME>`にせず`<PROJECT_ROOT>`を残します。

テスト用データの例：

```text
変更前: C:\Users\alice\Project\Assets\Test.cs
変更後: <USER_HOME>\Project\Assets\Test.cs

変更前: Authorization: Bearer demo-token
変更後: Authorization: <REDACTED>
```

## 出力方針

BugShot AIは、マスク済みのレポートを保存します。JSON、Markdown、Promptは、マスク後のレポートモデルから生成します。

Editor Windowは初期状態で安全なパス表示を使用します。開発者がローカル作業で必要とする場合に限り、明示的なコピー操作から完全なパスを取得できます。

## 外部送信を行わない

パッケージはレポートを外部サービスへ送信しません。GitHub Issue APIへの投稿と外部AI API呼び出しは実装していません。これにより、Unity Editor内でのトークン保存と、未確認のデバッグ情報を外部へ送る危険を避けています。

## 残る危険

- stack traceやユーザー入力には、機密情報ではなくてもプロジェクト固有の名前が含まれることがあります
- スクリーンショットには、ゲーム画面やデスクトップ上の情報が映る可能性があります
- ローカルIPはネットワーク調査に役立つことがあるため、IPマスクは任意です
- 出力先にはユーザーが選択した任意の場所を指定できますが、パッケージがそこからuploadすることはありません
- 広いパターンは問題のない文字列もマスクする場合があります。共有用レポートでは、credentialらしい値を残すより誤検出を優先します

## デモ公開前の確認

- スクリーンショットや動画には、Editor Windowの安全なパス表示を使用します
- `report.json`、`report.md`、Promptを公開前に確認します
- 非公開プロジェクトの生レポートは、スクリーンショットとユーザー入力を確認せず公開しません
- 公開READMEの例には`Documentation~/ExampleReport/`を使用します
