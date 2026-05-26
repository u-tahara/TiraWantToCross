import { StageData, ValidationIssue } from '../types/stage';

const FILE_SAFE = /^[a-zA-Z0-9_-]+$/;
const dup = (arr: string[]) => arr.filter((v, i) => v && arr.indexOf(v) !== i);

export const validateStages = (stages: StageData[]): ValidationIssue[] => {
  const issues: ValidationIssue[] = [];
  const issueIdCounts = new Map<string, number>();
  const toIssueId = (value: string) => value.replace(/[^a-zA-Z0-9_-]/g, '_');
  const addIssue = (issue: Omit<ValidationIssue, 'issueId'>, issueKey = '') => {
    const baseId = toIssueId(`stage_${issue.stageId}-${issue.path}-${issueKey}`);
    const count = (issueIdCounts.get(baseId) ?? 0) + 1;
    issueIdCounts.set(baseId, count);
    const issueId = count === 1 ? baseId : `${baseId}-${count}`;
    issues.push({ issueId, ...issue });
  };
  const stageIds = stages.map((s) => s.stageId);
  const duplicatedStageIds = new Set(dup(stageIds));

  stages.forEach((s, stageIndex) => {
    if (!s.stageId) addIssue({ stageId: s.stageId || '(empty)', stageIndex, path: 'stageId', message: 'stageIdは必須です' }, 'required');
    if (duplicatedStageIds.has(s.stageId)) addIssue({ stageId: s.stageId, stageIndex, path: 'stageId', message: 'stageIdが重複しています' }, 'duplicate');
    if (s.stageId && !FILE_SAFE.test(s.stageId)) addIssue({ stageId: s.stageId, stageIndex, path: 'stageId', message: 'stageIdに不正な文字が含まれます' }, 'invalid_chars');
    if (!s.title) addIssue({ stageId: s.stageId, stageIndex, path: 'title', message: 'titleは必須です' }, 'required');
    if (!Number.isFinite(s.schemaVersion)) addIssue({ stageId: s.stageId, stageIndex, path: 'schemaVersion', message: 'schemaVersionは数値である必要があります' }, 'not_finite');
    if (!Number.isFinite(s.optimalMoves)) addIssue({ stageId: s.stageId, stageIndex, path: 'optimalMoves', message: 'optimalMovesは数値である必要があります' }, 'not_finite');
    if (s.schemaVersion < 1) addIssue({ stageId: s.stageId, stageIndex, path: 'schemaVersion', message: 'schemaVersionは1以上が必要です' }, 'min_1');
    if (s.optimalMoves < 1) addIssue({ stageId: s.stageId, stageIndex, path: 'optimalMoves', message: 'optimalMovesは1以上が必要です' }, 'min_1');
    const capacityCandidates = [s.boat?.capacity, s.boat?.maxPassengers, s.maxPassengers, s.capacity]
      .filter((v): v is number => typeof v === 'number');
    const hasValidBoatCapacity = capacityCandidates.some((v) => v >= 1);
    if (!hasValidBoatCapacity) {
      addIssue({ stageId: s.stageId, stageIndex, path: 'boat.capacity', message: 'boatの定員は1以上が必要です' }, 'min_1');
    }
    if (!s.boat.startLocation) addIssue({ stageId: s.stageId, stageIndex, path: 'boat.startLocation', message: 'boat.startLocationは必須です' }, 'required');

    const locationIds = s.locations.map((l) => l.locationId);
    if (s.boat.startLocation && !locationIds.includes(s.boat.startLocation)) {
      addIssue({ stageId: s.stageId, stageIndex, path: 'boat.startLocation', message: 'boat.startLocationが不正です' }, 'invalid_ref');
    }
    const routeIds = s.routes.map((r) => r.routeId);
    const entityIds = s.entities.map((e) => e.entityId);

    dup(locationIds).forEach((id) => {
      const normalizedId = toIssueId(id) || 'empty';
      const firstIndex = locationIds.indexOf(id);
      addIssue({ stageId: s.stageId, stageIndex, path: 'locations', message: `locationId重複: ${id}` }, `duplicate_${normalizedId}_${firstIndex}`);
    });
    dup(routeIds).forEach((id) => {
      const normalizedId = toIssueId(id) || 'empty';
      const firstIndex = routeIds.indexOf(id);
      addIssue({ stageId: s.stageId, stageIndex, path: 'routes', message: `routeId重複: ${id}` }, `duplicate_${normalizedId}_${firstIndex}`);
    });
    dup(entityIds).forEach((id) => {
      const normalizedId = toIssueId(id) || 'empty';
      const firstIndex = entityIds.indexOf(id);
      addIssue({ stageId: s.stageId, stageIndex, path: 'entities', message: `entityId重複: ${id}` }, `duplicate_${normalizedId}_${firstIndex}`);
    });

    s.locations.forEach((l, i) => { if (!l.locationId) addIssue({ stageId: s.stageId, stageIndex, path: `locations[${i}]`, message: 'locationIdは必須です' }, 'required_location_id'); });
    s.routes.forEach((r, i) => {
      if (!r.routeId) addIssue({ stageId: s.stageId, stageIndex, path: `routes[${i}]`, message: 'routeIdは必須です' }, 'required_route_id');
      if (!locationIds.includes(r.from)) addIssue({ stageId: s.stageId, stageIndex, path: `routes[${i}]`, message: 'fromが不正です' }, 'invalid_from');
      if (!locationIds.includes(r.to)) addIssue({ stageId: s.stageId, stageIndex, path: `routes[${i}]`, message: 'toが不正です' }, 'invalid_to');
      if (r.from && r.from === r.to) addIssue({ stageId: s.stageId, stageIndex, path: `routes[${i}]`, message: 'from/toは同一にできません' }, 'same_from_to');
    });
    s.entities.forEach((e, i) => {
      if (!e.entityId) addIssue({ stageId: s.stageId, stageIndex, path: `entities[${i}]`, message: 'entityIdは必須です' }, 'required_entity_id');
      if (!locationIds.includes(e.startLocation)) addIssue({ stageId: s.stageId, stageIndex, path: `entities[${i}]`, message: 'startLocationが不正です' }, 'invalid_start_location');
    });

    s.clearConditions.forEach((c, i) => {
      if (c.conditionType === 'all_entities_at_location' && !locationIds.includes(c.targetLocationId)) {
        addIssue({ stageId: s.stageId, stageIndex, path: `clearConditions[${i}]`, message: 'targetLocationIdが不正です' }, 'invalid_target_location');
      }
    });
  });

  return issues;
};
