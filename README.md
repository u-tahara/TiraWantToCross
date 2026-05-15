# TiraWantToCross

「チラは渡りたい！」のUnityプロジェクトです。  
川渡り問題をモチーフにした**スマホ向け2Dロジックパズル**を開発します。

## 今回の状態（初期開発準備）
このリポジトリは、実装前に設計と開発ルールを整備した状態です。

- ゲーム仕様の初期整理
- ステージJSON仕様の定義
- Codex向け作業ガイドの作成
- Unity向けフォルダ構成の準備
- サンプルステージデータ（3問）の追加

> 本コミット時点では、C#の本格的なゲームロジック実装は未着手です。

## ドキュメント
- `docs/game_design.md` : ゲーム全体の目的・コアループ・失敗/成功条件
- `docs/stage_data_design.md` : ステージデータ(JSON)の構造とバリデーション方針
- `docs/codex_instructions.md` : Codexが今後実装を進める際の具体的な手順
- `AGENTS.md` : リポジトリ全体で守る開発ルール

## 主要ディレクトリ
- `Assets/Scripts/` : C#スクリプト配置先（今後実装）
- `Assets/Data/Stages/` : ステージJSON配置先
- `Assets/Prefabs/` : プレハブ配置先
- `Assets/Scenes/` : シーン配置先
- `Assets/Sprites/` : スプライト配置先

## サンプルステージ
- `Assets/Data/Stages/stage_001.json` : 2匹を左から右へ移動
- `Assets/Data/Stages/stage_002.json` : 3匹を左から右へ移動
- `Assets/Data/Stages/stage_003.json` : 3匹のうち1匹のみ操船可能

## 次の開発ステップ（推奨）
1. ステージJSONを読み込む`StageLoader`の最小実装
2. データの妥当性チェック（location存在確認、route整合性）
3. ステージ状態を保持する`GameState`モデル設計
4. 1ステージ分の手動プレイ可能な最小シーン作成
