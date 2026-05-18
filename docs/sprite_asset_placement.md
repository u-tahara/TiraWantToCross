# Sprite素材の配置ルール

Unityの`Resources.Load`で画像を読み込むため、Sprite素材は以下の場所に配置してください。

## 配置先フォルダ
- キャラクター画像: `Assets/Resources/Sprites/Characters/`
- ボート画像: `Assets/Resources/Sprites/Boat/`

## 命名ルール

### キャラクター画像
- `entityId` と同じファイル名（拡張子は `.png`）にすること
- 例:
  - `tira_1.png`
  - `tira_2.png`
  - `tira_3.png`
  - `tira_4.png`

### ボート画像
- ファイル名は `boat.png`

## Unity Import設定
画像をUnityに追加したあと、Inspectorで以下を設定してください。
- `Texture Type`: **Sprite (2D and UI)**

## 画像未配置時の挙動
- 指定Spriteが存在しない場合、表示は仮表示（フォールバック）になります。
- そのため、素材準備中でもゲーム進行を止めずに確認可能です。
