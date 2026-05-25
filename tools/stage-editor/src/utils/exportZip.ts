import JSZip from 'jszip';
import { StageData } from '../types/stage';

export const downloadUnityZip = async (stages: StageData[], appVersion: string): Promise<void> => {
  const zip = new JSZip();
  const folder = zip.folder('Assets/Data/Stages');
  if (!folder) throw new Error('ZIP作成失敗');

  stages.forEach((s) => folder.file(`${s.stageId}.json`, JSON.stringify(s, null, 2)));
  folder.file('stages_manifest.json', JSON.stringify({
    exportedAt: new Date().toISOString(),
    appVersion,
    stageCount: stages.length,
    stages: stages.map((s) => ({
      stageId: s.stageId,
      title: s.title,
      fileName: `${s.stageId}.json`,
      theme: s.theme,
      difficulty: s.difficulty,
      optimalMoveCount: s.optimalMoveCount,
      appVersionAdded: s.appVersionAdded
    }))
  }, null, 2));

  const blob = await zip.generateAsync({ type: 'blob' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `TiraWantToCross_Stages_v${appVersion}.zip`;
  a.click();
  URL.revokeObjectURL(url);
};
