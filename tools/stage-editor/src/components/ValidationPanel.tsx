import { useEffect, useRef } from 'react';
import { ValidationIssue } from '../types/stage';

type Props = {
  issues: ValidationIssue[];
  onSelectIssue?: (issue: ValidationIssue) => void;
  failedIssueId?: string | null;
  errorMessageId?: string;
};

export const ValidationPanel = ({ issues, onSelectIssue, failedIssueId = null, errorMessageId }: Props) => {
  const rootRef = useRef<HTMLElement | null>(null);
  useEffect(() => {
    if (!failedIssueId || !rootRef.current) return;
    const node = rootRef.current.querySelector<HTMLElement>(`[data-issue-id="${failedIssueId}"]`);
    node?.scrollIntoView({ block: 'nearest' });
  }, [failedIssueId]);

  return (
    <section ref={rootRef} className="card validation-card">
      <h3>バリデーション結果</h3>
      <p className="helper-text">JSON/ZIP出力前にエラーを確認してください。</p>
      <p><strong>エラー件数: {issues.length}件</strong></p>
      {issues.length === 0 ? <p className="validation-empty">エラーはありません</p> : (
        <ul className="js-validation-list">
          {issues.map((e) => (
            <li key={e.issueId} data-issue-id={e.issueId}>
              {onSelectIssue ? (
                <button
                  className="js-validation-issue-link"
                  type="button"
                  aria-invalid={failedIssueId === e.issueId}
                  aria-describedby={failedIssueId === e.issueId ? errorMessageId : undefined}
                  onClick={() => onSelectIssue(e)}
                >
                  [{e.stageId}] {e.path}: {e.message}
                </button>
              ) : (
                <span className="js-validation-issue-text">[{e.stageId}] {e.path}: {e.message}</span>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
};
