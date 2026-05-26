import { useRef } from 'react';
import { StageData } from '../types/stage';
import { downloadJson } from '../utils/exportJson';

type Props = { stage: StageData; onChange: (stage: StageData) => void };

export const StageEditor = ({ stage, onChange }: Props) => {
  const update = <K extends keyof StageData>(k: K, v: StageData[K]) => onChange({ ...stage, [k]: v });
  const toFiniteNumber = (value: string, fallback: number) => {
    const next = Number(value);
    return Number.isFinite(next) ? next : fallback;
  };
  const keySeed = useRef(0);
  const rowKeyMap = useRef(new WeakMap<object, string>());
  const getRowKey = (row: object, prefix: string) => {
    const existing = rowKeyMap.current.get(row);
    if (existing) return existing;
    const created = `${prefix}-row-${keySeed.current += 1}`;
    rowKeyMap.current.set(row, created);
    return created;
  };
  const transferRowKey = (before: object, after: object, prefix: string) => {
    const key = getRowKey(before, prefix);
    rowKeyMap.current.set(after, key);
  };
  const replaceRowWithKey = <T extends object>(rows: T[], index: number, nextRow: T, prefix: string) => {
    const next = [...rows];
    transferRowKey(rows[index], nextRow, prefix);
    next[index] = nextRow;
    return next;
  };
  const appendRowWithKey = <T extends object>(rows: T[], nextRow: T, prefix: string) => {
    getRowKey(nextRow, prefix);
    return [...rows, nextRow];
  };

  return <section>
    <h3>ステージ編集: {stage.stageId}</h3>
    <button className="js-export-stage-json" onClick={() => downloadJson(stage)}>1ステージJSON出力</button>
    <label>
      stageId
      <input className="js-stage-id" value={stage.stageId} readOnly />
    </label>
    {(['title','description','appVersionAdded','theme','difficulty'] as const).map((k) => (
      <label key={k}>{k}<input className={`js-${k}`} value={stage[k]} onChange={(e)=>update(k,e.target.value as never)} /></label>
    ))}
    <label>schemaVersion<input className="js-schema-version" type="number" value={stage.schemaVersion} onChange={(e)=>update('schemaVersion', toFiniteNumber(e.target.value, stage.schemaVersion))} /></label>
    <label>optimalMoves<input className="js-optimal" type="number" value={stage.optimalMoves} onChange={(e)=>update('optimalMoves', toFiniteNumber(e.target.value, stage.optimalMoves))} /></label>

    <h4>boat</h4>
    <input className="js-boat-start-location" value={stage.boat.startLocation} onChange={e=>update('boat',{...stage.boat,startLocation:e.target.value})} placeholder="startLocation"/>
    <input className="js-boat-capacity" type="number" value={stage.boat.capacity} onChange={e=>update('boat',{...stage.boat,capacity:toFiniteNumber(e.target.value, stage.boat.capacity)})} placeholder="capacity"/>
    <input className="js-boat-max-passengers" type="number" value={stage.boat.maxPassengers} onChange={e=>update('boat',{...stage.boat,maxPassengers:toFiniteNumber(e.target.value, stage.boat.maxPassengers)})} placeholder="maxPassengers"/>

    <h4>locations</h4>
    <button className="js-add-location" onClick={()=>{ update('locations',appendRowWithKey(stage.locations, {locationId:'',displayName:''}, 'location')); }}>追加</button>
    {stage.locations.map((l,i)=><div key={getRowKey(l, 'location')}><input className="js-location-id" value={l.locationId} onChange={e=>{const nextRow={...l,locationId:e.target.value};update('locations',replaceRowWithKey(stage.locations, i, nextRow, 'location'));}} placeholder="locationId"/>
    <input className="js-location-display-name" value={l.displayName} onChange={e=>{const nextRow={...l,displayName:e.target.value};update('locations',replaceRowWithKey(stage.locations, i, nextRow, 'location'));}} placeholder="displayName"/>
    <button className="js-delete-location" onClick={()=>{ update('locations',stage.locations.filter((_,x)=>x!==i)); }}>削除</button></div>)}

    <h4>routes</h4><button className="js-add-route" onClick={()=>{ update('routes',appendRowWithKey(stage.routes, {routeId:'',from:'',to:'',bidirectional:true}, 'route')); }}>追加</button>
    {stage.routes.map((r,i)=><div key={getRowKey(r, 'route')}><input className="js-route-id" value={r.routeId} onChange={e=>{const nextRow={...r,routeId:e.target.value};update('routes',replaceRowWithKey(stage.routes, i, nextRow, 'route'));}} placeholder="routeId"/><input className="js-route-from" value={r.from} onChange={e=>{const nextRow={...r,from:e.target.value};update('routes',replaceRowWithKey(stage.routes, i, nextRow, 'route'));}} placeholder="from"/><input className="js-route-to" value={r.to} onChange={e=>{const nextRow={...r,to:e.target.value};update('routes',replaceRowWithKey(stage.routes, i, nextRow, 'route'));}} placeholder="to"/><label>双方向<input className="js-route-bidirectional" type="checkbox" checked={r.bidirectional} onChange={e=>{const nextRow={...r,bidirectional:e.target.checked};update('routes',replaceRowWithKey(stage.routes, i, nextRow, 'route'));}} /></label><button className="js-delete-route" onClick={()=>{ update('routes',stage.routes.filter((_,x)=>x!==i)); }}>削除</button></div>)}

    <h4>entities</h4><button className="js-add-entity" onClick={()=>{ update('entities',appendRowWithKey(stage.entities, {entityId:'',displayName:'',startLocation:'',canOperateBoat:false,spriteId:''}, 'entity')); }}>追加</button>
    {stage.entities.map((r,i)=><div key={getRowKey(r, 'entity')}><input className="js-entity-id" value={r.entityId} onChange={e=>{const nextRow={...r,entityId:e.target.value};update('entities',replaceRowWithKey(stage.entities, i, nextRow, 'entity'));}} placeholder="entityId"/><input className="js-entity-display-name" value={r.displayName} onChange={e=>{const nextRow={...r,displayName:e.target.value};update('entities',replaceRowWithKey(stage.entities, i, nextRow, 'entity'));}} placeholder="displayName"/><input className="js-entity-start-location" value={r.startLocation} onChange={e=>{const nextRow={...r,startLocation:e.target.value};update('entities',replaceRowWithKey(stage.entities, i, nextRow, 'entity'));}} placeholder="startLocation"/><input className="js-entity-sprite-id" value={r.spriteId} onChange={e=>{const nextRow={...r,spriteId:e.target.value};update('entities',replaceRowWithKey(stage.entities, i, nextRow, 'entity'));}} placeholder="spriteId"/><label>操船<input className="js-entity-can-operate-boat" type="checkbox" checked={r.canOperateBoat} onChange={e=>{const nextRow={...r,canOperateBoat:e.target.checked};update('entities',replaceRowWithKey(stage.entities, i, nextRow, 'entity'));}}/></label><button className="js-delete-entity" onClick={()=>{ update('entities',stage.entities.filter((_,x)=>x!==i)); }}>削除</button></div>)}

    <h4>clearConditions</h4>
    <button
      className="js-add-clear-condition"
      onClick={() => { update('clearConditions', appendRowWithKey(stage.clearConditions, { conditionType: 'all_entities_at_location', targetLocationId: '' }, 'clear-condition')); }}
    >
      追加
    </button>
    {stage.clearConditions.map((c, i) => (
      <div key={getRowKey(c, 'clear-condition')}>
        <input
          className="js-clear-condition-type"
          value={c.conditionType}
          onChange={(e) => {
            const nextRow = { ...c, conditionType: e.target.value };
            update('clearConditions', replaceRowWithKey(stage.clearConditions, i, nextRow, 'clear-condition'));
          }}
          placeholder="conditionType"
        />
        <input
          className="js-clear-condition-target-location"
          value={c.targetLocationId}
          onChange={(e) => {
            const nextRow = { ...c, targetLocationId: e.target.value };
            update('clearConditions', replaceRowWithKey(stage.clearConditions, i, nextRow, 'clear-condition'));
          }}
          placeholder="targetLocationId"
        />
        <button className="js-delete-clear-condition" onClick={() => { update('clearConditions', stage.clearConditions.filter((_, x) => x !== i)); }}>削除</button>
      </div>
    ))}
  </section>;
};
