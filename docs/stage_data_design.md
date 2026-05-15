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
      "canOperateBoat": true
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
  - `capacity` (number): 最大搭乗数（現仕様は2を想定）。
  - `startLocation` (string): ボート初期位置（`locations.locationId`参照）。
- `locations` (array)
  - `locationId` (string): ロケーションID。
  - `displayName` (string): 表示名。
- `routes` (array)
  - `routeId` (string): ルートID。
  - `from` / `to` (string): 接続先ロケーションID。
  - `bidirectional` (bool): 双方向移動可否。
- `entities` (array)
  - `entityId` (string): キャラクター/アイテムID。
  - `displayName` (string): 表示名。
  - `startLocation` (string): 初期配置ロケーション。
  - `canOperateBoat` (bool): 操船可否。
- `failConditions` (array): 条件違反ルール（将来拡張）。

## 4. 仕様意図
- `locations` / `routes` によって2地点固定から脱却し、複数地点・複数ルートステージを定義可能にする。
- `startLocation` を各要素に持たせることで、柔軟な初期配置を実現する。

## 5. バリデーション方針（実装時）
- `boat.startLocation` が `locations` に存在すること。
- `routes.from` / `routes.to` が `locations` に存在すること。
- `entities.startLocation` が `locations` に存在すること。
- `capacity` が1以上であること。
- `optimalMoves` が1以上であること。
- `entityId` / `locationId` / `routeId` が重複しないこと。

## 6. 更新ルール
- JSON構造を変更した場合、必ず本ドキュメントを同時更新すること。
