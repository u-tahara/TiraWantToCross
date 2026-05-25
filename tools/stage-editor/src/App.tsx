import { useMemo, useState } from 'react';
import { StageEditor } from './components/StageEditor';
import { StageList } from './components/StageList';
import { ValidationPanel } from './components/ValidationPanel';
import { createEmptyStage, normalizeStageData, StageData } from './types/stage';
import { downloadUnityZip } from './utils/exportZip';
import { loadStages, saveStages } from './utils/storage';
import { validateStages } from './utils/validation';

const nextId = (stages: StageData[]) => {
  for (let i = 1; i < 10000; i += 1) {
    const id = `stage_${String(i).padStart(3, '0')}`;
    if (!stages.some((s) => s.stageId === id)) return id;
  }
  return `stage_${Date.now()}`;
};

function App() {
  const [stages, setStages] = useState<StageData[]>(() => loadStages());
  const [editingStageId, setEditingStageId] = useState<string | null>(stages.length > 0 ? stages[0].stageId : null);
  const [selected, setSelected] = useState<string[]>([]);
  const [query, setQuery] = useState('');
  const issues = useMemo(() => validateStages(stages), [stages]);
  const invalidStageIds = useMemo(() => new Set(issues.map((i) => i.stageId)), [issues]);

  const updateStages = (next: StageData[]) => { setStages(next); saveStages(next); };
  const editing = editingStageId !== null ? stages.find((s) => s.stageId === editingStageId) ?? null : null;
  const filtered = stages.filter((s) => [s.stageId, s.title, s.theme, s.difficulty].join(' ').toLowerCase().includes(query.toLowerCase()));

  const importJson = async (files: FileList | null) => {
    if (!files) return;
    const imported: StageData[] = [];
    const failedFiles: string[] = [];
    for (const f of Array.from(files)) {
      try {
        const txt = await f.text();
        imported.push(normalizeStageData(JSON.parse(txt), f.name.replace(/\.json$/i, '')));
      } catch {
        failedFiles.push(f.name);
      }
    }
    if (failedFiles.length > 0) {
      alert(`JSONの読み込みに失敗したファイル: ${failedFiles.join(', ')}`);
    }
    const merged = [...stages];
    imported.forEach((s) => {
      const idx = merged.findIndex((x) => x.stageId === s.stageId);
      if (idx >= 0) merged[idx] = s; else merged.push(s);
    });
    updateStages(merged);
  };


  const handleStageChange = (st: StageData) => {
    if (editingStageId === null) return;
    const editingIndex = stages.findIndex((s) => s.stageId === editingStageId);
    if (editingIndex < 0) return;
    const next = [...stages];
    next[editingIndex] = st;
    updateStages(next);
    if (editingStageId !== st.stageId) {
      setEditingStageId(st.stageId);
    }
  };

  return <main>
    <h1>Stage Editor</h1>
    <button className="js-new-stage" onClick={() => { const id = nextId(stages); const n = [...stages, createEmptyStage(id)]; updateStages(n); setEditingStageId(id); }}>新規ステージ</button>
    <input className="js-search-stage" placeholder="検索" value={query} onChange={(e) => setQuery(e.target.value)} />
    <input className="js-import-json" type="file" multiple accept="application/json" onChange={async (e) => { await importJson(e.target.files); e.currentTarget.value = ''; }} />
    <button className="js-export-all-zip" onClick={async () => {
      const appVersion = prompt('appVersionを入力してください', '1.0.0') ?? '1.0.0';
      const hasError = issues.length > 0;
      if (hasError && !confirm('エラーがあります。正常ステージのみ出力しますか？')) return;
      const valid = stages.filter((s) => !invalidStageIds.has(s.stageId));
      await downloadUnityZip(valid, appVersion);
    }}>全ステージZIP出力</button>
    <button className="js-export-selected-zip" onClick={async () => {
      const targets = stages.filter((s) => selected.includes(s.stageId));
      const appVersion = prompt('appVersionを入力してください', '1.0.0') ?? '1.0.0';
      const hasInvalidSelected = targets.some((s) => invalidStageIds.has(s.stageId));
      if (hasInvalidSelected && !confirm('選択ステージにエラーがあります。正常ステージのみ出力しますか？')) return;
      const valid = targets.filter((s) => !invalidStageIds.has(s.stageId));
      await downloadUnityZip(valid, appVersion);
    }}>選択ステージZIP出力</button>

    <StageList
      stages={filtered}
      selected={selected}
      setSelected={setSelected}
      onEdit={(stageId) => {
        if (!stages.some((s) => s.stageId === stageId)) return;
        setEditingStageId(stageId);
      }}
      onDuplicate={(stageId) => {
        const src = stages.find((s) => s.stageId === stageId);
        if (!src) return;
        const newId = prompt('複製後のstageId', `${src.stageId}_copy`);
        if (!newId || stages.some((s) => s.stageId === newId)) return;
        updateStages([...stages, { ...src, stageId: newId }]);
      }}
      onDelete={(stage) => {
        const targetIndex = stages.findIndex((s) => s === stage);
        if (targetIndex < 0) return;
        if (!confirm(`${stage.stageId} を削除しますか？`)) return;
        const next = [...stages];
        next.splice(targetIndex, 1);
        updateStages(next);
        if (editingStageId === stage.stageId) {
          setEditingStageId(next.length > 0 ? next[0].stageId : null);
        }
      }}
    />

    {editing ? <StageEditor stage={editing} onChange={handleStageChange} /> : <p>編集するステージを選択してください。</p>}
    <ValidationPanel issues={issues} />
  </main>;
}

export default App;
