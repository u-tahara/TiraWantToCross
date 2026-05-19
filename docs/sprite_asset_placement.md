# Sprite素材の配置ルール

Unityの`Resources.Load`で画像を読み込むため、Sprite素材は以下の場所に配置してください。

## 配置先フォルダ
- キャラクター画像: `Assets/Resources/Sprites/Characters/`
- ボート画像: `Assets/Resources/Sprites/Boat/`

## 命名ルール

### キャラクター画像
- `spriteId` を設定している場合は `spriteId` と同じファイル名（拡張子は `.png`）にすること
- `spriteId` が未設定・空文字の場合は `entityId` と同じファイル名で読み込まれる
- 命名は英小文字・数字・アンダースコアを推奨（例: `chinchilla_default`, `capybara_default`）
- 例:
  - `chinchilla_default.png`
  - `capybara_default.png`
  - `tira_1.png`（`spriteId` 未設定時のフォールバック確認用）

### ボート画像
- ファイル名は `boat.png`

## Unity Import設定
画像をUnityに追加したあと、Inspectorで以下を設定してください。
- `Texture Type`: **Sprite (2D and UI)**

## 画像未配置時の挙動
- 指定Spriteが存在しない場合、表示は仮表示（フォールバック）になります。
- そのため、素材準備中でもゲーム進行を止めずに確認可能です。


## 読み込み優先順位
1. `spriteId` が設定されている場合: `Assets/Resources/Sprites/Characters/{spriteId}.png`
2. `spriteId` が未設定・空文字の場合: `Assets/Resources/Sprites/Characters/{entityId}.png`
3. どちらの画像も見つからない場合: 仮色表示 + `IMG` プレースホルダーにフォールバック
