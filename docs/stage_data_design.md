# stage_data_design.md

## 1. 目的
本ドキュメントは、川渡り系パズルのステージデータをJSONで定義するための共通仕様です。

## 2. 基本構造
```json
{
  "stageId": "stage_001",
  "title": "2匹を渡そう",
  "optimalMoves": 3,
  "boat": {
    "capacity": 2,
    "startLocation": "left"
  },
  "maxPassengers": 2,
  "locations": [
    { "locationId": "left", "displayName": "ひだり岸" },
    { "locationId": "right", "displayName": "みぎ岸" }
  ],
  "routes": [
    { "routeId": "r1", "from": "left", "to": "right", "bidirectional": true }
  ],
  "entities": [
    {
      "entityId": "a",
      "displayName": "A",
      "startLocation": "left",
      "canOperateBoat": true,
      "spriteId": "chinchilla_default"
    }
  ],
  "uiText": {
    "objective": "チラとインコを右岸へ運ぼう",
    "tip": "2匹まで一緒にボートに乗れるよ",
    "stageSelectDescription": "2匹でボート移動の基本を学ぶステージ"
  },
  "clearConditions": [
    {
      "conditionType": "all_entities_at_location",
      "targetLocationId": "right"
    }
  ],
  "failConditions": []
}
```

## 3. フィールド定義
- `stageId` (string): ステージ識別子。ユニーク。
- `title` (string): ステージ表示名。
- `optimalMoves` (number): 最短手数。
- `boat` (object)
  - `capacity` (number): 最大搭乗数（最優先で参照）。
  - `maxPassengers` (number): 最大搭乗数の別名（`boat.capacity` 未指定時のフォールバック）。
  - `startLocation` (string): ボート初期位置（`locations.locationId`参照）。
- `maxPassengers` (number): 最大搭乗数の別名（`boat.capacity` と `boat.maxPassengers` が未指定時に参照）。
- `capacity` (number): 最大搭乗数の別名（上記3項目が未指定時に参照）。
- `locations` (array)
  - `locationId` (string): ロケーションID。
  - `displayName` (string): 表示名。
- `routes` (array)
  - `routeId` (string): ルートID。
  - `from` / `to` (string): 接続先ロケーションID。
  - `bidirectional` (bool): 双方向移動可否。
- `entities` (array)
  - `entityId` (string): ゲームロジック上の個体識別ID。
  - `displayName` (string): 画面表示名。
  - `startLocation` (string): 初期配置ロケーション。
  - `canOperateBoat` (bool): 操船可否。
  - `spriteId` (string, optional): 画像指定用ID。設定時は用途別画像（盤面: `Assets/Resources/Sprites/Characters/{spriteId}/{spriteId}_board.png`、アイコン: `Assets/Resources/Sprites/Characters/{spriteId}/{spriteId}_icon.png`）を優先して読み込む。用途別がない場合は `Assets/Resources/Sprites/Characters/{spriteId}/{spriteId}.png` と旧仕様互換パス（`Assets/Resources/Sprites/Characters/{spriteId}_*.png`）にフォールバックする。未設定・空文字時は同じ規則で `entityId` を画像IDとして使用する。
- `clearConditions` (array): クリア条件ルール。
  - `conditionType` (string): 条件種別。現時点は `all_entities_at_location` のみ対応。
  - `targetLocationId` (string): 全員到達先ロケーションID。
- `failConditions` (array): 条件違反ルール（将来拡張）。
  - `conditionType` (string): 条件種別。最小実装では `entity_alone_with` を予約。
  - `locationId` (string): 判定対象ロケーションID。
  - `entityIds` (array): 同居判定対象のentityIdリスト。
- `uiText` (object, optional): ステージ固有の表示文。
  - `objective` (string): ゲーム画面の目的文。
  - `tip` (string): ゲーム画面の補足文。
  - `stageSelectDescription` (string): ステージ一覧用説明文。
  - 未設定時はUI側で汎用文にフォールバックする。

## 4. 仕様意図
- `locations` / `routes` によって2地点固定から脱却し、複数地点・複数ルートステージを定義可能にする。
- `startLocation` を各要素に持たせることで、柔軟な初期配置を実現する。
- `clearConditions` / `failConditions` を配列化して、将来の複数ルール共存に対応する。
- ステージ固有の案内文（目的・補足・一覧説明）は `uiText` に集約し、未選択・操船不可・定員到達など共通ルール由来メッセージはC#側の共通処理で扱う。

## 5. バリデーション方針（実装時）
- `boat.startLocation` が `locations` に存在すること。
- `routes.from` / `routes.to` が `locations` に存在すること。
- `entities.startLocation` が `locations` に存在すること。
- 最大搭乗数（`boat.capacity` / `boat.maxPassengers` / `maxPassengers` / `capacity` のいずれか）が1以上であること。
- `optimalMoves` が1以上であること。
- `entityId` / `locationId` / `routeId` が重複しないこと。

## 6. 更新ルール
- JSON構造を変更した場合、必ず本ドキュメントを同時更新すること。

## 7. 配置場所（読み込み経路）
- ステージJSONは `Assets/Resources/Data/Stages/` 配下に配置する。
- ローダーは `Resources.Load` / `Resources.LoadAll` で `Data/Stages` を参照する。
- `LoadByStageId("stage_001")` のように、ファイル名（拡張子なし）を `stageId` と一致させる。
- これによりエディタ実行時・ビルド実行時の両方で同一経路から読み込める。
