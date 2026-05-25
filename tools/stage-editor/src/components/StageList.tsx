import { StageData } from '../types/stage';

type Props = {
  stages: StageData[];
  selected: string[];
  setSelected: (ids: string[]) => void;
  onEdit: (stageId: string) => void;
  onDuplicate: (stageId: string) => void;
  onDelete: (stageId: string) => void;
};

export const StageList = ({ stages, selected, setSelected, onEdit, onDuplicate, onDelete }: Props) => {
  const toggle = (id: string) => setSelected(selected.includes(id) ? selected.filter((v) => v !== id) : [...selected, id]);
  return (
    <table className="js-stage-table" border={1} cellPadding={4}>
      <thead><tr><th></th><th>stageId</th><th>title</th><th>theme</th><th>difficulty</th><th>optimalMoves</th><th>appVersionAdded</th><th>操作</th></tr></thead>
      <tbody>
        {stages.map((s, index) => (
          <tr key={`${s.stageId || 'empty'}-${index}`}>
            <td><input className="js-select-stage" type="checkbox" checked={selected.includes(s.stageId)} onChange={() => toggle(s.stageId)} /></td>
            <td>{s.stageId}</td><td>{s.title}</td><td>{s.theme}</td><td>{s.difficulty}</td><td>{s.optimalMoves}</td><td>{s.appVersionAdded}</td>
            <td>
              <button className="js-edit-stage" onClick={() => onEdit(s.stageId)}>編集</button>
              <button className="js-dup-stage" onClick={() => onDuplicate(s.stageId)}>複製</button>
              <button className="js-del-stage" onClick={() => onDelete(s.stageId)}>削除</button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
};
