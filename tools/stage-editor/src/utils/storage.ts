import { normalizeStageData, StageData } from '../types/stage';

const STORAGE_KEY = 'tira-stage-editor/stages';

export const loadStages = (): StageData[] => {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) return [];
  try {
    const parsed = JSON.parse(raw) as unknown;
    if (!Array.isArray(parsed)) return [];
    return parsed.map((stage, index) => normalizeStageData(stage, `stage_local_${String(index + 1).padStart(3, '0')}`));
  } catch {
    return [];
  }
};

export const saveStages = (stages: StageData[]): void => {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(stages));
};
