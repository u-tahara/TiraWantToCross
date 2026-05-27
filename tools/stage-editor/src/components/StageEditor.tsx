import { useRef } from 'react';
import { StageData } from '../types/stage';
import { downloadJson } from '../utils/exportJson';

type Props = { stage: StageData; onChange: (stage: StageData) => void; hasStageErrors: boolean };

export const StageEditor = ({ stage, onChange, hasStageErrors }: Props) => {
  const update = <K extends keyof StageData>(k: K, v: StageData[K]) => onChange({ ...stage, [k]: v });
  const toFiniteNumber = (value: string, fallback: number) => Number.isFinite(Number(value)) ? Number(value) : fallback;
  const keySeed = useRef(0);
  const rowKeyMap = useRef(new WeakMap<object, string>());
  const getRowKey = (row: object, prefix: string) => rowKeyMap.current.get(row) ?? (rowKeyMap.current.set(row, `${prefix}-${++keySeed.current}`), rowKeyMap.current.get(row)!);
  const replaceRowWithStableKey = <T extends object>(rows: T[], index: number, createNext: (row: T) => T, prefix: string) => rows.map((row, rowIndex) => {
    if (rowIndex != index) return row;
    const nextRow = createNext(row);
    const key = getRowKey(row, prefix);
    rowKeyMap.current.set(nextRow, key);
    return nextRow;
  });

  return <section className="card">
    <h2>ステージ編集: {stage.stageId}</h2>
    <p className="helper-text">この画面では、ステージの基本情報・地点・ルート・キャラクター・移動手段・ルールを入力し、JSONを出力できます。</p>
    <button className="js-export-stage-json button-primary" onClick={() => {
      if (hasStageErrors) {
        alert('このステージにはエラーがあります。先にバリデーション結果を修正してください。');
        return;
      }
      downloadJson(stage);
    }}>1ステージJSON出力</button>

    <h3>基本情報</h3><p className="helper-text">ステージID、タイトル、テーマ、難易度、最短手数などを設定します。</p>
    <label>stageId（識別ID）<input className="js-stage-id" value={stage.stageId} readOnly placeholder="stage_001" /></label>
    {(['title', 'description', 'appVersionAdded', 'theme', 'difficulty'] as const).map((k) => <label key={k}>{k}<input className={`js-${k}`} value={stage[k]} onChange={(e) => update(k, e.target.value as never)} placeholder={k === 'title' ? 'はじめての川渡り' : k} /></label>)}
    <div className="row-inline">
      <label>schemaVersion<input className="js-schema-version" type="number" value={stage.schemaVersion} onChange={(e) => update('schemaVersion', toFiniteNumber(e.target.value, stage.schemaVersion))} placeholder="1" /></label>
      <label>optimalMoves（最短手数）<input className="js-optimal" type="number" value={stage.optimalMoves} onChange={(e) => update('optimalMoves', toFiniteNumber(e.target.value, stage.optimalMoves))} placeholder="12" /></label>
    </div>

    <h3>地点設定</h3><p className="helper-text">left / right / center など、キャラクターやボートが存在できる場所を登録します。locationId は他設定から参照されるIDです。</p>
    <button className="js-add-location button-secondary" onClick={() => update('locations', [...stage.locations, { locationId: '', displayName: '' }])}>地点を追加</button>
    {stage.locations.map((l, i) => <div className="row-inline" key={getRowKey(l, 'location')}><input className="js-location-id" value={l.locationId} onChange={e => update('locations', replaceRowWithStableKey(stage.locations, i, (x) => ({ ...x, locationId: e.target.value }), 'location'))} placeholder="locationId（例: left）" /><input className="js-location-display-name" value={l.displayName} onChange={e => update('locations', replaceRowWithStableKey(stage.locations, i, (x) => ({ ...x, displayName: e.target.value }), 'location'))} placeholder="表示名（例: 左岸）" /><button className="js-delete-location button-danger" onClick={() => update('locations', stage.locations.filter((_, x) => x !== i))}>削除</button></div>)}

    <h3>ルート設定</h3><p className="helper-text">どの地点からどの地点へ移動できるかを設定します。from / to には登録済みの locationId を指定します。</p>
    <button className="js-add-route button-secondary" onClick={() => update('routes', [...stage.routes, { routeId: '', from: '', to: '', bidirectional: true }])}>ルートを追加</button>
    {stage.routes.map((r, i) => <div className="row-inline" key={getRowKey(r, 'route')}><input className="js-route-id" value={r.routeId} onChange={e => update('routes', replaceRowWithStableKey(stage.routes, i, (x) => ({ ...x, routeId: e.target.value }), 'route'))} placeholder="routeId（例: left_to_right）" /><input className="js-route-from" value={r.from} onChange={e => update('routes', replaceRowWithStableKey(stage.routes, i, (x) => ({ ...x, from: e.target.value }), 'route'))} placeholder="from locationId" /><input className="js-route-to" value={r.to} onChange={e => update('routes', replaceRowWithStableKey(stage.routes, i, (x) => ({ ...x, to: e.target.value }), 'route'))} placeholder="to locationId" /><label>双方向<input className="js-route-bidirectional" type="checkbox" checked={r.bidirectional} onChange={e => update('routes', replaceRowWithStableKey(stage.routes, i, (x) => ({ ...x, bidirectional: e.target.checked }), 'route'))} /></label><button className="js-delete-route button-danger" onClick={() => update('routes', stage.routes.filter((_, x) => x !== i))}>削除</button></div>)}

    <h3>キャラクター設定</h3><p className="helper-text">登場する動物やキャラクターを登録します。entityId は参照ID、startLocation は登録済み locationId を指定します。</p>
    <button className="js-add-entity button-secondary" onClick={() => update('entities', [...stage.entities, { entityId: '', displayName: '', startLocation: '', canOperateBoat: false, spriteId: '' }])}>キャラクターを追加</button>
    {stage.entities.map((r, i) => <div className="row-inline" key={getRowKey(r, 'entity')}><input className="js-entity-id" value={r.entityId} onChange={e => update('entities', replaceRowWithStableKey(stage.entities, i, (x) => ({ ...x, entityId: e.target.value }), 'entity'))} placeholder="entityId（例: wolf）" /><input className="js-entity-display-name" value={r.displayName} onChange={e => update('entities', replaceRowWithStableKey(stage.entities, i, (x) => ({ ...x, displayName: e.target.value }), 'entity'))} placeholder="表示名（例: オオカミ）" /><input className="js-entity-start-location" value={r.startLocation} onChange={e => update('entities', replaceRowWithStableKey(stage.entities, i, (x) => ({ ...x, startLocation: e.target.value }), 'entity'))} placeholder="startLocation（例: left）" /><input className="js-entity-sprite-id" value={r.spriteId} onChange={e => update('entities', replaceRowWithStableKey(stage.entities, i, (x) => ({ ...x, spriteId: e.target.value }), 'entity'))} placeholder="spriteId" /><label>操船<input className="js-entity-can-operate-boat" type="checkbox" checked={r.canOperateBoat} onChange={e => update('entities', replaceRowWithStableKey(stage.entities, i, (x) => ({ ...x, canOperateBoat: e.target.checked }), 'entity'))} /></label><button className="js-delete-entity button-danger" onClick={() => update('entities', stage.entities.filter((_, x) => x !== i))}>削除</button></div>)}

    <h3>移動手段設定</h3><p className="helper-text">ボートなどの移動手段を登録します。capacity は一度に乗れる数です。startLocation には登録済み locationId を指定します。</p>
    <div className="row-inline"><label>startLocation<input className="js-boat-start-location" value={stage.boat.startLocation} onChange={e => update('boat', { ...stage.boat, startLocation: e.target.value })} placeholder="left" /></label>
      <label>capacity<input className="js-boat-capacity" type="number" value={stage.boat.capacity} onChange={e => update('boat', { ...stage.boat, capacity: toFiniteNumber(e.target.value, stage.boat.capacity) })} placeholder="2" /></label>
      <label>maxPassengers<input className="js-boat-max-passengers" type="number" value={stage.boat.maxPassengers} onChange={e => update('boat', { ...stage.boat, maxPassengers: toFiniteNumber(e.target.value, stage.boat.maxPassengers) })} placeholder="2" /></label></div>

    <h3>ルール設定</h3><p className="helper-text">禁止組み合わせやゴール条件を設定します。targetLocationId には登録済み locationId を指定します。</p>
    <button className="js-add-clear-condition button-secondary" onClick={() => update('clearConditions', [...stage.clearConditions, { conditionType: 'all_entities_at_location', targetLocationId: '' }])}>クリア条件を追加</button>
    {stage.clearConditions.map((c, i) => <div className="row-inline" key={getRowKey(c, 'clear-condition')}><input className="js-clear-condition-type" value={c.conditionType} onChange={e => update('clearConditions', replaceRowWithStableKey(stage.clearConditions, i, (x) => ({ ...x, conditionType: e.target.value }), 'clear-condition'))} placeholder="conditionType（例: all_entities_at_location）" /><input className="js-clear-condition-target-location" value={c.targetLocationId} onChange={e => update('clearConditions', replaceRowWithStableKey(stage.clearConditions, i, (x) => ({ ...x, targetLocationId: e.target.value }), 'clear-condition'))} placeholder="targetLocationId（例: right）" /><button className="js-delete-clear-condition button-danger" onClick={() => update('clearConditions', stage.clearConditions.filter((_, x) => x !== i))}>削除</button></div>)}
  </section>;
};
