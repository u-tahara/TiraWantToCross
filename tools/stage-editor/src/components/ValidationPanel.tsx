import { ValidationIssue } from '../types/stage';

export const ValidationPanel = ({ issues }: { issues: ValidationIssue[] }) => (
  <section>
    <h3>バリデーション結果</h3>
    {issues.length === 0 ? <p>エラーなし</p> : (
      <ul className="js-validation-list">
        {issues.map((e, i) => <li key={`${e.stageId}-${i}`}>[{e.stageId}] {e.path}: {e.message}</li>)}
      </ul>
    )}
  </section>
);
