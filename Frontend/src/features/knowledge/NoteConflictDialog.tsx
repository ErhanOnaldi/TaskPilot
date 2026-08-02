import { useState } from 'react';
import { Button } from '../../components/ui/Button';
import { Dialog } from '../../components/ui/Dialog';

export function NoteConflictDialog({ open, local, server, onClose, onLoadServer }: { open: boolean; local: string; server: string; onClose: () => void; onLoadServer: () => void }) {
  const [compare, setCompare] = useState(false);
  return <Dialog open={open} onClose={onClose} title="Bu not siz düzenlerken güncellendi" description="Otomatik birleştirme yapılmadı. Yerel metniniz korunuyor." size="lg" footer={<><Button onClick={onClose}>Editöre dön</Button><Button onClick={() => void navigator.clipboard.writeText(local)}>Yerel metni kopyala</Button><Button onClick={() => setCompare((value) => !value)}>İki sürümü karşılaştır</Button><Button variant="primary" onClick={onLoadServer}>Sunucudaki sürümü yükle</Button></>} >{compare ? <div className="conflict-compare"><section><h3>Yerel metin</h3><pre>{local}</pre></section><section><h3>Sunucu sürümü</h3><pre>{server}</pre></section></div> : <p className="quiet-copy">409 Conflict nedeniyle kayıt yapılmadı. Devam etmek için bir seçenek belirleyin.</p>}</Dialog>;
}
