# ローカル向けステージJSONエディタ レビュー結果

対象: `tools/stage-editor/src` 一式

## 要修正ポイント（優先度順）

### 1. フィルタ中に編集すると別ステージへ書き込まれるリスク（高）
- `App.tsx` では編集中ステージを `editingIndex`（元配列の添字）で保持しています。
- しかし一覧は `filtered`（検索で絞り込んだ配列）を表示しており、検索条件が変わると表示順・要素が変化します。
- この状態で `editingIndex` を元に更新を続けると、ユーザーの意図しないステージへ上書きする可能性があります。

**改善案**
- `editingIndex` ではなく `editingStageId` で編集中対象を保持し、更新時に `stageId` で検索して置換する。
- さらに `stageId` 編集時の扱い（後述）と組み合わせ、内部一意キーを別に持つとより安全です。

### 2. `stageId` 直接編集により参照切れ・選択不整合が起きる（高）
- `stageId` を入力欄で直接変更できますが、`selected` は文字列配列で保持しているため、変更後に選択状態が崩れます。
- `onEdit` / `onDelete` でもオブジェクト参照と index を混在利用しており、ID変更後のUXが不安定になります。

**改善案**
- 案A: `stageId` を読み取り専用にし、専用の「ID変更」操作で重複チェック＋関連状態更新を一括実施。
- 案B: 内部キー（UUID等）を導入し、UI編集対象・選択対象は内部キーで管理。JSON出力時のみ `stageId` を使う。

### 3. `Number()` 変換で `NaN` が入りうる（中）
- 数値項目（`schemaVersion`, `optimalMoves`, `capacity`, `maxPassengers`）で空文字や不正入力時に `NaN` が状態へ保存されます。
- そのままJSON出力すると仕様外データの温床になります。

**改善案**
- 入力値が空の場合は `0` または未設定扱いに統一。
- `Number.isFinite(next)` で検証し、無効値は更新しない or エラーメッセージ表示。

### 4. keyに配列indexを使っており、行追加・削除時に入力取り違えが起きる（中）
- `locations/routes/entities/clearConditions` のレンダリングが `key={i}` になっています。
- Reactでは中間削除時にDOM再利用で別行の入力が残ることがあり、編集ミスを誘発します。

**改善案**
- 一時キー（`_clientId`）を各要素に付与して `key` に使用。
- もしくは `locationId` 等が入力必須なら、そのユニーク性を担保してkey利用。

### 5. アクセシビリティ/テスト容易性のclass命名が一部未適用（中）
- 指定ルール「jsから要素を指定するときは `js-○○` class を使用」に対し、以下が未付与です。
  - boat系input
  - 行内の削除ボタン（locations/routes/entities/clearConditions）
  - clearConditions の input
- 将来のE2EやUI操作スクリプトで要素特定が難しくなります。

**改善案**
- 主要操作・入力すべてに `js-*` class を付与（規則を統一）。

### 6. バリデーション結果と編集導線の接続が弱い（低〜中）
- `ValidationPanel` は表示のみで、該当ステージやフィールドへジャンプできません。
- エラー件数が増えると修正効率が落ちます。

**改善案**
- エラー行クリックで該当 `stageId` を編集対象にする。
- 可能なら `path` を使って該当入力をハイライト。

## 実施順の提案（小PR分割）
1. **PR1（安全性）**: 編集対象管理を `stageId` / 内部キー化し、フィルタ中編集の誤更新を解消。
2. **PR2（データ品質）**: 数値入力の `NaN` 対策、`stageId` 変更フローのガード追加。
3. **PR3（運用性）**: `js-*` class の全面整備、ValidationPanelから編集導線を追加。


## PR分割の詳細（実装チケット化しやすい形）

### PR1: 編集対象の誤更新防止（安全性）
**目的**
- 検索フィルタ利用中でも、意図したステージだけを確実に編集できる状態にする。

**対象ファイル**
- `tools/stage-editor/src/App.tsx`
- 必要に応じて `tools/stage-editor/src/components/StageList.tsx`

**作業項目**
- 編集中ステージの管理を `editingIndex` から `editingStageId`（または内部キー）へ置換。
- `onEdit/onDelete/onDuplicate` の対象解決を index依存からID依存へ変更。
- 検索条件変更後も編集対象がズレないことを確認。

**受け入れ条件**
- 検索語を変更しても、編集中ステージへの入力が別ステージへ反映されない。
- 編集・削除・複製の対象が常に表示中の該当行と一致する。

### PR2: `stageId` と数値入力のデータ品質改善（品質）
**目的**
- 出力JSONに仕様外データ（重複ID/`NaN`）が入らないようにする。

**対象ファイル**
- `tools/stage-editor/src/App.tsx`
- `tools/stage-editor/src/components/StageEditor.tsx`
- 必要に応じて `tools/stage-editor/src/utils/validation.ts`

**作業項目**
- `stageId` 編集方式を見直し（読み取り専用 + 専用変更操作、または内部キー導入）。
- `stageId` 変更時に重複チェックと `selected` など関連状態の同期を実装。
- 数値入力で `Number.isFinite` を使い、`NaN` を状態へ入れない。

**受け入れ条件**
- 不正な数値入力後でも状態・出力JSONに `NaN` が含まれない。
- `stageId` 変更後も選択状態や編集対象が破綻しない。

### PR3: 操作性・保守性の向上（運用性）
**目的**
- UIテストしやすく、バリデーション修正ループを短くする。

**対象ファイル**
- `tools/stage-editor/src/components/StageEditor.tsx`
- `tools/stage-editor/src/components/ValidationPanel.tsx`
- 必要に応じて `tools/stage-editor/src/components/StageList.tsx`

**作業項目**
- 主要操作要素に `js-○○` class を付与（boat入力、削除ボタン、clearConditions入力など）。
- ValidationPanelの各エラーから該当ステージ編集へ遷移できる導線を追加。
- 可能なら `path` ベースで該当フィールドの強調表示を検討。

**受け入れ条件**
- E2E/手動確認で要素特定に困らない（`js-*` class が一貫）。
- エラー表示から対象ステージへ1アクションで到達できる。

