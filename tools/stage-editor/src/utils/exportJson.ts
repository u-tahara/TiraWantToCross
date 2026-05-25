import { StageData } from '../types/stage';

export const downloadJson = (stage: StageData): void => {
  const blob = new Blob([JSON.stringify(stage, null, 2)], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `${stage.stageId}.json`;
  a.click();
  URL.revokeObjectURL(url);
};
