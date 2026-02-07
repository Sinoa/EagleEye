# EagleEye 開発フェーズ・進行管理

## 開発フェーズ

```
Phase 0: データパイプライン構築          ← 現在ここ（パーサー・場況復元まで完了）
Phase 1: 最小構成ベースモデル（手牌Attention + 基本スカラー特徴、打牌出力のみ）
Phase 2: 捨て牌追加（4プレイヤー分のAttention + RoPE）
Phase 3: 全アクション対応（鳴き・リーチ・和了・槓の出力追加）
Phase 4: LoRA効果検証（極端なキャラで単層LoRA検証）
Phase 5: パイプライン構築（キャラ量産の自動化）
Phase 6: 大規模運用（数百体のキャラ管理）
Phase 7: 3人麻雀対応（オプション）
Phase 8: 強化学習（オプション）
```

## Phase 0 残タスク

- [ ] GameState → GameStateSnapshot 変換ロジック
- [ ] インスタンスID付与（手牌ソート + 同一牌カウント）
- [ ] 有効アクションマスク生成（ルールベース合法手判定）
- [ ] JSONL出力（シリアライズ + ファイル分割）
- [ ] 視点変換（絶対位置 → 相対位置）
- [ ] データリーク防止テスト
- [ ] train/valid/test 分割

## 既存実装への確認事項（Replayer拡張の可能性）

1. ~~**リーチ巡目**~~: `PlayerState.ReachTurnNumber` (int?) で取得可能 ✅
2. ~~**残り山牌数**~~: `GameState.RemainingTileCount` (int) で取得可能 ✅
3. ~~**副露の巡目**~~: `MeldInfo.TurnNumber` (int?) で取得可能 ✅
4. **副露の順序**: Melds リストが時系列順に格納されているか（要確認）
