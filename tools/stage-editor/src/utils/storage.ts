import { StageData } from '../types/stage';

const STORAGE_KEY = 'tira-stage-editor/stages';

export const loadStages = (): StageData[] => {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) return [];
  try {
    const parsed = JSON.parse(raw) as StageData[];
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
};

export const saveStages = (stages: StageData[]): void => {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(stages));
};
