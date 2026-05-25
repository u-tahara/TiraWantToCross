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
    <label>optimalMoveCount<input className="js-optimal" type="number" value={stage.optimalMoveCount} onChange={(e)=>update('optimalMoveCount', Number(e.target.value))} /></label>
    <h4>locations</h4>
    <button className="js-add-location" onClick={()=>update('locations',[...stage.locations,{locationId:'',name:'',type:''}])}>追加</button>
    {stage.locations.map((l,i)=><div key={i}><input value={l.locationId} onChange={e=>{const n=[...stage.locations];n[i]={...l,locationId:e.target.value};update('locations',n);}} placeholder="locationId"/>
    <input value={l.name} onChange={e=>{const n=[...stage.locations];n[i]={...l,name:e.target.value};update('locations',n);}} placeholder="name"/>
    <input value={l.type} onChange={e=>{const n=[...stage.locations];n[i]={...l,type:e.target.value};update('locations',n);}} placeholder="type"/>
    <button onClick={()=>update('locations',stage.locations.filter((_,x)=>x!==i))}>削除</button></div>)}
    <h4>routes</h4><button className="js-add-route" onClick={()=>update('routes',[...stage.routes,{routeId:'',from:'',to:'',transportId:''}])}>追加</button>
    {stage.routes.map((r,i)=><div key={i}><input value={r.routeId} onChange={e=>{const n=[...stage.routes];n[i]={...r,routeId:e.target.value};update('routes',n);}} placeholder="routeId"/><input value={r.from} onChange={e=>{const n=[...stage.routes];n[i]={...r,from:e.target.value};update('routes',n);}} placeholder="from"/><input value={r.to} onChange={e=>{const n=[...stage.routes];n[i]={...r,to:e.target.value};update('routes',n);}} placeholder="to"/><input value={r.transportId} onChange={e=>{const n=[...stage.routes];n[i]={...r,transportId:e.target.value};update('routes',n);}} placeholder="transportId"/><button onClick={()=>update('routes',stage.routes.filter((_,x)=>x!==i))}>削除</button></div>)}
    <h4>entities</h4><button className="js-add-entity" onClick={()=>update('entities',[...stage.entities,{entityId:'',name:'',type:'',startLocation:'',goalLocation:'',canOperateBoat:false}])}>追加</button>
    {stage.entities.map((r,i)=><div key={i}><input value={r.entityId} onChange={e=>{const n=[...stage.entities];n[i]={...r,entityId:e.target.value};update('entities',n);}} placeholder="entityId"/><input value={r.name} onChange={e=>{const n=[...stage.entities];n[i]={...r,name:e.target.value};update('entities',n);}} placeholder="name"/><input value={r.type} onChange={e=>{const n=[...stage.entities];n[i]={...r,type:e.target.value};update('entities',n);}} placeholder="type"/><input value={r.startLocation} onChange={e=>{const n=[...stage.entities];n[i]={...r,startLocation:e.target.value};update('entities',n);}} placeholder="startLocation"/><input value={r.goalLocation} onChange={e=>{const n=[...stage.entities];n[i]={...r,goalLocation:e.target.value};update('entities',n);}} placeholder="goalLocation"/><label>操船<input type="checkbox" checked={r.canOperateBoat} onChange={e=>{const n=[...stage.entities];n[i]={...r,canOperateBoat:e.target.checked};update('entities',n);}}/></label><button onClick={()=>update('entities',stage.entities.filter((_,x)=>x!==i))}>削除</button></div>)}
    <h4>transports</h4><button className="js-add-transport" onClick={()=>update('transports',[...stage.transports,{transportId:'',name:'',capacity:1,startLocation:''}])}>追加</button>
    {stage.transports.map((r,i)=><div key={i}><input value={r.transportId} onChange={e=>{const n=[...stage.transports];n[i]={...r,transportId:e.target.value};update('transports',n);}} placeholder="transportId"/><input value={r.name} onChange={e=>{const n=[...stage.transports];n[i]={...r,name:e.target.value};update('transports',n);}} placeholder="name"/><input type="number" value={r.capacity} onChange={e=>{const n=[...stage.transports];n[i]={...r,capacity:Number(e.target.value)};update('transports',n);}} placeholder="capacity"/><input value={r.startLocation} onChange={e=>{const n=[...stage.transports];n[i]={...r,startLocation:e.target.value};update('transports',n);}} placeholder="startLocation"/><button onClick={()=>update('transports',stage.transports.filter((_,x)=>x!==i))}>削除</button></div>)}
  </section>;
};
