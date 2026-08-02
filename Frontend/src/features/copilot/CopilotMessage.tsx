import { Copy, RotateCcw } from 'lucide-react';
import { CitationChip } from './CitationChip';

export interface ChatMessage { id: string; role: 'user' | 'assistant'; content: string }

export function CopilotMessage({ message, streaming, onCitation }: { message: ChatMessage; streaming: boolean; onCitation: (kind: 'Task' | 'Note', id: number) => void }) {
  const parts = message.content.split(/(\[(?:Task|Note):\d+\])/g);
  return <article className={`copilot-message copilot-message--${message.role}`}><header><span>{message.role === 'user' ? 'SİZ' : 'COPILOT'}</span><time>şimdi</time></header><div>{parts.map((part, index) => { const match = part.match(/^\[(Task|Note):(\d+)\]$/); return match ? <CitationChip key={index} kind={match[1] as 'Task' | 'Note'} id={Number(match[2])} onClick={() => onCitation(match[1] as 'Task' | 'Note', Number(match[2]))} /> : <span key={index}>{part}</span>; })}{streaming ? <span className="stream-caret" aria-hidden="true" /> : null}</div>{message.role === 'assistant' && !streaming ? <footer><button aria-label="Yanıtı kopyala" onClick={() => void navigator.clipboard.writeText(message.content)}><Copy /></button><button aria-label="Yanıtı yeniden oluştur"><RotateCcw /></button></footer> : null}</article>;
}
