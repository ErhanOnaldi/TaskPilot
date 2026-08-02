import { useEffect, useRef, type PropsWithChildren, type ReactNode } from 'react';
import { X } from 'lucide-react';

export function Dialog({ open, title, description, onClose, children, footer, size = 'md' }: PropsWithChildren<{
  open: boolean;
  title: string;
  description?: string;
  onClose: () => void;
  footer?: ReactNode;
  size?: 'sm' | 'md' | 'lg';
}>) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog ref={ref} className={`dialog dialog--${size}`} onClose={onClose} onCancel={(event) => { event.preventDefault(); onClose(); }}>
      <header className="dialog__header">
        <div><h2>{title}</h2>{description ? <p>{description}</p> : null}</div>
        <button className="icon-button" aria-label="Pencereyi kapat" onClick={onClose}><X /></button>
      </header>
      <div className="dialog__body">{children}</div>
      {footer ? <footer className="dialog__footer">{footer}</footer> : null}
    </dialog>
  );
}
