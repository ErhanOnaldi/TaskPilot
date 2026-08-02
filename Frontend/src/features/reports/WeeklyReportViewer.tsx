import type { WeeklyReportResponse } from '../../api/endpoints';
import { Button } from '../../components/ui/Button';
import { formatDate } from '../../lib/formatters';

export function WeeklyReportViewer({ report, canReview, onReview }: { report: WeeklyReportResponse; canReview: boolean; onReview: (approve: boolean) => void }) {
  return <article className="report-viewer"><header><div><span className="eyebrow">HAFTALIK RAPOR</span><h2>{formatDate(report.createdAtUtc)}</h2></div><span className={`report-status report-status--${report.status.toLowerCase()}`}>{report.status === 'PendingReview' ? '◈ Review bekliyor' : report.status === 'Approved' ? '● Onaylandı' : '⊘ Reddedildi'}</span></header><div className="markdown-preview">{report.content.split('\n').map((line, index) => line.startsWith('## ') ? <h2 key={index}>{line.slice(3)}</h2> : line.startsWith('# ') ? <h1 key={index}>{line.slice(2)}</h1> : line ? <p key={index}>{line}</p> : <br key={index} />)}</div>{canReview && report.status === 'PendingReview' ? <footer><Button variant="danger" onClick={() => onReview(false)}>Reddet</Button><Button variant="primary" onClick={() => onReview(true)}>Onayla</Button></footer> : null}</article>;
}
