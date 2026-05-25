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

export type ValidationIssue = { stageId: string; path: string; message: string };

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
