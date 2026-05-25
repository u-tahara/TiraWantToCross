import { StageData, ValidationIssue } from '../types/stage';

const FILE_SAFE = /^[a-zA-Z0-9_-]+$/;
const dup = (arr: string[]) => arr.filter((v, i) => v && arr.indexOf(v) !== i);

export const validateStages = (stages: StageData[]): ValidationIssue[] => {
  const issues: ValidationIssue[] = [];
  const stageIds = stages.map((s) => s.stageId);
  const duplicatedStageIds = new Set(dup(stageIds));

  stages.forEach((s) => {
    if (!s.stageId) issues.push({ stageId: s.stageId || '(empty)', path: 'stageId', message: 'stageIdは必須です' });
    if (duplicatedStageIds.has(s.stageId)) issues.push({ stageId: s.stageId, path: 'stageId', message: 'stageIdが重複しています' });
    if (s.stageId && !FILE_SAFE.test(s.stageId)) issues.push({ stageId: s.stageId, path: 'stageId', message: 'stageIdに不正な文字が含まれます' });
    if (!s.title) issues.push({ stageId: s.stageId, path: 'title', message: 'titleは必須です' });
    if (s.schemaVersion < 1) issues.push({ stageId: s.stageId, path: 'schemaVersion', message: 'schemaVersionは1以上が必要です' });
    if (s.optimalMoveCount < 1) issues.push({ stageId: s.stageId, path: 'optimalMoveCount', message: 'optimalMoveCountは1以上が必要です' });

    const locationIds = s.locations.map((l) => l.locationId);
    const entityIds = s.entities.map((e) => e.entityId);
    const transportIds = s.transports.map((t) => t.transportId);

    dup(locationIds).forEach((id) => issues.push({ stageId: s.stageId, path: 'locations', message: `locationId重複: ${id}` }));
    dup(entityIds).forEach((id) => issues.push({ stageId: s.stageId, path: 'entities', message: `entityId重複: ${id}` }));
    dup(transportIds).forEach((id) => issues.push({ stageId: s.stageId, path: 'transports', message: `transportId重複: ${id}` }));

    s.locations.forEach((l, i) => { if (!l.locationId) issues.push({ stageId: s.stageId, path: `locations[${i}]`, message: 'locationIdは必須です' }); });
    s.transports.forEach((t, i) => {
      if (!t.transportId) issues.push({ stageId: s.stageId, path: `transports[${i}]`, message: 'transportIdは必須です' });
      if (t.capacity < 1) issues.push({ stageId: s.stageId, path: `transports[${i}]`, message: 'capacityは1以上が必要です' });
      if (!locationIds.includes(t.startLocation)) issues.push({ stageId: s.stageId, path: `transports[${i}]`, message: 'startLocationが不正です' });
    });
    s.routes.forEach((r, i) => {
      if (!r.routeId) issues.push({ stageId: s.stageId, path: `routes[${i}]`, message: 'routeIdは必須です' });
      if (!locationIds.includes(r.from)) issues.push({ stageId: s.stageId, path: `routes[${i}]`, message: 'fromが不正です' });
      if (!locationIds.includes(r.to)) issues.push({ stageId: s.stageId, path: `routes[${i}]`, message: 'toが不正です' });
      if (r.from && r.from === r.to) issues.push({ stageId: s.stageId, path: `routes[${i}]`, message: 'from/toは同一にできません' });
      if (!transportIds.includes(r.transportId)) issues.push({ stageId: s.stageId, path: `routes[${i}]`, message: 'transportId参照が不正です' });
    });
    s.entities.forEach((e, i) => {
      if (!e.entityId) issues.push({ stageId: s.stageId, path: `entities[${i}]`, message: 'entityIdは必須です' });
      if (!locationIds.includes(e.startLocation)) issues.push({ stageId: s.stageId, path: `entities[${i}]`, message: 'startLocationが不正です' });
      if (!locationIds.includes(e.goalLocation)) issues.push({ stageId: s.stageId, path: `entities[${i}]`, message: 'goalLocationが不正です' });
    });
  });

  return issues;
};
