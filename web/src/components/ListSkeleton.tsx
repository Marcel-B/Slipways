export function ListSkeleton({ rows = 6 }: { rows?: number }) {
  return (
    <ul className="item-list skeleton-list" role="status" aria-label="Lade…">
      {Array.from({ length: rows }, (_, i) => (
        <li key={i}>
          <div className="item">
            <span className="item-icon skeleton" />
            <span className="item-text">
              <span className="skeleton skeleton-line" />
              <span className="skeleton skeleton-line skeleton-short" />
            </span>
          </div>
        </li>
      ))}
    </ul>
  )
}
