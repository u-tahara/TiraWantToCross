import { StageData } from '../types/stage';
import { downloadJson } from '../utils/exportJson';

type Props = { stage: StageData; onChange: (stage: StageData) => void };

export const StageEditor = ({ stage, onChange }: Props) => {
  const update = <K extends keyof StageData>(k: K, v: StageData[K]) => onChange({ ...stage, [k]: v });

  return <section>
    <h3>ステージ編集: {stage.stageId}</h3>
    <button className="js-export-stage-json" onClick={() => downloadJson(stage)}>1ステージJSON出力</button>
    {(['stageId','title','description','appVersionAdded','theme','difficulty'] as const).map((k) => (
      <label key={k}>{k}<input className={`js-${k}`} value={stage[k]} onChange={(e)=>update(k,e.target.value as never)} /></label>
    ))}
    <label>schemaVersion<input className="js-schema-version" type="number" value={stage.schemaVersion} onChange={(e)=>update('schemaVersion', Number(e.target.value))} /></label>
    <label>optimalMoves<input className="js-optimal" type="number" value={stage.optimalMoves} onChange={(e)=>update('optimalMoves', Number(e.target.value))} /></label>

    <h4>boat</h4>
    <input value={stage.boat.startLocation} onChange={e=>update('boat',{...stage.boat,startLocation:e.target.value})} placeholder="startLocation"/>
    <input type="number" value={stage.boat.capacity} onChange={e=>update('boat',{...stage.boat,capacity:Number(e.target.value)})} placeholder="capacity"/>
    <input type="number" value={stage.boat.maxPassengers} onChange={e=>update('boat',{...stage.boat,maxPassengers:Number(e.target.value)})} placeholder="maxPassengers"/>

    <h4>locations</h4>
    <button className="js-add-location" onClick={()=>update('locations',[...stage.locations,{locationId:'',displayName:''}])}>追加</button>
    {stage.locations.map((l,i)=><div key={i}><input value={l.locationId} onChange={e=>{const n=[...stage.locations];n[i]={...l,locationId:e.target.value};update('locations',n);}} placeholder="locationId"/>
    <input value={l.displayName} onChange={e=>{const n=[...stage.locations];n[i]={...l,displayName:e.target.value};update('locations',n);}} placeholder="displayName"/>
    <button onClick={()=>update('locations',stage.locations.filter((_,x)=>x!==i))}>削除</button></div>)}

    <h4>routes</h4><button className="js-add-route" onClick={()=>update('routes',[...stage.routes,{routeId:'',from:'',to:'',bidirectional:true}])}>追加</button>
    {stage.routes.map((r,i)=><div key={i}><input value={r.routeId} onChange={e=>{const n=[...stage.routes];n[i]={...r,routeId:e.target.value};update('routes',n);}} placeholder="routeId"/><input value={r.from} onChange={e=>{const n=[...stage.routes];n[i]={...r,from:e.target.value};update('routes',n);}} placeholder="from"/><input value={r.to} onChange={e=>{const n=[...stage.routes];n[i]={...r,to:e.target.value};update('routes',n);}} placeholder="to"/><label>双方向<input type="checkbox" checked={r.bidirectional} onChange={e=>{const n=[...stage.routes];n[i]={...r,bidirectional:e.target.checked};update('routes',n);}} /></label><button onClick={()=>update('routes',stage.routes.filter((_,x)=>x!==i))}>削除</button></div>)}

    <h4>entities</h4><button className="js-add-entity" onClick={()=>update('entities',[...stage.entities,{entityId:'',displayName:'',startLocation:'',canOperateBoat:false,spriteId:''}])}>追加</button>
    {stage.entities.map((r,i)=><div key={i}><input value={r.entityId} onChange={e=>{const n=[...stage.entities];n[i]={...r,entityId:e.target.value};update('entities',n);}} placeholder="entityId"/><input value={r.displayName} onChange={e=>{const n=[...stage.entities];n[i]={...r,displayName:e.target.value};update('entities',n);}} placeholder="displayName"/><input value={r.startLocation} onChange={e=>{const n=[...stage.entities];n[i]={...r,startLocation:e.target.value};update('entities',n);}} placeholder="startLocation"/><input value={r.spriteId} onChange={e=>{const n=[...stage.entities];n[i]={...r,spriteId:e.target.value};update('entities',n);}} placeholder="spriteId"/><label>操船<input type="checkbox" checked={r.canOperateBoat} onChange={e=>{const n=[...stage.entities];n[i]={...r,canOperateBoat:e.target.checked};update('entities',n);}}/></label><button onClick={()=>update('entities',stage.entities.filter((_,x)=>x!==i))}>削除</button></div>)}

    <h4>clearConditions</h4>
    <button
      className="js-add-clear-condition"
      onClick={() => update('clearConditions', [...stage.clearConditions, { conditionType: 'all_entities_at_location', targetLocationId: '' }])}
    >
      追加
    </button>
    {stage.clearConditions.map((c, i) => (
      <div key={i}>
        <input
          value={c.conditionType}
          onChange={(e) => {
            const n = [...stage.clearConditions];
            n[i] = { ...c, conditionType: e.target.value };
            update('clearConditions', n);
          }}
          placeholder="conditionType"
        />
        <input
          value={c.targetLocationId}
          onChange={(e) => {
            const n = [...stage.clearConditions];
            n[i] = { ...c, targetLocationId: e.target.value };
            update('clearConditions', n);
          }}
          placeholder="targetLocationId"
        />
        <button onClick={() => update('clearConditions', stage.clearConditions.filter((_, x) => x !== i))}>削除</button>
      </div>
    ))}
  </section>;
};
