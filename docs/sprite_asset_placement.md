# Sprite素材の配置ルール

Unityの`Resources.Load`で画像を読み込むため、Sprite素材は以下の場所に配置してください。

## 配置先フォルダ
- キャラクター画像: `Assets/Resources/Sprites/Characters/{spriteId}/`
- ボート画像: `Assets/Resources/Sprites/Boat/`

## 命名ルール

### キャラクター画像
- 盤面表示用: `{spriteId}_board.png`
- 下部アイコン用: `{spriteId}_icon.png`
- 用途別画像がない場合のフォールバック用: `{spriteId}.png`（任意）
- `spriteId` が未設定・空文字の場合は `entityId` を同様の規則で使用

### 命名例
- `Assets/Resources/Sprites/Characters/tira/tira_board.png`
- `Assets/Resources/Sprites/Characters/tira/tira_icon.png`
- `Assets/Resources/Sprites/Characters/inko/inko_board.png`
- `Assets/Resources/Sprites/Characters/inko/inko_icon.png`
- `Assets/Resources/Sprites/Characters/cat/cat_board.png`
- `Assets/Resources/Sprites/Characters/cat/cat_icon.png`
- `Assets/Resources/Sprites/Characters/bear/bear_board.png`
- `Assets/Resources/Sprites/Characters/bear/bear_icon.png`

### ボート画像
- ファイル名は `boat.png`

## 読み込み優先順位
1. 用途別（`spriteId`あり）
   - Board: `Sprites/Characters/{spriteId}/{spriteId}_board`
   - Icon: `Sprites/Characters/{spriteId}/{spriteId}_icon`
2. 通常画像（`spriteId`あり）
   - `Sprites/Characters/{spriteId}/{spriteId}`
3. 旧仕様互換（`spriteId`あり）
   - `Sprites/Characters/{spriteId}_board` / `Sprites/Characters/{spriteId}_icon` / `Sprites/Characters/{spriteId}`
4. `spriteId` がない場合は `entityId` で 1〜3 と同じ順序を試行
5. 見つからない場合は仮表示（`IMG`プレースホルダー）

## Unity Import設定
画像をUnityに追加したあと、Inspectorで以下を設定してください。
- `Texture Type`: **Sprite (2D and UI)**

## 画像未配置時の挙動
- 指定Spriteが存在しない場合、表示は仮表示（フォールバック）になります。
- そのため、素材準備中でもゲーム進行を止めずに確認可能です。
