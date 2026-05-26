import { useEffect, useRef } from 'react';
import { ValidationIssue } from '../types/stage';

type Props = {
  issues: ValidationIssue[];
  onSelectIssue?: (issue: ValidationIssue) => void;
  failedIssueId?: string | null;
  errorMessageId?: string;
};

const focusFailedIssue = (root: HTMLElement, failedIssueId: string, lastFocusedFailedIdRef: { current: string | null }) => {
  const escapedIssueId = typeof CSS !== 'undefined' && typeof CSS.escape === 'function'
    ? CSS.escape(failedIssueId)
    : failedIssueId.replace(/"/g, '\\"');
  const failedItem = root.querySelector<HTMLElement>(`[data-issue-id="${escapedIssueId}"]`);
  if (!failedItem) return;
  const button = failedItem.querySelector<HTMLButtonElement>('.js-validation-issue-link');
  if (!button) return;
  if (!button.isConnected) return;
  if (document.activeElement === button) {
    lastFocusedFailedIdRef.current = failedIssueId;
    return;
  }
  failedItem.scrollIntoView({ block: 'nearest' });
  button.focus();
  lastFocusedFailedIdRef.current = failedIssueId;
};

export const ValidationPanel = ({ issues, onSelectIssue, failedIssueId = null, errorMessageId }: Props) => {
  const rootRef = useRef<HTMLElement | null>(null);
  const lastFocusedFailedIdRef = useRef<string | null>(null);

  useEffect(() => {
    if (!failedIssueId) {
      lastFocusedFailedIdRef.current = null;
      return;
    }
    if (lastFocusedFailedIdRef.current === failedIssueId) return;
    const rafId = requestAnimationFrame(() => {
      const root = rootRef.current;
      if (!root) return;
      focusFailedIssue(root, failedIssueId, lastFocusedFailedIdRef);
    });
    return () => cancelAnimationFrame(rafId);
  }, [failedIssueId]);

  return (
    <section ref={rootRef}>
      <h3>バリデーション結果</h3>
      {issues.length === 0 ? <p>エラーなし</p> : (
        <ul className="js-validation-list">
          {issues.map((e) => (
            <li
              key={e.issueId}
              data-issue-id={e.issueId}
              className={failedIssueId === e.issueId ? 'js-validation-issue-failed' : undefined}
            >
              {onSelectIssue ? (
                <button
                  className="js-validation-issue-link"
                  type="button"
                  aria-invalid={failedIssueId === e.issueId}
                  aria-describedby={failedIssueId === e.issueId ? errorMessageId : undefined}
                  aria-label={`ステージ${e.stageId}のエラーへ移動: ${e.path}`}
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
