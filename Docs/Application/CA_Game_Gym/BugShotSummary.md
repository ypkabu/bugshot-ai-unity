# CA Game Gym応募用補助資料 - BugShot AI for Unity

BugShot AIはゲーム作品ではなく、Unityでのデバッグ作業を補助するEditor拡張です。CA Game Gym応募ではECHO//SHIFTをメイン作品とし、本作はUnity API、C#、エラー処理、テスト、Gitを使った開発経験を補足するサブ作品として扱います。

## 実装調査

以下はリポジトリ内の実コード、テスト、検証記録、Git履歴で確認した事実です。

| 観点 | 確認した実装 |
| --- | --- |
| Log callback / Error capture | `BugShotAIRecorder`が`Application.logMessageReceived`を購読し、設定対象のError、Exception、AssertをReport候補として受け取る。 |
| Screenshot | `ScreenCapture.CaptureScreenshotAsTexture()`と`EncodeToPNG()`を使用する。取得失敗時も理由を記録し、テキストReportの保存は続ける。 |
| Screenshot Queue / FIFO | Screenshotが必要なReportを`Queue<PendingCapture>`へ追加し、`Dequeue()`で受信順に処理する。上限は64件で、超過時は最古の項目をScreenshotなしで保存する。 |
| Frame timing | Capture coroutineが各項目の前に`WaitForEndOfFrame`を待ち、描画完了後にScreenshotを取得する。 |
| Report / JSON / Markdown / Prompt | Report builderがScene、Environment、FPS、Player位置、recent events、logsをまとめ、formatterとstorageがJSON、Markdown、日英Promptをローカル保存する。外部送信処理はない。 |
| Failure handling | Screenshot失敗、PNG書き込み失敗、破損Report、Settings読み込み失敗を個別に扱い、可能な範囲のReportと失敗理由を残す。 |
| Recorder lifecycle | `OnEnable`で購読を張り直し、`OnDisable`でcallback解除とpending captureのflushを行う。Play Mode終了時はEditor側のguardからflushする。 |
| Callback cleanup | Runtimeのlog/event callback、EditorのPlay Mode callback、Report待機用`EditorApplication.update` callbackに解除経路がある。再購読前にも解除して重複登録を防ぐ。 |
| Recursive logging prevention | 保存処理中の`isHandlingLog`と内部prefix `[BugShot]`の除外を併用し、内部Warningを再度Report化しない。 |
| Duplicate prevention | log type、condition、stack traceの有用な先頭行からfingerprintを作り、cooldown内の同一Errorを抑制する。発生回数は保持する。 |
| Editor Window | Report一覧と詳細、Recorder作成、テストError、Screenshot、Privacy preview、Markdown/Prompt exportを標準IMGUIで提供する。 |
| Samples | Basic Setupに操作breadcrumb付きの床抜け風Errorと、複数種類のErrorを発生させるdemo panelがある。 |
| Tests | EditMode testは30件。Privacy、fingerprint、duplicate、JSON、Markdown、Prompt、storage policy、書き込み失敗を対象にする。 |
| Validation | Submission Validationは通常検証27件とdomain reloadをまたぐpersistence検証2件+3件を持つ。Clean UPM、Windows Player Build smoke用の再現スクリプトも含む。 |
| Git history / release | 実装、sample/validation、docs/demo、releaseを4コミットに分け、PR #1をmergeした履歴がある。`v0.2.0`はmainのmerge commitを指すannotated tagである。 |

直近の記録上の検証環境はUnity 6000.4.6f1、Windows Editorです。Unity 2022.3 LTSは未検証です。GitHub ActionsによるCI実行済みとは表現せず、ローカルのcommand-line testとclean validationとして説明します。

## Game Gym応募で使う強みTOP3

1. **Unity APIとlifecycleの理解**

   `Application.logMessageReceived`、`WaitForEndOfFrame`、coroutine、Play Mode遷移、callback解除を組み合わせ、EditorとRuntimeの責務をasmdefで分離した。
2. **実際のデバッグ課題をReportへ変える設計**

   Errorだけでは再現状況が足りない課題に対し、scene、stack trace、recent events、環境情報、Screenshotを同じReportへ保存した。取得失敗時も残せる情報を失わない。
3. **壊れ方を含めた検証**

   同一Errorの連続発生、Screenshot失敗、破損Report、保存上限、callback重複、domain reload、clean UPM、Player Buildをテストまたはvalidation対象にした。

## 一言説明

Unityのエラー発生時にログ・画面・直前操作を保存し、原因整理を助けるEditor拡張です。

## 100字説明

UnityのErrorやException発生時に、ログ、スタックトレース、シーン、直前操作、任意の画面をローカル保存するEditor拡張です。調査材料を一つのReportにまとめ、Issue作成前の整理を助けます。

## 200字説明

BugShot AIは、UnityのErrorやException発生時を後から確認するデバッグ補助ツールです。ログ、stack trace、Scene、FPS、環境、直前操作、Screenshotを一つのReportへ保存します。Editor Windowで内容を確認し、JSON、Markdown、GitHub Issue用Promptを書き出せます。原因を自動特定せず、再現時の情報不足を減らすローカルツールです。

## 技術的に工夫した点 200字

Error callbackでは撮影要求を64件上限のFIFO Queueへ入れ、coroutineで`WaitForEndOfFrame`後に取得します。Errorが重なっても受信順を保ち、超過やPlay Mode終了時はScreenshotなしのReportを残します。保存中flagと内部prefixで再帰記録を防ぎ、`OnDisable`とPlay Mode遷移でcallbackを解除しました。

## 課題解決経験 300字

ConsoleのErrorだけでは直前操作、画面、Scene、環境が分からず、再現に時間がかかりました。初期版はRecorderに処理が集中し、同一Errorの連続保存やScreenshot失敗も課題でした。そこでPrivacy処理、重複判定、保存、表示を分け、recent eventsとfingerprintを追加しました。Screenshotはframe timingのためQueue化し、取得不能でもJSONとMarkdownを残します。callback重複、domain reload、破損Report、保存上限をtestとSubmission Validationで確認し、検証手順もscript化しました。

## Unity経験として説明する200字

Application.logMessageReceivedでErrorを受け、OnEnableとOnDisableで購読を管理しました。ScreenshotはWaitForEndOfFrame後に取得し、Play Mode終了時は待機分を保存します。EditorWindowを使い、Runtime asmdefからUnityEditorを分離しました。Unity Test Frameworkとclean UPMで検証しました。

## Git経験として使える事実

- Runtime/Editor/Tests、sample/validation、documentation/demo、releaseを責務別の4コミットへ分割した。
- release branchからPR #1を作成し、4コミットを保持するmerge commitでmainへ統合した。
- Package変更とUnity project側の既存dirtyを分け、公開対象だけをcommitした。
- `package.json`と`CHANGELOG.md`のrelease確定を専用commitに分けた。
- mainのmerge commitへannotated tag `v0.2.0`を付けた。
- 自動CIの実績とは書かず、ローカルtest runner、clean validation、build smokeの結果として区別する。

## ECHO//SHIFTで示すもの

- Top-down 3D time-loop puzzleとしてのゲーム制作
- PlayerとEchoの入力記録・再生を中心としたGame Client実装
- 移動、interaction、battery、door、goalなどのGameplay
- section進行とruntime lifecycleを含むUnity runtime開発

## BugShot AIで補強するもの

- EditorWindow、SettingsProvider、Play Mode callbackを使うUnity Editor拡張
- Error capture、frame timing、Queueを扱うdebugging tool開発
- failure handling、recursive logging prevention、duplicate preventionによるreliability
- EditMode test、Submission Validation、clean UPM、Player Build smoke
- commit分割、PR、release tagを含むdevelopment workflow

この役割分担は実装内容と一致します。応募ではECHO//SHIFTを先に説明し、BugShot AIは「ゲーム制作を支える側のUnity理解」を示す補助リンクとして扱います。

## 採用担当目線レビュー

### BugShot AIをGame Gym応募で載せるメリット

Gameplay以外にも、Unityのframe timing、Editor API、lifecycle、失敗時の設計、テストまで考えたことを具体的なコードで示せます。ECHO//SHIFT単体では見えにくい、開発環境を自分で改善する姿勢の補強になります。

### 載せるデメリット

名前にAIを含むため、AI機能が主役の大きな製品、または生成AIへ全面依存した実装と誤解される可能性があります。また、説明量を増やしすぎるとメインのゲーム作品が埋もれます。応募フォームでは3つの技術点に絞り、補助作品と明記する必要があります。

### 「AIに全部作らせたのでは？」と疑われやすい箇所

クラス分割、30件のテスト、複数の検証script、日英Prompt、整ったdocumentationは、完成状態だけを見ると機械的に見えやすい箇所です。Recorderへ処理が集中した初期版、重複保存、BatchmodeでのScreenshot失敗、検証scriptのprocess待機問題をどう見つけ、何を基準に直したかを自分の言葉で説明できる必要があります。

### 書類で説明すべきこと

- ConsoleのErrorだけでは再現情報が不足した、という出発点
- `Application.logMessageReceived`からReport保存までの流れ
- ScreenshotをFIFO Queueと`WaitForEndOfFrame`で扱った理由
- 取得失敗時にもJSON/Markdownを残す判断
- 自動送信せず、Privacy previewとローカル保存を選んだ判断
- ECHO//SHIFTが主作品で、BugShot AIはUnity開発力の補足であること

### 書類では説明しなくてよいこと

- 全Report fieldや全Privacy patternの列挙
- Promptの日英文面の詳細
- 30件すべてのtest名
- 将来機能やv0.3.0の構想
- UIボタン単位の操作説明

## AI利用

実装方針の整理、テスト観点の洗い出し、コードレビュー、ドキュメント構成の補助にAIツールを使用しました。最終的な仕様判断、コードの採否、Unityへの組み込み、動作確認、修正、テスト結果の確認は自分で行っています。

## 応募フォーム記載候補

Unity開発時のエラー調査を効率化するため、Error発生時のログ、Scene、直前操作、画面を保存するEditor拡張を制作しました。Application.logMessageReceivedでErrorを受け、画面取得はFIFO QueueでWaitForEndOfFrame後に実行します。取得失敗やPlay Mode終了時もReportを残し、callback解除、再帰防止、重複抑制を実装しました。Unity 6000.4.6f1でEditMode test 30件、Submission Validation、clean UPM、Player Build smokeを記録済みです。応募資料追加ではコード本体を変更していません。

# CA Game Gym Supplement Verdict

**USE**

BugShot AIは単独でGame Gym応募の主役にする作品ではありませんが、ECHO//SHIFTと並べることで、Gameplay以外のUnity API理解、デバッグ課題の解決、信頼性と検証への取り組みを補強できます。

## Game Gym応募で使うべき強みTOP3

1. Unityのlog callback、frame timing、Play Mode lifecycleを扱った実装
2. Error発生時の情報不足を、再確認可能なReportへ変えた課題解決
3. failure case、domain reload、clean environmentまで含めた検証

## ECHO//SHIFTと被らない強み

Unity Editor拡張、debugging tool、report pipeline、failure handling、automated validation、release workflowです。ゲームの面白さやGameplayの説明はECHO//SHIFTへ任せます。

## GitHubを載せるなら修正必須の点

リポジトリ直下に案内READMEがなかったため、本作業でPackage READMEへの入口と検証対象versionを示す短いREADMEを追加しました。Package README自体の冒頭は用途、Unity向けであること、解決する問題、実動demoが短時間で分かるため、大規模変更は不要です。

## P0

- なし。新機能や大規模refactorは行わない。

## P1

- 応募フォームではECHO//SHIFTを先に掲載し、BugShot AIを補助作品と明記する。
- 検証済みversionはUnity 6000.4.6f1と書き、Unity 2022.3 LTS対応済みとは書かない。
- test数やvalidation結果を載せる場合は、GitHub Actionsではなくローカル検証結果と明記する。

## 応募後でよい作業

- Unity 2022.3 LTSでの互換性検証
- GitHub Actionsなど継続的な自動実行環境
- v0.3.0以降の機能検討
- 長文記事や追加の面接資料
