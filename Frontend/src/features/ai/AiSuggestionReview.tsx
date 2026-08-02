import { Sparkles } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { aiSuggestionApi, type AiSuggestionResponse } from '../../api/endpoints';
import { isDemoMode } from '../../api/dataSource';
import { Button } from '../../components/ui/Button';
import { Dialog } from '../../components/ui/Dialog';

const demoSuggestion: AiSuggestionResponse = { id: 7, projectId: 1, status: 'Completed', title: 'Password recovery e-posta teslim hatalarını kök nedeniyle çöz', priority: 'Critical', labels: ['auth', 'incident'], subtasks: ['Bounce ve suppression loglarını 72 saat için çıkar', 'Retry politikasını exponential backoff ile yeniden tanımla', 'Teslim başarı oranı için uyarı eşiği ekle'], dueDate: null, appliedTaskId: null };

export function AiSuggestionReview({ open, projectId, onClose }: { open: boolean; projectId: number; onClose: () => void }) {
  const [prompt, setPrompt] = useState('');
  const [suggestion, setSuggestion] = useState<AiSuggestionResponse | null>(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const poll = useRef<number | null>(null);
  useEffect(() => () => { if (poll.current) window.clearInterval(poll.current); }, []);
  const create = async () => {
    if (prompt.trim().length < 8) { setError('İhtiyacı en az 8 karakterle açıklayın.'); return; }
    setBusy(true); setError('');
    try {
      if (isDemoMode) {
        setSuggestion({ ...demoSuggestion, projectId, status: 'Processing' });
        window.setTimeout(() => setSuggestion({ ...demoSuggestion, projectId }), 1500);
      } else {
        const created = await aiSuggestionApi.create(projectId, prompt.trim());
        setSuggestion(created);
        if (['Pending', 'Processing'].includes(created.status)) poll.current = window.setInterval(async () => { const next = await aiSuggestionApi.get(created.id); setSuggestion(next); if (!['Pending', 'Processing'].includes(next.status) && poll.current) window.clearInterval(poll.current); }, 1500);
      }
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Öneri oluşturulamadı. Hassas ayrıntılar gizlendi.'); }
    finally { setBusy(false); }
  };
  const apply = async () => {
    if (!suggestion || suggestion.status !== 'Completed') return;
    setBusy(true); setError('');
    try { const applied = isDemoMode ? { ...suggestion, status: 'Applied' as const, appliedTaskId: 176 } : await aiSuggestionApi.apply(suggestion.id); setSuggestion(applied); }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Öneri uygulanamadı. Daha önce uygulanmış olabilir.'); }
    finally { setBusy(false); }
  };
  const reset = () => { setSuggestion(null); setPrompt(''); setError(''); };
  const status = suggestion?.status;
  return <Dialog open={open} onClose={onClose} title="AI ile görev tasarla" description="Öneri siz Uygula demeden göreve dönüşmez." size="lg" footer={status === 'Completed' ? <><Button onClick={onClose}>Arka planda kalsın</Button><Button variant="primary" onClick={() => void apply()} disabled={busy}>Öneriyi uygula</Button></> : status === 'Applied' ? <Button variant="primary" onClick={onClose}>Görevlere dön</Button> : status === 'Failed' ? <Button variant="primary" onClick={reset}>Tekrar dene</Button> : <><Button onClick={onClose}>{status === 'Processing' || status === 'Pending' ? 'Arka planda çalışsın' : 'Vazgeç'}</Button>{!status ? <Button variant="primary" onClick={() => void create()} disabled={busy}>Öneri oluştur</Button> : null}</>}><div className="ai-suggestion"><div className="ai-suggestion__status"><Sparkles /><span>{status ?? 'INPUT'}</span></div>{!suggestion ? <><label>İhtiyacı doğal dille anlatın<textarea rows={5} value={prompt} onChange={(event) => { setPrompt(event.target.value); setError(''); }} placeholder="Örn. Password recovery teslim sorunlarını araştır…" /></label><div className="suggestion-chips">{['Password recovery teslim sorunlarını araştır', 'Auth uçlarına rate limit ekle', 'Release checklist’i güncelle'].map((item) => <button key={item} onClick={() => setPrompt(item)}>{item}</button>)}</div></> : null}{['Pending', 'Processing'].includes(status ?? '') ? <div className="ai-processing" role="status"><span /><h3>Proje bağlamı inceleniyor</h3><p>Bu pencereyi kapatabilirsiniz; sonuç bildirimden erişilebilir olur.</p></div> : null}{suggestion && status === 'Completed' ? <div className="suggestion-review"><span className="eyebrow">ÖNERİLEN GÖREV</span><h3>{suggestion.title}</h3><div><span>{suggestion.priority}</span>{suggestion.labels.map((label) => <span key={label}>#{label}</span>)}</div><h4>Alt görevler</h4><ul>{suggestion.subtasks.map((item) => <li key={item}>{item}</li>)}</ul><p>Bu aşamada kalıcı kayıt oluşturulmadı.</p></div> : null}{suggestion && status === 'Applied' ? <div className="ai-applied"><strong>✓ TP-{suggestion.appliedTaskId} oluşturuldu</strong><p>Apply tek kullanımlıktır; aynı öneri yeniden uygulanamaz.</p></div> : null}{error ? <p className="field-error" role="alert">{error}</p> : null}</div></Dialog>;
}
