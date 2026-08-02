import { Eye, Pencil } from 'lucide-react';
import { useState } from 'react';

function InlineText({ text }: { text: string }) {
  const parts = text.split(/(\[\[[^\]]+\]\])/g);
  return <>{parts.map((part, index) => part.startsWith('[[') ? <button className="wiki-link" key={index}>{part.slice(2, -2).split('|').at(-1)}</button> : <span key={index}>{part}</span>)}</>;
}

function MarkdownPreview({ content }: { content: string }) {
  return <div className="markdown-preview">{content.split('\n').map((line, index) => {
    if (line.startsWith('# ')) return <h1 key={index}><InlineText text={line.slice(2)} /></h1>;
    if (line.startsWith('## ')) return <h2 key={index}><InlineText text={line.slice(3)} /></h2>;
    if (line.startsWith('- ')) return <p className="markdown-list" key={index}>• <InlineText text={line.slice(2)} /></p>;
    if (/^\d+\. /.test(line)) return <p className="markdown-list" key={index}><InlineText text={line} /></p>;
    if (line.startsWith('> ')) return <blockquote key={index}><InlineText text={line.slice(2)} /></blockquote>;
    return line ? <p key={index}><InlineText text={line} /></p> : <br key={index} />;
  })}</div>;
}

export function MarkdownEditor({ value, onChange, readOnly }: { value: string; onChange: (value: string) => void; readOnly: boolean }) {
  const [mode, setMode] = useState<'edit' | 'preview'>('edit');
  return <div className={`markdown-editor${readOnly ? ' markdown-editor--readonly' : ''}`}><div className="editor-tabs"><button className={mode === 'edit' ? 'is-active' : ''} onClick={() => setMode('edit')} disabled={readOnly}><Pencil />Edit</button><button className={mode === 'preview' || readOnly ? 'is-active' : ''} onClick={() => setMode('preview')}><Eye />Preview</button></div>{!readOnly ? <textarea className={mode === 'preview' ? 'is-mobile-hidden' : ''} aria-label="Markdown not içeriği" value={value} onChange={(event) => onChange(event.target.value)} spellCheck="true" /> : null}<div className={`markdown-editor__preview${mode === 'edit' && !readOnly ? ' is-mobile-hidden' : ''}`}><MarkdownPreview content={value} /></div></div>;
}
