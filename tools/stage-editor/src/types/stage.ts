export type BoatData = {
  capacity: number;
  maxPassengers: number;
  startLocation: string;
};

export type Location = { locationId: string; displayName: string };
export type Route = { routeId: string; from: string; to: string; bidirectional: boolean };
export type Entity = {
  entityId: string;
  displayName: string;
  startLocation: string;
  canOperateBoat: boolean;
  spriteId: string;
};

export type ClearCondition = {
  conditionType: string;
  targetLocationId: string;
};

export type FailCondition = {
  conditionType: string;
  locationId: string;
  entityIds: string[];
  guardianEntityIds: string[];
};

export type StageUiText = {
  objective: string;
  tip: string;
  stageSelectDescription: string;
};

export type StageData = {
  stageId: string;
  title: string;
  description: string;
  schemaVersion: number;
  appVersionAdded: string;
  theme: string;
  difficulty: string;
  optimalMoves: number;
  capacity: number;
  maxPassengers: number;
  boat: BoatData;
  locations: Location[];
  routes: Route[];
  entities: Entity[];
  uiText: StageUiText;
  clearConditions: ClearCondition[];
  failConditions: FailCondition[];
};

export type ValidationIssue = { issueId: string; stageId: string; stageIndex?: number; path: string; message: string };

export const createEmptyStage = (stageId: string): StageData => ({
  stageId,
  title: '',
  description: '',
  schemaVersion: 1,
  appVersionAdded: '1.0.0',
  theme: 'default',
  difficulty: 'normal',
  optimalMoves: 1,
  capacity: 2,
  maxPassengers: 2,
  boat: { capacity: 2, maxPassengers: 2, startLocation: '' },
  locations: [],
  routes: [],
  entities: [],
  uiText: { objective: '', tip: '', stageSelectDescription: '' },
  clearConditions: [{ conditionType: 'all_entities_at_location', targetLocationId: '' }],
  failConditions: []
});

const asString = (v: unknown, fallback = ''): string => (typeof v === 'string' ? v : fallback);
const asNumber = (v: unknown, fallback = 0): number => (typeof v === 'number' && Number.isFinite(v) ? v : fallback);
const asBoolean = (v: unknown, fallback = false): boolean => (typeof v === 'boolean' ? v : fallback);

export const normalizeStageData = (raw: unknown, fallbackStageId?: string): StageData => {
  const src = (raw && typeof raw === 'object') ? (raw as Record<string, unknown>) : {};
  const stageId = asString(src.stageId, fallbackStageId ?? '').trim() || fallbackStageId || 'stage_imported';
  const capacity = Math.max(0, asNumber(src.capacity, 2));
  const maxPassengers = Math.max(0, asNumber(src.maxPassengers, capacity || 2));
  const rawBoat = (src.boat && typeof src.boat === 'object') ? (src.boat as Record<string, unknown>) : {};

  const base = createEmptyStage(stageId);

  return {
    ...base,
    stageId,
    title: asString(src.title),
    description: asString(src.description),
    schemaVersion: Math.max(1, asNumber(src.schemaVersion, 1)),
    appVersionAdded: asString(src.appVersionAdded, '1.0.0'),
    theme: asString(src.theme, 'default'),
    difficulty: asString(src.difficulty, 'normal'),
    optimalMoves: Math.max(1, asNumber(src.optimalMoves, 1)),
    capacity,
    maxPassengers,
    boat: {
      capacity: Math.max(0, asNumber(rawBoat.capacity, capacity)),
      maxPassengers: Math.max(0, asNumber(rawBoat.maxPassengers, maxPassengers)),
      startLocation: asString(rawBoat.startLocation)
    },
    locations: Array.isArray(src.locations)
      ? src.locations.map((v) => {
        const o = (v && typeof v === 'object') ? (v as Record<string, unknown>) : {};
        return { locationId: asString(o.locationId), displayName: asString(o.displayName) };
      })
      : [],
    routes: Array.isArray(src.routes)
      ? src.routes.map((v) => {
        const o = (v && typeof v === 'object') ? (v as Record<string, unknown>) : {};
        return {
          routeId: asString(o.routeId),
          from: asString(o.from),
          to: asString(o.to),
          bidirectional: asBoolean(o.bidirectional)
        };
      })
      : [],
    entities: Array.isArray(src.entities)
      ? src.entities.map((v) => {
        const o = (v && typeof v === 'object') ? (v as Record<string, unknown>) : {};
        return {
          entityId: asString(o.entityId),
          displayName: asString(o.displayName),
          startLocation: asString(o.startLocation),
          canOperateBoat: asBoolean(o.canOperateBoat),
          spriteId: asString(o.spriteId)
        };
      })
      : [],
    uiText: {
      objective: asString((src.uiText as Record<string, unknown> | undefined)?.objective),
      tip: asString((src.uiText as Record<string, unknown> | undefined)?.tip),
      stageSelectDescription: asString((src.uiText as Record<string, unknown> | undefined)?.stageSelectDescription)
    },
    clearConditions: Array.isArray(src.clearConditions)
      ? src.clearConditions.map((v) => {
        const o = (v && typeof v === 'object') ? (v as Record<string, unknown>) : {};
        return { conditionType: asString(o.conditionType), targetLocationId: asString(o.targetLocationId) };
      })
      : base.clearConditions,
    failConditions: Array.isArray(src.failConditions)
      ? src.failConditions.map((v) => {
        const o = (v && typeof v === 'object') ? (v as Record<string, unknown>) : {};
        return {
          conditionType: asString(o.conditionType),
          locationId: asString(o.locationId),
          entityIds: Array.isArray(o.entityIds) ? o.entityIds.map((x) => asString(x)) : [],
          guardianEntityIds: Array.isArray(o.guardianEntityIds) ? o.guardianEntityIds.map((x) => asString(x)) : []
        };
      })
      : []
  };
};
