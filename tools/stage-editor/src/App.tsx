import { useEffect, useMemo, useState } from 'react';
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
const FILE_SAFE = /^[a-zA-Z0-9_-]+$/;
const NAVIGATION_ERROR_PREFIX = '対象ステージを特定できませんでした';

const resolveStageFromIssue = (
  issue: { stageId: string; stageIndex?: number },
  stages: StageData[],
  stagesByStageId: Map<string, StageData[]>,
) => {
  const normalizedStageId = issue.stageId === '(empty)' ? '' : issue.stageId;
  const sameStageIdStages = stagesByStageId.get(normalizedStageId) ?? [];
  const targetByStageId = sameStageIdStages.length === 1 ? sameStageIdStages[0] : null;
  const stageAtIndex =
    typeof issue.stageIndex === 'number' && issue.stageIndex >= 0 && issue.stageIndex < stages.length
      ? stages[issue.stageIndex]
      : null;
  const targetByIndex = stageAtIndex !== null && stageAtIndex.stageId === normalizedStageId
    ? stageAtIndex
    : null;
  return targetByStageId ?? targetByIndex;
};

function App() {
  const [stages, setStages] = useState<StageData[]>(() => loadStages());
  const [editingStage, setEditingStage] = useState<StageData | null>(stages.length > 0 ? stages[0] : null);
  const [editingStageHint, setEditingStageHint] = useState<{ stageId: string; duplicateOrder: number } | null>(
    stages.length > 0 ? { stageId: stages[0].stageId, duplicateOrder: 0 } : null,
  );
  const [selected, setSelected] = useState<string[]>([]);
  const [query, setQuery] = useState('');
  const [validationNavigationError, setValidationNavigationError] = useState('');
  const [lastFailedIssueId, setLastFailedIssueId] = useState<string | null>(null);
  const issues = useMemo(() => validateStages(stages), [stages]);
  const invalidStageIds = useMemo(() => new Set(issues.map((i) => i.stageId)), [issues]);
  const stagesByStageId = useMemo(() => {
    const map = new Map<string, StageData[]>();
    stages.forEach((stage) => {
      const list = map.get(stage.stageId);
      if (list) {
        list.push(stage);
      } else {
        map.set(stage.stageId, [stage]);
      }
    });
    return map;
  }, [stages]);

  const updateStages = (next: StageData[]) => { setStages(next); saveStages(next); };
  const editing = editingStage;
  const filtered = stages.filter((s) => [s.stageId, s.title, s.theme, s.difficulty].join(' ').toLowerCase().includes(query.toLowerCase()));

  const toStageHint = (target: StageData, all: StageData[]) => {
    let duplicateOrder = 0;
    for (let i = 0; i < all.length; i += 1) {
      const stage = all[i];
      if (stage.stageId !== target.stageId) continue;
      if (stage === target) break;
      duplicateOrder += 1;
    }
    return { stageId: target.stageId, duplicateOrder };
  };

  const isSameStageHint = (
    a: { stageId: string; duplicateOrder: number } | null,
    b: { stageId: string; duplicateOrder: number } | null,
  ) => a?.stageId === b?.stageId && a?.duplicateOrder === b?.duplicateOrder;

  useEffect(() => {
    if (!validationNavigationError) return;
    setValidationNavigationError('');
    setLastFailedIssueId(null);
  }, [editingStage, query]);

  useEffect(() => {
    if (!lastFailedIssueId) return;
    if (issues.some((issue) => issue.issueId === lastFailedIssueId)) return;
    setValidationNavigationError('');
    setLastFailedIssueId(null);
  }, [issues, lastFailedIssueId]);

  useEffect(() => {
    if (editingStage !== null) {
      const sameRef = stages.find((s) => s === editingStage);
      if (sameRef) {
        const nextHint = toStageHint(sameRef, stages);
        if (!isSameStageHint(editingStageHint, nextHint)) {
          setEditingStageHint(nextHint);
        }
        return;
      }
    }

    if (editingStageHint !== null) {
      const sameIdStages = stages.filter((s) => s.stageId === editingStageHint.stageId);
      if (sameIdStages.length > 0) {
        const resolved = sameIdStages[Math.min(editingStageHint.duplicateOrder, sameIdStages.length - 1)];
        setEditingStage(resolved);
        setEditingStageHint(toStageHint(resolved, stages));
        return;
      }
    }

    const fallback = stages.length > 0 ? stages[0] : null;
    setEditingStage(fallback);
    setEditingStageHint(fallback ? toStageHint(fallback, stages) : null);
  }, [editingStage, editingStageHint, stages]);

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
    if (editingStage === null) return;
    const editingIndex = stages.findIndex((s) => s === editingStage);
    if (editingIndex < 0) return;
    const next = [...stages];
    next[editingIndex] = st;
    updateStages(next);
    setEditingStage(st);
    setEditingStageHint(toStageHint(st, next));
  };

  const handleRenameStageId = () => {
    if (editingStage === null) return;
    const nextId = prompt('新しいstageIdを入力してください', editingStage.stageId);
    if (nextId === null) return;
    const trimmed = nextId.trim();
    if (!trimmed || trimmed === editingStage.stageId) return;
    if (!FILE_SAFE.test(trimmed)) {
      alert('stageIdには英数字・アンダースコア・ハイフンのみ使用できます');
      return;
    }
    if (stages.some((s) => s.stageId === trimmed)) {
      alert(`stageId「${trimmed}」は既に存在します`);
      return;
    }

    const editingIndex = stages.findIndex((s) => s === editingStage);
    if (editingIndex < 0) return;

    const renamed = { ...editingStage, stageId: trimmed };
    const next = [...stages];
    next[editingIndex] = renamed;
    updateStages(next);
    setEditingStage(renamed);
    setEditingStageHint(toStageHint(renamed, next));
    setSelected((prev) => prev.map((id) => (id === editingStage.stageId ? trimmed : id)));
  };

  return <main className="app-container">
    <section className="card">
      <h1>Stage Editor</h1>
      <p className="helper-text">この画面でできること: ステージ作成・編集、JSON取込、JSON/Unity用ZIP出力、バリデーション確認。</p>
      <p className="helper-text">作業順: 基本情報 → 地点 → ルート → キャラクター → 移動手段 → ルール → バリデーション確認 → 出力。</p>
    </section>
    <section className="card">
    <div className="toolbar">
    <button className="js-new-stage button-primary" onClick={() => { const id = nextId(stages); const n = [...stages, createEmptyStage(id)]; updateStages(n); setEditingStage(n[n.length - 1]); setEditingStageHint(toStageHint(n[n.length - 1], n)); }}>新規ステージ</button>
    <button className="js-rename-stage-id button-secondary" disabled={editing === null} onClick={handleRenameStageId}>stageId変更</button>
    <input className="js-search-stage" placeholder="検索" value={query} onChange={(e) => setQuery(e.target.value)} />
    <input className="js-import-json" type="file" multiple accept="application/json" onChange={async (e) => { await importJson(e.target.files); e.currentTarget.value = ''; }} />
    <button className="js-export-all-zip button-primary" onClick={async () => {
      const appVersion = prompt('appVersionを入力してください', '1.0.0') ?? '1.0.0';
      const hasError = issues.length > 0;
      if (hasError && !confirm('エラーがあります。正常ステージのみ出力しますか？')) return;
      const valid = stages.filter((s) => !invalidStageIds.has(s.stageId));
      if (valid.length === 0) { alert("出力対象の正常ステージがありません"); return; }
      await downloadUnityZip(valid, appVersion);
    }}>全ステージZIP出力</button>
    <button className="js-export-selected-zip button-secondary" onClick={async () => {
      const targets = stages.filter((s) => selected.includes(s.stageId));
      const appVersion = prompt('appVersionを入力してください', '1.0.0') ?? '1.0.0';
      const hasInvalidSelected = targets.some((s) => invalidStageIds.has(s.stageId));
      if (hasInvalidSelected && !confirm('選択ステージにエラーがあります。正常ステージのみ出力しますか？')) return;
      const valid = targets.filter((s) => !invalidStageIds.has(s.stageId));
      if (valid.length === 0) { alert("出力対象の正常ステージがありません"); return; }
      await downloadUnityZip(valid, appVersion);
    }}>選択ステージZIP出力</button>
    </div>
    </section>

    <StageList
      stages={filtered}
      selected={selected}
      setSelected={setSelected}
      onEdit={(stage) => {
        setEditingStage(stage);
        setEditingStageHint(toStageHint(stage, stages));
      }}
      onDuplicate={(src) => {
        const newId = prompt('複製後のstageId', `${src.stageId}_copy`);
        if (!newId) return;
        const trimmed = newId.trim();
        if (!trimmed) return;
        if (!FILE_SAFE.test(trimmed)) {
          alert('stageIdには英数字・アンダースコア・ハイフンのみ使用できます');
          return;
        }
        if (stages.some((s) => s.stageId === trimmed)) {
          alert(`stageId「${trimmed}」は既に存在します`);
          return;
        }
        updateStages([...stages, { ...src, stageId: trimmed }]);
      }}
      onDelete={(stage) => {
        const targetIndex = stages.findIndex((s) => s === stage);
        if (targetIndex < 0) return;
        if (!confirm(`${stage.stageId} を削除しますか？`)) return;
        const next = [...stages];
        next.splice(targetIndex, 1);
        updateStages(next);
        if (editingStage === stage) {
          const fallback = next.length > 0 ? next[0] : null;
          setEditingStage(fallback);
          setEditingStageHint(fallback ? toStageHint(fallback, next) : null);
        }
      }}
    />

    {editing ? <StageEditor stage={editing} onChange={handleStageChange} hasStageErrors={issues.some((i) => resolveStageFromIssue(i, stages, stagesByStageId) === editing)} /> : <p>編集するステージを選択してください。</p>}
    {validationNavigationError ? <p id="js-validation-navigation-error" className="js-validation-navigation-error" role="alert">{validationNavigationError}</p> : null}
    <ValidationPanel
      issues={issues}
      failedIssueId={lastFailedIssueId}
      errorMessageId="js-validation-navigation-error"
      onSelectIssue={(issue) => {
        const target = resolveStageFromIssue(issue, stages, stagesByStageId);
        if (!target) {
          const labelStageId = issue.stageId || '(empty)';
          setValidationNavigationError(`${NAVIGATION_ERROR_PREFIX}: [${labelStageId}] ${issue.path} (${issue.issueId})`);
          setLastFailedIssueId(issue.issueId);
          return;
        }
        setValidationNavigationError('');
        setLastFailedIssueId(null);
        setEditingStage(target);
        setEditingStageHint(toStageHint(target, stages));
        if (document.activeElement instanceof HTMLElement && document.activeElement.classList.contains('js-validation-issue-link')) {
          requestAnimationFrame(() => {
            const firstEditorInput = document.querySelector<HTMLElement>('.js-title')
              ?? document.querySelector<HTMLElement>('.js-stage-id');
            firstEditorInput?.focus();
          });
        }
      }}
    />
  </main>;
}

export default App;
