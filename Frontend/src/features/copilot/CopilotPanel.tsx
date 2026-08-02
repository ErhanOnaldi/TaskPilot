import { ArrowUp, Bot, Square, X } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { copilotApi } from '../../api/endpoints';
import { isDemoMode } from '../../api/dataSource';
import { Button } from '../../components/ui/Button';
import { CopilotMessage, type ChatMessage } from './CopilotMessage';

const demoAnswer = 'Bu kapsamda iki iş öne çıkıyor. Password recovery e-posta teslim hataları [Task:142] kritik durumda ve devam ediyor. Authentication uçlarına rate limiting [Task:151] ise hâlâ atanmamış. Karar bağlamı [Note:1] Authentication Architecture notunda. Öneri: TP-151 için sahip atayın ve TP-142 üzerinde günlük kontrol noktası tanımlayın.';

export function CopilotPanel({ projectId, projectName }: { projectId: number; projectName: string }) {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [draft, setDraft] = useState('');
  const [streaming, setStreaming] = useState(false);
  const [sessionId, setSessionId] = useState<number | null>(null);
  const [source, setSource] = useState<{ kind: 'Task' | 'Note'; id: number } | null>(null);
  const [completedAnnouncement, setCompletedAnnouncement] = useState('');
  const abort = useRef<AbortController | null>(null);
  const timer = useRef<number | null>(null);
  useEffect(() => () => { abort.current?.abort(); if (timer.current) window.clearInterval(timer.current); }, []);

  const stop = () => { abort.current?.abort(); if (timer.current) window.clearInterval(timer.current); setStreaming(false); };
  const send = async (suggestion?: string) => {
    const text = (suggestion ?? draft).trim();
    if (!text || streaming) return;
    const assistantId = crypto.randomUUID();
    setMessages((current) => [...current, { id: crypto.randomUUID(), role: 'user', content: text }, { id: assistantId, role: 'assistant', content: '' }]);
    setDraft(''); setStreaming(true); setCompletedAnnouncement('');
    if (isDemoMode) {
      let index = 0;
      timer.current = window.setInterval(() => {
        index += 5;
        setMessages((current) => current.map((message) => message.id === assistantId ? { ...message, content: demoAnswer.slice(0, index) } : message));
        if (index >= demoAnswer.length) { if (timer.current) window.clearInterval(timer.current); setStreaming(false); setCompletedAnnouncement('Copilot yanıtı tamamlandı.'); }
      }, 22);
      return;
    }
    abort.current = new AbortController();
    try {
      await copilotApi.streamProject(projectId, text, sessionId, abort.current.signal, (event, data) => {
        if (event === 'session' && data && typeof data === 'object' && 'sessionId' in data) setSessionId(Number(data.sessionId));
        if (event === 'token' && data && typeof data === 'object' && 'token' in data) setMessages((current) => current.map((message) => message.id === assistantId ? { ...message, content: message.content + String(data.token ?? '') } : message));
        if (event === 'completed') { setStreaming(false); setCompletedAnnouncement('Copilot yanıtı tamamlandı.'); }
        if (event === 'error') throw new Error('Copilot yanıtı tamamlanamadı.');
      });
    } catch (error) {
      if (!(error instanceof DOMException && error.name === 'AbortError')) setMessages((current) => current.map((message) => message.id === assistantId ? { ...message, content: 'Yanıt oluşturulamadı. Hassas ayrıntılar gizlendi; yeniden deneyin.' } : message));
      setStreaming(false);
    }
  };
  return <div className="copilot-layout"><section className="copilot-main"><header className="copilot-header"><div className="copilot-mark"><Bot /></div><div><span className="eyebrow">AURORA COPILOT</span><h1>{projectName}</h1><p>Bu projenin görev ve notlarıyla sınırlandırılmış kaynaklı yanıtlar.</p></div></header><div className="copilot-thread">{!messages.length ? <div className="copilot-empty"><Bot /><h2>Proje hakkında ne bilmek istiyorsunuz?</h2><p>Yanıtlar erişiminiz olan görev ve notlarla sınırlandırılır.</p><div>{['En riskli açık görevler hangileri?', 'Password recovery ile ilgili çalışmaları bul.', 'Bu haftayı özetle.'].map((sample) => <button key={sample} onClick={() => void send(sample)}>{sample}</button>)}</div></div> : messages.map((message, index) => <CopilotMessage key={message.id} message={message} streaming={streaming && index === messages.length - 1} onCitation={(kind, id) => setSource({ kind, id })} />)}</div><div className="sr-only" aria-live="polite">{completedAnnouncement}</div><form className="copilot-composer" onSubmit={(event) => { event.preventDefault(); void send(); }}><label className="sr-only" htmlFor="copilot-message">Copilot mesajı</label><textarea id="copilot-message" value={draft} onChange={(event) => setDraft(event.target.value)} placeholder="Bu proje hakkında sor…" rows={3} />{streaming ? <button type="button" className="send-button" onClick={stop} aria-label="Yanıtı durdur"><Square /></button> : <button className="send-button" type="submit" disabled={!draft.trim()} aria-label="Mesajı gönder"><ArrowUp /></button>}<span>Copilot hata yapabilir. Kritik bilgileri kaynaklardan doğrulayın.</span></form></section>{source ? <aside className="source-preview"><header><span className="eyebrow">KAYNAK ÖNİZLEME</span><button className="icon-button" onClick={() => setSource(null)} aria-label="Kaynak önizlemesini kapat"><X /></button></header><span className="source-kind">{source.kind === 'Task' ? `TASK TP-${source.id}` : `NOTE ${source.id}`}</span><h2>{source.kind === 'Task' ? 'Görev kaynağı' : 'Bilgi notu'}</h2><p>Kaynak içeriği ayrı bir panelde açılır; sohbet bağlamı korunur.</p><Button variant="primary">Kaynağı aç</Button></aside> : null}</div>;
}
