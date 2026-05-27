import { StageData } from '../types/stage';

type Props = {
  stages: StageData[];
  selected: string[];
  setSelected: (ids: string[]) => void;
  onEdit: (stage: StageData) => void;
  onDuplicate: (stage: StageData) => void;
  onDelete: (stage: StageData) => void;
};

export const StageList = ({ stages, selected, setSelected, onEdit, onDuplicate, onDelete }: Props) => {
  const toggle = (id: string) => setSelected(selected.includes(id) ? selected.filter((v) => v !== id) : [...selected, id]);
  return (
    <section className="card">
      <h2>ステージ一覧</h2>
      <p className="helper-text">編集したいステージを選択します。ZIP出力したい場合はチェックを付けてください。</p>
      <table className="js-stage-table list-table">
        <thead><tr><th></th><th>stageId</th><th>title</th><th>theme</th><th>difficulty</th><th>optimalMoves</th><th>appVersionAdded</th><th>操作</th></tr></thead>
        <tbody>
          {stages.map((s, index) => (
            <tr key={`${s.stageId || 'empty'}-${index}`}>
              <td><input className="js-select-stage" type="checkbox" checked={selected.includes(s.stageId)} onChange={() => toggle(s.stageId)} /></td>
              <td>{s.stageId}</td><td>{s.title}</td><td>{s.theme}</td><td>{s.difficulty}</td><td>{s.optimalMoves}</td><td>{s.appVersionAdded}</td>
              <td className="actions-row">
                <button className="js-edit-stage button-primary" onClick={() => onEdit(s)}>編集</button>
                <button className="js-dup-stage button-secondary" onClick={() => onDuplicate(s)}>複製</button>
                <button className="js-del-stage button-danger" onClick={() => onDelete(s)}>削除</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
};
