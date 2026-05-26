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
    if (!Number.isFinite(s.schemaVersion)) issues.push({ stageId: s.stageId, path: 'schemaVersion', message: 'schemaVersionは数値である必要があります' });
    if (!Number.isFinite(s.optimalMoves)) issues.push({ stageId: s.stageId, path: 'optimalMoves', message: 'optimalMovesは数値である必要があります' });
    if (s.schemaVersion < 1) issues.push({ stageId: s.stageId, path: 'schemaVersion', message: 'schemaVersionは1以上が必要です' });
    if (s.optimalMoves < 1) issues.push({ stageId: s.stageId, path: 'optimalMoves', message: 'optimalMovesは1以上が必要です' });
    const capacityCandidates = [s.boat?.capacity, s.boat?.maxPassengers, s.maxPassengers, s.capacity]
      .filter((v): v is number => typeof v === 'number');
    const hasValidBoatCapacity = capacityCandidates.some((v) => v >= 1);
    if (!hasValidBoatCapacity) {
      issues.push({ stageId: s.stageId, path: 'boat.capacity', message: 'boatの定員は1以上が必要です' });
    }
    if (!s.boat.startLocation) issues.push({ stageId: s.stageId, path: 'boat.startLocation', message: 'boat.startLocationは必須です' });

    const locationIds = s.locations.map((l) => l.locationId);
    if (s.boat.startLocation && !locationIds.includes(s.boat.startLocation)) {
      issues.push({ stageId: s.stageId, path: 'boat.startLocation', message: 'boat.startLocationが不正です' });
    }
    const routeIds = s.routes.map((r) => r.routeId);
    const entityIds = s.entities.map((e) => e.entityId);

    dup(locationIds).forEach((id) => issues.push({ stageId: s.stageId, path: 'locations', message: `locationId重複: ${id}` }));
    dup(routeIds).forEach((id) => issues.push({ stageId: s.stageId, path: 'routes', message: `routeId重複: ${id}` }));
    dup(entityIds).forEach((id) => issues.push({ stageId: s.stageId, path: 'entities', message: `entityId重複: ${id}` }));

    s.locations.forEach((l, i) => { if (!l.locationId) issues.push({ stageId: s.stageId, path: `locations[${i}]`, message: 'locationIdは必須です' }); });
    s.routes.forEach((r, i) => {
      if (!r.routeId) issues.push({ stageId: s.stageId, path: `routes[${i}]`, message: 'routeIdは必須です' });
      if (!locationIds.includes(r.from)) issues.push({ stageId: s.stageId, path: `routes[${i}]`, message: 'fromが不正です' });
      if (!locationIds.includes(r.to)) issues.push({ stageId: s.stageId, path: `routes[${i}]`, message: 'toが不正です' });
      if (r.from && r.from === r.to) issues.push({ stageId: s.stageId, path: `routes[${i}]`, message: 'from/toは同一にできません' });
    });
    s.entities.forEach((e, i) => {
      if (!e.entityId) issues.push({ stageId: s.stageId, path: `entities[${i}]`, message: 'entityIdは必須です' });
      if (!locationIds.includes(e.startLocation)) issues.push({ stageId: s.stageId, path: `entities[${i}]`, message: 'startLocationが不正です' });
    });

    s.clearConditions.forEach((c, i) => {
      if (c.conditionType === 'all_entities_at_location' && !locationIds.includes(c.targetLocationId)) {
        issues.push({ stageId: s.stageId, path: `clearConditions[${i}]`, message: 'targetLocationIdが不正です' });
      }
    });
  });

  return issues;
};
