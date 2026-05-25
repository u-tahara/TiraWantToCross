export type Location = { locationId: string; name: string; type: string };
export type Route = { routeId: string; from: string; to: string; transportId: string };
export type Entity = {
  entityId: string;
  name: string;
  type: string;
  startLocation: string;
  goalLocation: string;
  canOperateBoat: boolean;
};
export type Transport = {
  transportId: string;
  name: string;
  capacity: number;
  startLocation: string;
};

export type StageRule = {
  forbiddenPairs: string[][];
  operableEntityIds: string[];
  customGoals: Array<{ entityId: string; goalLocation: string }>;
};

export type StageData = {
  stageId: string;
  title: string;
  description: string;
  schemaVersion: number;
  appVersionAdded: string;
  theme: string;
  difficulty: string;
  optimalMoveCount: number;
  locations: Location[];
  routes: Route[];
  entities: Entity[];
  transports: Transport[];
  rules: StageRule;
  goal: { allAtLocation: string; perEntityGoals: Array<{ entityId: string; goalLocation: string }> };
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
  optimalMoveCount: 1,
  locations: [],
  routes: [],
  entities: [],
  transports: [],
  rules: { forbiddenPairs: [], operableEntityIds: [], customGoals: [] },
  goal: { allAtLocation: '', perEntityGoals: [] }
});
