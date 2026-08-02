# TaskPilot Frontend — React Dönüşüm ve Production Polish Handoff

## 1. Amaç

Bu klasördeki `TaskPilot.dc.html`, TaskPilot için onaylanan **Midnight Atelier** tasarım prototipidir. React dönüşümünün amacı prototipi yeniden yorumlamak veya yeni bir görsel yön üretmek değil; aynı görünümü, bilgi yoğunluğunu ve etkileşim modelini gerçek backend sözleşmelerine bağlı, erişilebilir ve sürdürülebilir bir web uygulamasına dönüştürmektir.

Prototip görsel kaynak, bu doküman ise uygulama ve kalite sözleşmesidir. Çelişki olduğunda:

1. Yetki, veri ve iş kurallarında backend sözleşmesi,
2. Production davranışında bu handoff,
3. Görsel ayrıntılarda `TaskPilot.dc.html`

önceliklidir.

## 2. Korunacak Görsel Kimlik

Aşağıdaki kararlar değiştirilmemelidir:

- Ürün atmosferi: **Midnight Atelier** — sakin, teknik, yoğun fakat yorucu olmayan.
- Ana koyu arka plan: `#0B0D12`.
- Primary / Signal Indigo: `#7767E8`.
- AI / Aurora Cyan: `#35C7D6`.
- Success: `#22C55E`.
- Warning: `#F59E0B`.
- Danger: `#F0526E`.
- UI fontu: Inter veya Geist.
- Teknik metadata fontu: JetBrains Mono.
- Workspace rail + navigation sidebar + main workspace + opsiyonel sağ panel yapısı.
- Bilgi yoğun, kompakt tablo ve Kanban yaklaşımı.
- AI özelliklerinin cyan/indigo ile ayrılması; bütün ekranın gradient kutulara dönüştürülmemesi.
- Dashboard, görev listesi, beş kolonlu Kanban, Knowledge editor, graph ve Copilot için prototipteki ana kompozisyon.
- Türkçe ürün arayüzü. Backend enum değerleri kullanıcıya ham biçimde gösterilmemelidir.

Yeni bir logo, farklı renk paleti, aşırı yuvarlak kart sistemi, glassmorphism veya generic SaaS dashboard yaklaşımı üretilmemelidir.

## 3. React Dönüşüm Kuralları

- Önerilen temel: React + TypeScript + Vite.
- Prototip bir `iframe` içine alınmamalı ve büyük bir HTML string olarak taşınmamalıdır.
- `support.js` production runtime bağımlılığı yapılmamalıdır; bu dosya prototip runtime'ına aittir.
- Tasarım reusable React component'lerine ayrılmalıdır.
- Server state için tek bir query/cache katmanı kullanılmalıdır (ör. TanStack Query).
- Route'lar gerçek URL ve deep-link üretmelidir (ör. React Router).
- Formlar typed validation kullanmalıdır.
- Access token mümkünse yalnız bellekte tutulmalı; refresh akışı merkezi API client üzerinden yönetilmelidir.
- Backend `ServiceResult` ve ProblemDetails cevapları tek bir hata adaptöründe normalize edilmelidir.
- Enum, tarih, rol ve durum gösterimleri merkezi formatter/map dosyalarından gelmelidir.
- Optimistic UI yalnız güvenli ve geri alınabilir işlemlerde kullanılmalıdır.
- React component içinde dağınık `fetch` çağrıları yapılmamalıdır.
- Role/permission kontrolü yalnız görsel değildir; backend her zaman nihai yetki kaynağıdır.

Önerilen üst seviye dizinler:

```text
src/
  app/            # router, providers, app shell
  api/            # typed client, auth refresh, error mapping
  components/     # ortak design-system component'leri
  features/
    auth/
    workspaces/
    projects/
    tasks/
    knowledge/
    notifications/
    ai-suggestions/
    copilot/
    reports/
  hooks/
  lib/
  styles/
  types/
```

## 4. Production'da Zorunlu Düzeltmeler

### P0 — Kullanıcıya Çıkmadan Önce

#### 4.1 Prototype/debug kontrollerini ayır

Şunlar yalnız development/prototype modunda bulunmalıdır:

- Üstteki `TASKPILOT · PROTOTYPE` kontrol şeridi.
- Owner / Manager / Member / Guest rol simülatörü.
- 1440 / 834 / 390 viewport simülatörü.
- `SKELETON` debug butonu.
- `backend dependency: ...` metinleri.
- Tasarım dokümanları ve developer handoff navigasyonu.

Production build içinde bunlar render edilmemelidir. Eksik backend özelliği varsa son kullanıcıya teknik dependency yazısı göstermek yerine özellik gizlenmeli, boş durum gösterilmeli veya feature flag kullanılmalıdır.

#### 4.2 Due date semantiğini düzelt

`gecikmiş` yalnız açık görevlerde kullanılmalıdır:

```text
status ∈ {Todo, InProgress, InReview}
AND dueDate < now
```

- Done görev geçmiş due date taşıyorsa otomatik olarak `gecikmiş` yazma.
- Eğer `CompletedAt > DueDate` ise isteğe bağlı olarak `geç tamamlandı` kullanılabilir.
- Cancelled görev hiçbir zaman `gecikmiş` gösterilmemelidir.
- Cancelled görevde yalnız tarih veya `iptal edildi` metni kullanılmalıdır.

#### 4.3 Gerçek task geçiş matrisini uygula

```text
Todo        → InProgress | Cancelled
InProgress  → InReview   | Cancelled
InReview    → Done       | Cancelled
Done        → InProgress
Cancelled   → InProgress (yalnız Manager/Owner)
```

- Geçersiz Kanban hedefleri drag sırasında pasif görünmelidir.
- Klavye kullanıcıları için kart üzerinde aynı geçişleri sunan bir aksiyon bulunmalıdır.
- Team Member yalnız kendisine atanmış görevin durumunu değiştirebilir.
- Guest durum değiştiremez.
- Arşivlenmiş projede mutation yapılamaz.
- UI'daki kısıt backend hatasının yerine geçmez; 403/409 cevapları yine ele alınmalıdır.

#### 4.4 Note concurrency davranışını koru

- Note güncellemesinde `If-Match` ile mevcut numeric version gönderilmelidir.
- 428 cevabı eksik version isteği olarak ele alınmalıdır.
- 409 cevabında sessiz overwrite yapılmamalıdır.
- Conflict modalı şu aksiyonları sunmalıdır:
  - Sunucudaki sürümü yükle.
  - Yerel metni panoya kopyala.
  - İki sürümü karşılaştır.
  - Editöre dön.
- Backend otomatik merge sağlamadığı sürece `birleştir ve kaydet` davranışı eklenmemelidir.

#### 4.5 AI insan onayı sınırını koru

AI suggestion lifecycle:

```text
Pending → Processing → Completed | Failed → Applied
```

- Öneri kullanıcı `Apply` demeden görev haline gelmemelidir.
- Apply tek kullanımlık olmalı; ikinci denemede 409 anlaşılır gösterilmelidir.
- Pending/Processing sırasında kullanıcı ekranı terk edebilmelidir.
- Arka plan sonucu bildirim üzerinden erişilebilir olmalıdır.
- Hata mesajında prompt, secret veya hassas context gösterilmemelidir.

### P1 — Okunabilirlik ve Responsive

#### 4.6 Minimum yazı ölçüleri

- Ana gövde/UI metni: 13–14 px.
- Sidebar/navigation: en az 12.5–13 px.
- Metadata ve tablo başlığı: en az 11–12 px.
- 9.5 px metin production'da kullanılmamalıdır.
- JetBrains Mono yalnız label, enum, ID, tarih ve teknik metadata için kullanılmalıdır.
- Ana açıklama ve uzun metinler Inter/Geist olmalıdır.

#### 4.7 Gerçek responsive davranış

Desktop, tablet ve mobile yalnız CSS scale ile küçültülmemelidir.

**Desktop (>= 1280):**

- Workspace rail ve sidebar görünür.
- Sağ context paneli içerik yanında açılır.
- Liste ve Kanban bilgi yoğunluğunu korur.

**Tablet (768–1279):**

- Sidebar collapsible/overlay olabilir.
- Sağ panel overlay veya 320 px drawer olur.
- Ana tablo gerekli alanları korur, düşük öncelikli metadata gizlenebilir.

**Mobile (<= 767):**

- Bottom navigation kullanılmalıdır.
- Task drawer tam ekran sayfaya dönüşmelidir.
- Görev listesi tablo değil mobil kart/list olmalıdır.
- Kanban yatay scroll ve açık kolon başlıklarıyla çalışmalıdır.
- Note editor `Edit` / `Preview` tabları kullanmalıdır; split görünüm kapalıdır.
- Copilot tam ekran veya tam genişlik drawer olmalıdır.
- Graph için liste alternatifi bulunmalıdır.
- Tüm touch target'lar en az 44×44 px olmalıdır.

#### 4.8 Panel genişlikleri

- Note editor ana içerik alanı masaüstünde minimum 520–600 px olmalıdır.
- Sidebar, explorer ve backlinks panelleri kapatılabilir olmalıdır.
- Uzun başlıklar kelime ortasından kırılmamalıdır.
- Panel genişlikleri mümkünse kullanıcı tarafından değiştirilebilir ve local preference olarak saklanabilir.

### P1 — Erişilebilirlik

- WCAG 2.2 AA kontrast hedeflenmelidir.
- Tüm icon-only butonlarda accessible name bulunmalıdır.
- Focus ring görünür olmalıdır.
- Modal açıldığında focus trap uygulanmalı; kapanınca focus tetikleyiciye dönmelidir.
- `Esc` drawer, modal, palette ve context panelini tutarlı kapatmalıdır.
- Command palette: macOS `Cmd+K`, diğer sistemlerde `Ctrl+K`.
- Kanban drag-and-drop için klavye alternatifi zorunludur.
- Graph için screen-reader uyumlu liste görünümü sunulmalıdır.
- Status ve priority yalnız renkle anlatılmamalı; icon/metin bulunmalıdır.
- `prefers-reduced-motion` desteklenmelidir.
- SSE token akışı `aria-live` alanını aşırı güncellememelidir; tamamlanan mesaj kontrollü duyurulmalıdır.

## 5. Yetki Matrisi

### Workspace

| Aksiyon | Owner | Manager | Member | Guest |
|---|---:|---:|---:|---:|
| Görüntüleme | Evet | Evet | Evet | Evet |
| Workspace güncelleme/arşivleme | Evet | Hayır | Hayır | Hayır |
| Üye davet etme | Evet | Evet | Hayır | Hayır |
| Workspace rolü değiştirme | Evet | Hayır | Hayır | Hayır |
| Proje oluşturma | Evet | Evet | Hayır | Hayır |

### Project

| Aksiyon | Owner/Project Manager | Team Member | Guest |
|---|---:|---:|---:|
| Projeyi görme | Evet | Üye olduğu proje | Üye olduğu proje |
| Proje ayarları | Evet | Hayır | Hayır |
| Görev oluşturma | Evet | Evet | Hayır |
| Görev atama | Evet | Hayır | Hayır |
| Durum değiştirme | Evet | Yalnız kendi görevi | Hayır |
| Yorum yazma | Evet | Evet | Evet |
| Label yönetimi | Evet | Hayır | Hayır |

### Knowledge

- Guest: read-only.
- Member ve üstü: note create/update/delete.
- Manager/Owner: klasör ve knowledge tag yönetimi.
- Guest'e görev atanamaz; assignee picker içinde seçilebilir hedef gibi gösterilmemelidir.
- Kullanıcının hiçbir zaman kullanamayacağı yönetim aksiyonları gizlenmelidir.
- Geçici olarak kullanılamayan aksiyonlar disabled + açıklayıcı tooltip olmalıdır.

## 6. Backend ile Uyum

Backend base URL environment değişkeninden gelmelidir. Local varsayılan örnek:

```text
http://localhost:18080
```

Önemli gerçek endpoint grupları:

- Auth: `/api/auth/*`
- Workspaces: `/api/workspaces/*`
- Workspace projects: `/api/workspaces/{workspaceId}/projects`
- Workspace members: `/api/workspaces/{workspaceId}/members`
- Project: `/api/projects/{projectId}`
- Project members: `/api/projects/{projectId}/members`
- Tasks: `/api/projects/{projectId}/tasks`, `/api/tasks/{taskId}`
- Task status/assign: `/api/tasks/{taskId}/status`, `/api/tasks/{taskId}/assign`
- Comments: `/api/tasks/{taskId}/comments`, `/api/comments/{commentId}`
- Labels: `/api/projects/{projectId}/labels`, `/api/tasks/{taskId}/labels/{labelId}`
- Dashboard: `/api/projects/{projectId}/dashboard`
- Notifications: `/api/notifications`
- Knowledge: workspace folder listeleme/yazma endpoint'leri, `POST /api/workspaces/{workspaceId}/notes` ile not oluşturma ve `/api/notes/{noteId}/*` altında not detay/link/revision işlemleri
- Knowledge search/graph: `/api/workspaces/{workspaceId}/knowledge/search`, `/api/workspaces/{workspaceId}/knowledge/graph`
- Task-note link: `/api/tasks/{taskId}/notes/{noteId}`
- AI suggestion: `/api/projects/{projectId}/ai/task-suggestions`, `/api/ai/suggestions/{id}`, `/api/ai/suggestions/{id}/apply`
- Project semantic search: `/api/projects/{projectId}/tasks/semantic-search`
- Copilot: `/api/projects/{projectId}/ai/chat`, `/api/workspaces/{workspaceId}/ai/chat`, streaming karşılıkları ve `/api/ai/chat/{sessionId}`
- Reports: `/api/projects/{projectId}/ai/reports`, `/api/ai/reports/{reportId}`, approve/reject ve schedule uçları

**Not:** Endpoint isimleri tahmin edilmemeli; implementasyon öncesi çalışan OpenAPI/Swagger sözleşmesinden typed client üretilmeli veya DTO'lar doğrulanmalıdır.

### Mevcut read-contract boşlukları

Aşağıdaki alanlar prototipte mock veriyle gösterilmiştir ve gerçek entegrasyon öncesi backend desteği gerektirir:

1. Workspace/project note listesi.
2. Task'a atanmış label'ları okuma.
3. Task'a bağlı note listesini okuma.
4. Project AI suggestion geçmişi.
5. Copilot session listesi.
6. Project weekly report geçmişi.

Bu boşluklar sessizce farklı endpoint'lerden türetilmemeli veya N+1 isteklerle taklit edilmemelidir. Endpoint hazır değilse ilgili yüzey feature flag/empty-state arkasında tutulmalıdır.

## 7. Veri ve UI Semantiği

- Project durumları: `Active`, `Completed`, `Archived`.
- Task durumları: `Todo`, `InProgress`, `InReview`, `Done`, `Cancelled`.
- Priority: `Low`, `Medium`, `High`, `Critical`.
- AI suggestion: `Pending`, `Processing`, `Completed`, `Failed`, `Applied`.
- Weekly report: `PendingReview`, `Approved`, `Rejected`.
- Yorum limiti: 2000 karakter.
- Due date geçmiş tarih seçilemez.
- Completion rate hesabına Cancelled görevler girmez.
- Archived project/workspace read-only banner göstermelidir.
- Notification click, `Type + RelatedEntityId` güvenli biçimde tanınabiliyorsa ilgili kaynağa götürmelidir.
- Semantic result score ana mesaj değil secondary metadata olmalıdır.
- Copilot citation'ları `[Task:id]` ve `[Note:id]` olarak tıklanabilir kaynak chip'lerine dönüştürülmelidir.
- Citation tıklandığında chat bağlamı kaybolmadan source preview açılmalıdır.

## 8. Component Sözleşmesi

En az aşağıdaki reusable component'ler oluşturulmalıdır:

- `AppShell`
- `WorkspaceRail`
- `NavigationSidebar`
- `ProjectTree`
- `TopToolbar`
- `CommandPalette`
- `StatusBadge`
- `PriorityBadge`
- `MemberAvatar` / `MemberPicker`
- `LabelChip` / `LabelPicker`
- `TaskTable`
- `TaskKanban`
- `TaskCard`
- `TaskDetailDrawer`
- `TaskStatusTransitionMenu`
- `MarkdownEditor`
- `NoteExplorer`
- `BacklinksPanel`
- `RevisionTimeline`
- `KnowledgeGraph`
- `CopilotPanel`
- `CopilotMessage`
- `CitationChip`
- `AiSuggestionReview`
- `WeeklyReportViewer`
- `NotificationCenter`
- `PermissionGate`
- `EmptyState`
- `LoadingSkeleton`
- `ProblemState`
- `ConfirmationDialog`
- `NoteConflictDialog`

Component'ler role kontrolü, API çağrısı ve görsel markup'ı tek yerde karıştırmamalıdır. Feature container veri/permission kararını verir; presentational component sonucu render eder.

## 9. Hata ve Asenkron Durumlar

Her ana feature şu durumları kapsamalıdır:

- Initial loading.
- Refetching.
- Empty state.
- Search no-results.
- Field validation.
- 401 + tek kontrollü refresh denemesi.
- 403 mevcut bağlamı koruyan permission state.
- 404 resource state.
- 409 conflict.
- 428 version required.
- 429 rate limit + varsa `Retry-After`.
- 500/general problem.
- Network unavailable.
- Optimistic mutation rollback.
- AI Pending/Processing/Failed.
- SSE disconnect/retry.

Hata detayında correlation ID kopyalanabilir olabilir; ham stack trace, token, prompt veya secret gösterilmemelidir.

## 10. Test ve Kabul Kriterleri

React dönüşümü aşağıdaki kontroller geçmeden tamamlanmış sayılmaz:

### Görsel

- 1440 px görünüm prototiple görsel olarak tutarlı.
- 1280 px'de yatay sayfa taşması yok.
- 834 px tablet akışları kullanılabilir.
- 390 px mobile bottom navigation ve tam ekran detail akışları çalışıyor.
- Dark ve light theme token'ları tutarlı.

### Davranış

- URL deep-link ile aynı task/note/session tekrar açılıyor.
- Refresh sonrası auth/session davranışı kontrollü.
- Tüm task status geçişleri role ve transition matrisiyle uyumlu.
- Guest mutation aksiyonlarını göremiyor veya kullanamıyor.
- Arşivlenmiş projede mutation yok.
- Note 409 conflict veri kaybına yol açmıyor.
- AI suggestion Apply yalnız bir kez çalışıyor.
- Copilot streaming ve citation navigation çalışıyor.
- Notification read/read-all çalışıyor.

### Erişilebilirlik

- Sadece klavyeyle ana akışlar tamamlanabiliyor.
- Focus visible.
- Modal/drawer focus trap doğru.
- Icon-only butonlarda accessible name var.
- Status/priority renk olmadan da anlaşılır.
- Reduced motion çalışıyor.
- Otomatik erişilebilirlik taramasında kritik ihlal yok.

### Kalite

- TypeScript strict açık.
- Lint, typecheck ve testler başarılı.
- Feature bileşenleri API DTO'larına doğrudan kırılgan biçimde bağlanmamış.
- Secret veya gerçek `.env` değeri repoya girmiyor.
- Prototype/debug kontrolleri production build'de yok.

## 11. Kapsam Dışı

Şunlar bu React dönüşümüne eklenmemelidir:

- Offline/local-first sync.
- Gerçek zamanlı ortak note editing.
- Dosya attachment sistemi.
- Canvas.
- Plugin marketplace.
- Billing/payment.
- Calendar provider entegrasyonu.
- Native mobil uygulama.
- Git/PR entegrasyonu.
- Backend'de bulunmayan task progress yüzdesi veya approval workflow.

## 12. Son Handoff İlkesi

Bu prototip yeniden tasarlanmayacak kadar olgun durumdadır. React ajanının ana işi:

1. Görsel kimliği korumak,
2. Prototype-only öğeleri production'dan ayırmak,
3. Gerçek backend sözleşmesine bağlanmak,
4. Responsive ve accessibility davranışını kanıtlamak,
5. Eksik backend read-contract'lerini açık ve güvenli şekilde izole etmektir.
