import {
  ArrowRight,
  Check,
  ChevronRight,
  Menu,
  Moon,
  Network,
  Sparkles,
  Sun,
  X,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAppContext } from '../../app/AppProviders';
import './landing.css';

type SurfaceId = 'tasks' | 'notes' | 'ai';

const surfaces: Record<SurfaceId, { label: string; title: string; body: string; bullets: string[] }> = {
  tasks: {
    label: 'Görev takibi',
    title: 'Klavyeyle akan issue takibi',
    body: 'Liste ve Kanban aynı veriyi gösterir. Durum geçişleri kural tabanlıdır; geçersiz hedefler daha bırakmadan anlaşılır.',
    bullets: [
      'Todo → Devam → İnceleme → Bitti akışında yalnız geçerli geçişler',
      'Liste, Kanban ve görev detayları aynı backend sözleşmesini kullanır',
      'Her görev, kararını içeren nota iki yönlü bağlanabilir',
    ],
  },
  notes: {
    label: 'Bilgi merkezi',
    title: 'Markdown yazın, bağlantı kalsın',
    body: 'Bölünmüş editör, wiki-link yaklaşımı ve geri bağlantı paneli kararların görevlerden kopmasını önler.',
    bullets: [
      'Paralel düzenlemede çakışma sessizce üzerine yazılmaz',
      'Her not numeric sürüm bilgisiyle güvenli şekilde kaydedilir',
      'Bilgi grafiği bağlantılı kararları ve görev bağlamını görünür kılar',
    ],
  },
  ai: {
    label: 'AI Copilot',
    title: 'Kaynak göstermeyen yanıt yok',
    body: 'Copilot çalışma alanındaki görev ve not bağlamını kullanır; yanıtlarını tıklanabilir kaynaklarla destekler.',
    bullets: [
      'Akış hâlinde yanıt ve numaralı kaynaklar',
      'AI önerileri kullanıcı onayı olmadan göreve dönüşmez',
      'Cloudflare Workers AI ile semantik arama ve bağlamlı yanıt',
    ],
  },
};

const heroTasks = [
  { id: 'ATL-118', title: 'Ödeme akışı retry mantığını yeniden yaz', status: 'İNCELEME', tone: 'warning', who: 'DK' },
  { id: 'ATL-124', title: 'Grafik görünümünde küme renklendirmesi', status: 'TODO', tone: 'muted', who: 'SE' },
  { id: 'ATL-131', title: 'Çakışma çözümü karşılaştırma görünümü', status: 'DEVAM', tone: 'info', who: 'MY' },
  { id: 'ATL-097', title: 'Haftalık rapor onay akışı', status: 'BİTTİ', tone: 'success', who: 'AK' },
  { id: 'ATL-142', title: 'Copilot alıntı numaralandırması', status: 'DEVAM', tone: 'info', who: 'SE' },
];

const kanban = [
  { name: 'Todo', color: 'var(--text-3)', cards: [['Grafik küme renklendirmesi', 'ATL-124'], ['Konuk rolü okuma rozetleri', 'ATL-140']] },
  { name: 'Devam', color: 'var(--info)', cards: [['Çakışma çözümü modalı', 'ATL-131'], ['Copilot alıntı numaraları', 'ATL-142']] },
  { name: 'İnceleme', color: 'var(--warning)', cards: [['Ödeme akışı retry mantığı', 'ATL-118']] },
];

const roles = [
  { name: 'Owner', color: 'var(--primary)', description: 'Çalışma alanının sahibi; ayarlar, üyeler ve tüm projeler.', permissions: ['Tüm projeler ve ayarlar', 'Rol ve üyelik yönetimi', 'Arşivden geri alma'] },
  { name: 'Manager', color: 'var(--ai)', description: 'Projeyi yürütür, üye davet eder ve ekip akışını yönetir.', permissions: ['Görev atama ve etiket yönetimi', 'Proje oluşturma', 'Haftalık rapor onayı'] },
  { name: 'Member', color: 'var(--success)', description: 'Günlük işini yönetir, görev ve not üretir.', permissions: ['Görev ve not oluşturma', 'Kendi görevini ilerletme', 'Copilot bağlamını kullanma'] },
  { name: 'Guest', color: 'var(--text-3)', description: 'Davet edildiği projede sınırlı, güvenli erişim.', permissions: ['Proje içeriğini görüntüleme', 'Yorumları takip etme', 'Mutation araçları gizli'] },
];

const faqs = [
  { question: 'TaskPilot bugün hangi verileri birlikte tutuyor?', answer: 'Projeler, görevler, bağlantılı notlar, bilgi grafiği, bildirimler, AI önerileri ve kaynaklı Copilot konuşmaları aynı çalışma alanı bağlamında tutulur.' },
  { question: 'Copilot cevaplarını neye dayandırıyor?', answer: 'Copilot ilgili proje veya çalışma alanındaki görev ve notları semantik olarak arar. Yanıtta kullanılan kaynaklar görev ve not referansları olarak kullanıcıya gösterilir.' },
  { question: 'Konuk kullanıcılar ne yapabilir?', answer: 'Guest rolü okuma odaklıdır. Yönetim, atama ve düzenleme aksiyonları arayüzde gösterilmez; backend de aynı izinleri ayrıca doğrular.' },
];

function ProductPreview({ surface }: { surface: SurfaceId }) {
  if (surface === 'tasks') {
    return <div className="landing-kanban">{kanban.map((column) => <section key={column.name}><header><span style={{ background: column.color }} />{column.name}<small>{column.cards.length}</small></header>{column.cards.map(([title, id]) => <article key={id}><strong>{title}</strong><footer><code>{id}</code><span>TP</span></footer></article>)}</section>)}</div>;
  }
  if (surface === 'notes') {
    return <div className="landing-note-preview"><div className="landing-note-editor"><code># Ödeme Retry RFC</code><p>Retry politikası <mark>[[ATL-118]]</mark> görevine bağlıdır.</p><p>Karar kaydı: <mark>[[Mimari Konsey]]</mark></p><div className="landing-autocomplete">[[öde<span>|</span><small>Ödeme RFC<br />Ödeme Sağlayıcı Karşılaştırma</small></div></div><div className="landing-note-render"><h4>Ödeme Retry RFC</h4><p>Retry politikası <a href="#grafik">ATL-118</a> görevine bağlıdır. Karar kaydı: <a href="#grafik">Mimari Konsey</a></p><hr /><code>GERİ BAĞLANTILAR · 3</code><ul><li>Sprint 24 Planı</li><li>Mimari Konsey</li><li>ATL-118 görev notu</li></ul></div></div>;
  }
  return <div className="landing-ai-preview"><div className="landing-user-message">Sprint 24 neden gecikiyor?</div><div className="landing-ai-answer"><span><Sparkles /></span><p>İncelemede bekleyen üç görevin ikisi aynı kişiye atanmış. Retry kararı <a href="#grafik">Ödeme RFC</a> onayına bağlı. Tahmini kayma: <strong>2,5 gün</strong>.</p></div><div className="landing-citations"><span>1 · Ödeme RFC</span><span>2 · ATL-118</span><span>3 · Sprint 24</span></div><aside><code>ÖNERİLEN AKSİYON</code><p>ATL-118 için incelemeye bir günlük SLA ekle.</p><span className="landing-preview-action">İncele</span></aside></div>;
}

function HeroProduct() {
  return <div className="landing-product" aria-label="TaskPilot ürün arayüzü önizlemesi">
    <header><i /><i /><i /><code>atlas-web-platform · görevler</code><kbd>⌘K</kbd></header>
    <div className="landing-product-body">
      <aside className="landing-workspace-rail"><b>N</b><span>AC</span><span>+</span></aside>
      <aside className="landing-product-nav"><code>ÇALIŞMA ALANI</code><span>◇ Bugün</span><strong>≡ Görevler <small>24</small></strong><span>▤ Bilgi merkezi</span><span className="ai">✦ Copilot</span><code>PROJELER</code><span>▪ Atlas Web Platform</span><span>▪ Ödeme Altyapısı</span></aside>
      <section className="landing-product-list"><header><strong>Sprint 24 · Aktif</strong><code>LİSTE</code><small>Sıralama: öncelik</small></header>{heroTasks.map((task) => <article key={task.id}><i className={task.tone}>●</i><code>{task.id}</code><span>{task.title}</span><small>{task.status}</small><b>{task.who}</b></article>)}</section>
      <aside className="landing-product-copilot"><header><span>✦</span><strong>Copilot</strong></header><p>Sprint 24’te <strong>3 görev</strong> incelemede bekliyor. En riskli olan:</p><article><strong>ATL-118 · Ödeme akışı retry mantığı</strong><small>4 gündür incelemede</small></article><div><span>↗ Ödeme RFC</span><span>↗ ATL-118</span></div><div className="landing-copilot-prompt">Copilot’a sor <i>|</i></div></aside>
    </div>
  </div>;
}

/** Fades sections in as they enter the viewport instead of snapping them into place. */
function useRevealOnScroll() {
  useEffect(() => {
    const targets = Array.from(document.querySelectorAll<HTMLElement>('.landing-page [data-reveal]'));
    const reveal = (target: HTMLElement) => { target.dataset.reveal = 'in'; };
    if (!('IntersectionObserver' in window)) {
      targets.forEach(reveal);
      return;
    }
    const observer = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (!entry.isIntersecting) return;
        reveal(entry.target as HTMLElement);
        observer.unobserve(entry.target);
      });
    }, { rootMargin: '0px 0px -10% 0px', threshold: 0.06 });
    targets.forEach((target) => observer.observe(target));
    return () => observer.disconnect();
  }, []);
}

export function LandingPage() {
  const { theme, toggleTheme } = useAppContext();
  useRevealOnScroll();
  const [surface, setSurface] = useState<SurfaceId>('tasks');
  const [openFaq, setOpenFaq] = useState(0);
  const [menuOpen, setMenuOpen] = useState(false);

  return <div className="landing-page">
    <header className="landing-header">
      <div className="landing-container landing-header-inner">
        <a className="landing-brand" href="#top" aria-label="TaskPilot ana sayfa"><img className="brand-mark-image" src="/brand-mark.png" alt="" width="28" height="28" loading="eager" decoding="async" /><strong>TaskPilot</strong></a>
        <nav className={menuOpen ? 'is-open' : ''} aria-label="Ana navigasyon">
          <a href="#yuzeyler" onClick={() => setMenuOpen(false)}>Ürün</a><a href="#grafik" onClick={() => setMenuOpen(false)}>Bilgi grafiği</a><a href="#roller" onClick={() => setMenuOpen(false)}>Roller</a><a href="#sss" onClick={() => setMenuOpen(false)}>SSS</a>
        </nav>
        <div className="landing-header-actions">
          <button className="landing-icon-button" type="button" onClick={toggleTheme} aria-label={theme === 'dark' ? 'Açık temaya geç' : 'Koyu temaya geç'}>{theme === 'dark' ? <Sun /> : <Moon />}</button>
          <Link className="landing-login" to="/login">Giriş yap</Link>
          <Link className="landing-primary landing-header-cta" to="/register">Ücretsiz başla</Link>
          <button className="landing-menu-button" type="button" onClick={() => setMenuOpen((value) => !value)} aria-label={menuOpen ? 'Menüyü kapat' : 'Menüyü aç'} aria-expanded={menuOpen}>{menuOpen ? <X /> : <Menu />}</button>
        </div>
      </div>
    </header>

    <main>
      <section id="top" className="landing-hero landing-container">
        <h1 data-reveal="" style={{ transitionDelay: '70ms' }}>Görevleriniz ve bilginiz<br /><span>aynı grafikte yaşasın.</span></h1>
        <p data-reveal="" style={{ transitionDelay: '140ms' }}>TaskPilot; issue takibini, bağlantılı notları ve kaynak gösteren AI copilot’ı tek çalışma alanında birleştirir.</p>
        <div className="landing-hero-actions" data-reveal="" style={{ transitionDelay: '210ms' }}><Link className="landing-primary" to="/register">Ücretsiz başla <ArrowRight /></Link><a className="landing-secondary" href="#yuzeyler">Ürünü keşfet</a></div>
        <code className="landing-hero-note" data-reveal="" style={{ transitionDelay: '260ms' }}>Kredi kartı yok · ücretsiz beta · gerçek backend bağlantısı</code>
        <div data-reveal="" style={{ transitionDelay: '320ms' }}><HeroProduct /></div>
      </section>

      <section className="landing-stats landing-container" aria-label="TaskPilot öne çıkanlar">
        {[['3 yüzey', 'Görev, bilgi ve AI aynı çalışma alanında'], ['PostgreSQL', 'Kalıcı ve ilişkisel proje verisi'], ['%100', 'Copilot yanıtlarında kaynak hedefi'], ['4 rol', 'Owner, Manager, Member ve Guest']].map(([value, label], index) => <article key={value} data-reveal="" style={{ transitionDelay: `${index * 70}ms` }}><strong>{value}</strong><span>{label}</span></article>)}
      </section>

      <section id="yuzeyler" className="landing-section landing-container">
        <code className="landing-eyebrow" data-reveal="">ÜÇ YÜZEY · TEK ZİHİN</code><h2 data-reveal="">Araç değiştirmek, bağlamı kaybetmektir.</h2><p className="landing-lead" data-reveal="">Görev, not ve yapay zekâ aynı veri modelini paylaşır. Proje bağlamı ekranlar arasında kaybolmaz.</p>
        <div className="landing-tabs" role="tablist" aria-label="Ürün yüzeyleri" data-reveal="">{(Object.keys(surfaces) as SurfaceId[]).map((id) => <button type="button" role="tab" aria-selected={surface === id} key={id} onClick={() => setSurface(id)}>{surfaces[id].label}</button>)}</div>
        <div data-reveal=""><div className="landing-surface-card" key={surface}><div><h3>{surfaces[surface].title}</h3><p>{surfaces[surface].body}</p><ul>{surfaces[surface].bullets.map((bullet) => <li key={bullet}><ChevronRight />{bullet}</li>)}</ul></div><ProductPreview surface={surface} /></div></div>
      </section>

      <section id="grafik" className="landing-section landing-container landing-graph-section">
        <div data-reveal=""><code className="landing-eyebrow">BİLGİ GRAFİĞİ</code><h2>Bağlantıyı kurun,<br />bağlam kendiliğinden oluşsun.</h2><p className="landing-lead">Notlar, görevler ve kararlar aynı proje içinde bağlanır. Grafik görünümü hangi kararın hangi işi beslediğini bir bakışta gösterir.</p><ul className="landing-points"><li><i className="primary" /><span><strong>İki yönlü bağlantılar</strong> kararın kullanıldığı görev ve notları görünür kılar.</span></li><li><i className="ai" /><span><strong>Proje filtreleri</strong> büyük grafiği ilgili bağlama indirger.</span></li><li><i className="success" /><span><strong>Copilot bağlamı</strong> aynı ilişkileri semantik aramada kullanır.</span></li></ul></div>
        <div className="landing-graph-card" data-reveal="" style={{ transitionDelay: '90ms' }}><header><code>GRAF · PROJE BAĞLAMI</code><span><Network /> CANLI</span></header><svg viewBox="0 0 520 400" role="img" aria-label="Görev ve notlardan oluşan bilgi grafiği"><g className="edges"><line x1="260" y1="200" x2="128" y2="106" /><line x1="260" y1="200" x2="392" y2="128" /><line x1="260" y1="200" x2="150" y2="300" /><line x1="260" y1="200" x2="386" y2="296" /><line x1="128" y1="106" x2="212" y2="62" /><line x1="392" y1="128" x2="452" y2="212" /><line x1="150" y1="300" x2="248" y2="352" /><line x1="386" y1="296" x2="452" y2="212" /><line x1="128" y1="106" x2="70" y2="196" /><line x1="70" y1="196" x2="150" y2="300" /></g><g className="active-edges"><line x1="260" y1="200" x2="392" y2="128" /><line x1="260" y1="200" x2="150" y2="300" /></g><g className="nodes"><circle cx="260" cy="200" r="15" className="main" /><circle cx="260" cy="200" r="26" className="pulse" /><circle cx="128" cy="106" r="9" className="cyan" /><circle cx="392" cy="128" r="10" className="violet" /><circle cx="150" cy="300" r="8" className="green" /><circle cx="386" cy="296" r="9" className="yellow" /><circle cx="212" cy="62" r="5" /><circle cx="452" cy="212" r="6" /><circle cx="248" cy="352" r="6" /><circle cx="70" cy="196" r="5" /></g><g className="labels"><text x="260" y="236" textAnchor="middle">Sprint 24</text><text x="128" y="88" textAnchor="middle">Ödeme RFC</text><text x="392" y="110" textAnchor="middle">ATL-118</text><text x="150" y="322" textAnchor="middle">Mimari Konsey</text><text x="386" y="318" textAnchor="middle">Retry SLA</text></g></svg></div>
      </section>

      <section id="roller" className="landing-section landing-container"><code className="landing-eyebrow" data-reveal="">ROLLER VE İZİNLER</code><h2 data-reveal="">Yetkisi olmayan düğmeyi görmez.</h2><p className="landing-lead" data-reveal="">Dört rol ve backend tarafından doğrulanan izin modeli. Arayüz, kullanıcının gerçek çalışma alanı ve proje rolüne göre şekillenir.</p><div className="landing-role-grid">{roles.map((role, index) => <article key={role.name} data-reveal="" style={{ transitionDelay: `${index * 70}ms` }}><header><i style={{ background: role.color }} /><h3>{role.name}</h3></header><p>{role.description}</p><ul>{role.permissions.map((permission) => <li key={permission}><Check />{permission}</li>)}</ul></article>)}</div></section>

      <section id="sss" className="landing-section landing-faq"><code className="landing-eyebrow" data-reveal="">SIK SORULANLAR</code><h2 data-reveal="">Merak edilenler</h2><div data-reveal="">{faqs.map((faq, index) => <article key={faq.question}><button type="button" onClick={() => setOpenFaq((current) => current === index ? -1 : index)} aria-expanded={openFaq === index}><span>{faq.question}</span><b aria-hidden="true">+</b></button><div className="landing-faq-answer" data-open={openFaq === index ? 'true' : undefined}><p>{faq.answer}</p></div></article>)}</div></section>

      <section className="landing-cta landing-container" data-reveal=""><Sparkles /><h2>Ekibinizin belleğini bir daha kaybetmeyin.</h2><p>Çalışma alanınızı birkaç dakikada kurun; görev, bilgi ve Copilot bağlamını aynı yerde tutun.</p><div><Link className="landing-primary" to="/register">Ücretsiz başla <ArrowRight /></Link><Link className="landing-secondary" to="/login">Hesabım var</Link></div></section>
    </main>

    <footer className="landing-footer landing-container"><div><a className="landing-brand" href="#top"><img className="brand-mark-image" src="/brand-mark.png" alt="" width="28" height="28" loading="eager" decoding="async" /><strong>TaskPilot</strong></a><p>Görev, bilgi ve yapay zekâ<br />tek çalışma alanında.</p></div><nav aria-label="Alt navigasyon"><a href="#yuzeyler">Ürün</a><a href="#grafik">Bilgi grafiği</a><a href="#roller">Roller</a><Link to="/login">Giriş</Link><Link to="/register">Kayıt</Link></nav><small>© 2026 TaskPilot · İstanbul</small></footer>
  </div>;
}
