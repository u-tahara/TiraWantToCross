# Stage Editor

## 概要
このツールは、川渡りロジックパズルゲームで使用するステージJSONを作成・編集・出力するためのローカルWeb管理画面です。

サーバーやDBは使用せず、ブラウザ上でステージを編集し、JSONまたはUnity用ZIPとして出力します。

## 起動方法
```bash
cd tools/stage-editor
npm install
npm run dev
```

## ビルド方法
```bash
cd tools/stage-editor
npm run build
```

## 基本的な使い方
1. ステージ一覧画面を開く
2. 新規ステージを作成する
3. 基本情報を入力する
4. 地点を追加する
5. ルートを追加する
6. キャラクターを追加する
7. 移動手段を追加する
8. ルール・ゴール・最短手数を設定する
9. バリデーションエラーを確認する
10. 問題がなければJSONまたはUnity用ZIPを出力する
11. ZIPを展開してUnityプロジェクトの `Assets/Data/Stages/` に配置する

## 入力項目の説明
### 基本情報
- `stageId`：ステージ識別ID（例: `stage_001`）
- `title`：ステージ名
- `description`：説明文
- `schemaVersion`：ステージJSON形式のバージョン
- `appVersionAdded`：追加したアプリバージョン
- `theme`：テーマ
- `difficulty`：難易度
- `optimalMoves`：最短手数

### 地点設定
キャラクターやボートが存在できる場所を登録します（例: `left` `right` `center`）。

### ルート設定
`from` / `to` に登録済み `locationId` を指定し、移動可能ルートを設定します。

### キャラクター設定
登場キャラクターを登録します。`startLocation` には登録済み `locationId` を指定します。

### 移動手段設定
ボートなどの移動手段を登録します。`capacity` は一度に乗れる数です。

## JSON出力方法
1ステージ単体JSONを出力できます。ファイル名は `stageId + ".json"` です（例: `stage_001.json`）。

## Unity用ZIP出力方法
全ステージまたは選択ステージをUnity用ZIPとして出力できます。ZIP内は以下の構成です。

```text
Assets/
  Data/
    Stages/
      stage_001.json
      stage_002.json
      stages_manifest.json
```

## Unityプロジェクトへの配置方法
Unity用ZIPを展開し、`Assets/Data/Stages/` 配下のJSONをUnityプロジェクトの同パスに配置してください。

## localStorage保存について
編集中ステージはブラウザの `localStorage` に保存されます。ブラウザを閉じても保持されますが、ブラウザデータ削除や別ブラウザでは消失・非共有になる場合があります。重要データは必ずJSONまたはZIPで出力してください。

## よくあるエラーと対処法
- `stageId が空です`：`stage_001` のようなIDを入力
- `stageId が重複しています`：既存と重複しないIDへ変更
- `route の from / to が存在しません`：先に locations に地点を追加
- `entity の startLocation が存在しません`：locations の `locationId` を参照
- `boat.startLocation が存在しません`：移動手段の開始地点を locations に合わせる
- `capacity が1未満です`：`capacity` を1以上に設定
- `optimalMoves が1未満です`：最短手数を1以上に設定
