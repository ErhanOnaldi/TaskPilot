# TaskPilot — AI Destekli Proje Yönetim Platformu

## Doküman Türü

Bu doküman TaskPilot projesi için ürün gereksinim dokümanı, teknik analiz ve geliştirme yol haritası birleşimidir.

Amaç yalnızca teknoloji öğrenmek değildir. Amaç, gerçek dünyaya yakın bir proje yönetim platformunu adım adım geliştirirken ASP.NET Core, PostgreSQL, Redis, RabbitMQ, Docker, test, logging ve AI Agent entegrasyonlarını doğal ihtiyaçlar üzerinden öğrenmektir.

---

# 1. Ürün Vizyonu

TaskPilot; küçük ve orta ölçekli yazılım ekiplerinin proje, görev, yorum, etiket, bildirim ve AI destekli görev planlama süreçlerini yönetebildiği bir proje yönetim platformudur.

Kullanıcılar workspace oluşturur, projeler açar, ekip üyelerini davet eder, görevler oluşturur, görevleri kişilere atar, yorum yapar, görev durumlarını takip eder ve proje dashboard'u üzerinden ilerlemeyi görür.

AI katmanı, kullanıcıya görev yazarken yardımcı olur. Örneğin bir kullanıcı "Landing page performansını iyileştir" diye görev açtığında sistem görev için öncelik, etiket, alt görev ve tahmini deadline önerebilir.

---

# 2. Problem Tanımı

Birçok junior geliştirici proje geliştirirken şu hataya düşer:

- Önce teknoloji seçer.
- Sonra entity yazar.
- Ama uygulamanın gerçek davranışlarını netleştirmez.

TaskPilot bu hatayı düzeltmek için tasarlanmıştır. Bu projede önce domain ve gereksinimler tanımlanır, sonra API ve veri modeli çıkarılır, en son teknoloji kararları uygulanır.

---

# 3. Hedef Kullanıcılar

## 3.1 Workspace Owner

Workspace'i oluşturan kişidir.

Yetkileri:

- Workspace ayarlarını yönetir.
- Kullanıcı davet eder.
- Rolleri değiştirir.
- Proje oluşturur.
- Workspace'i arşivleyebilir.
- Tüm projeleri görebilir.

## 3.2 Project Manager

Belirli projeleri yöneten kişidir.

Yetkileri:

- Proje detaylarını düzenler.
- Görev oluşturur.
- Görev atar.
- Görev durumlarını değiştirir.
- Proje dashboard'unu görüntüler.
- Proje üyelerini yönetir.

## 3.3 Team Member

Projede çalışan kullanıcıdır.

Yetkileri:

- Kendisine atanan görevleri görür.
- Görev durumunu güncelleyebilir.
- Yorum yazabilir.
- Dosya ekleyebilir.
- Görev oluşturabilir fakat proje ayarlarını değiştiremez.

## 3.4 Guest

Sınırlı erişimli kullanıcıdır.

Yetkileri:

- Sadece dahil edildiği projeleri görür.
- Yorum yazabilir.
- Görev oluşturamaz.
- Proje ayarlarını değiştiremez.

---

# 4. MVP Kapsamı

İlk geliştirilecek sürümde aşağıdaki özellikler bulunmalıdır:

- Kullanıcı kayıt ve giriş
- JWT tabanlı authentication
- Workspace oluşturma
- Proje oluşturma
- Projeye üye ekleme
- Görev CRUD
- Görev atama
- Görev durum güncelleme
- Görev önceliği belirleme
- Yorum ekleme
- Etiket ekleme
- Proje dashboard'u
- Temel bildirim sistemi
- Audit log
- Redis cache
- RabbitMQ event publishing
- AI task suggestion

MVP dışında bırakılanlar:

- Gerçek zamanlı chat
- Gelişmiş dosya yönetimi
- Ödeme sistemi
- Takvim entegrasyonu
- Mobil uygulama
- Çok gelişmiş raporlama
- Multi-language destek

---

# 5. Functional Requirements

## FR-001 Kullanıcı Kayıt Olabilir

Kullanıcı email ve şifre ile sisteme kayıt olabilir.

Acceptance Criteria:

- Email zorunludur.
- Email benzersiz olmalıdır.
- Şifre minimum 8 karakter olmalıdır.
- Şifre hashlenerek saklanmalıdır.
- Aynı email ile ikinci kayıt yapılamaz.
- Başarılı kayıt sonrası kullanıcı oluşturulma tarihi kaydedilir.

---

## FR-002 Kullanıcı Giriş Yapabilir

Kullanıcı email ve şifre ile giriş yapabilir.

Acceptance Criteria:

- Email sistemde yoksa genel hata dönülür.
- Şifre yanlışsa genel hata dönülür.
- Başarılı girişte access token döner.
- Token içinde UserId ve Email claim olarak bulunur.
- PasswordHash response içinde asla dönmez.

---

## FR-003 Kullanıcı Workspace Oluşturabilir

Giriş yapmış kullanıcı yeni workspace oluşturabilir.

Acceptance Criteria:

- Workspace adı zorunludur.
- Workspace adı maksimum 100 karakterdir.
- Workspace'i oluşturan kullanıcı Owner rolüne sahip olur.
- Bir kullanıcı birden fazla workspace oluşturabilir.

---

## FR-004 Workspace'e Kullanıcı Davet Edilebilir

Workspace Owner veya yetkili kullanıcı workspace'e yeni kullanıcı davet edebilir.

Acceptance Criteria:

- Davet edilen email geçerli formatta olmalıdır.
- Aynı kullanıcı aynı workspace'e iki kere eklenemez.
- Varsayılan rol Team Member olur.
- Davet işlemi notification event'i üretir.

---

## FR-005 Proje Oluşturulabilir

Workspace içinde proje oluşturulabilir.

Acceptance Criteria:

- Proje adı zorunludur.
- Proje adı aynı workspace içinde benzersiz olmalıdır.
- Proje bir workspace'e bağlı olmalıdır.
- Projeyi oluşturan kişi proje manager olarak atanır.
- Proje varsayılan olarak Active statüsünde başlar.

---

## FR-006 Proje Güncellenebilir

Yetkili kullanıcı proje bilgilerini güncelleyebilir.

Acceptance Criteria:

- Sadece Workspace Owner veya Project Manager güncelleyebilir.
- Proje adı boş olamaz.
- Arşivlenmiş proje güncellenemez.
- Güncelleme audit log üretir.

---

## FR-007 Görev Oluşturulabilir

Yetkili kullanıcı proje içinde görev oluşturabilir.

Acceptance Criteria:

- Title zorunludur.
- Title maksimum 100 karakterdir.
- Description opsiyoneldir.
- DueDate geçmiş tarih olamaz.
- Priority belirtilmezse Medium olur.
- Status varsayılan olarak Todo olur.
- AssignedUserId verilirse kullanıcı aynı projenin üyesi olmalıdır.
- Görev oluşturulunca TaskCreated event'i yayınlanır.

---

## FR-008 Görev Güncellenebilir

Yetkili kullanıcı görev bilgilerini güncelleyebilir.

Acceptance Criteria:

- Görev başlığı boş olamaz.
- DueDate geçmiş tarih olamaz.
- Arşivlenmiş projedeki görev güncellenemez.
- Görev atanan kullanıcı değiştirilirse TaskAssigned event'i yayınlanır.
- Güncelleme audit log üretir.

---

## FR-009 Görev Durumu Değiştirilebilir

Kullanıcı görev durumunu değiştirebilir.

Geçerli statüler:

- Todo
- InProgress
- InReview
- Done
- Cancelled

Acceptance Criteria:

- Todo -> InProgress yapılabilir.
- InProgress -> InReview yapılabilir.
- InReview -> Done yapılabilir.
- Done görev tekrar InProgress yapılabilir.
- Cancelled görev sadece Project Manager tarafından tekrar açılabilir.
- Status değişikliği audit log üretir.
- Done olduğunda CompletedAt set edilir.
- Done dışına alınırsa CompletedAt null yapılır.

---

## FR-010 Görev Kullanıcıya Atanabilir

Yetkili kullanıcı görevi proje üyesine atayabilir.

Acceptance Criteria:

- Atanan kullanıcı proje üyesi olmalıdır.
- Guest kullanıcılara görev atanamaz.
- Aynı kullanıcıya tekrar atama yapılırsa event üretilmez.
- Atama sonrası bildirim oluşturulur.

---

## FR-011 Göreve Yorum Eklenebilir

Proje üyesi görev altına yorum yazabilir.

Acceptance Criteria:

- Yorum boş olamaz.
- Yorum maksimum 2000 karakterdir.
- Yorum yazan kullanıcı proje üyesi olmalıdır.
- Yorum oluşturulunca CommentAdded event'i yayınlanır.

---

## FR-012 Göreve Etiket Eklenebilir

Yetkili kullanıcı göreve label ekleyebilir.

Acceptance Criteria:

- Label adı boş olamaz.
- Label adı proje içinde benzersiz olmalıdır.
- Aynı label aynı göreve iki kere eklenemez.
- Label adı maksimum 50 karakterdir.

---

## FR-013 Proje Dashboard'u Görüntülenebilir

Kullanıcı proje dashboard'unu görüntüleyebilir.

Dashboard verileri:

- Toplam görev sayısı
- Todo görev sayısı
- InProgress görev sayısı
- Done görev sayısı
- Geciken görev sayısı
- Completion rate
- En son yorumlar
- En yüksek öncelikli açık görevler

Acceptance Criteria:

- Sadece proje üyeleri görüntüleyebilir.
- Dashboard Redis ile cache'lenir.
- Görev, yorum veya atama değiştiğinde cache invalidation yapılır.

---

## FR-014 Bildirim Oluşturulabilir

Sistem önemli olaylarda bildirim oluşturur.

Bildirim üreten olaylar:

- Görev atanması
- Göreve yorum eklenmesi
- Deadline yaklaşması
- AI önerisinin hazır olması

Acceptance Criteria:

- Bildirim kullanıcıya bağlıdır.
- Okundu/okunmadı bilgisi tutulur.
- Kullanıcı kendi bildirimlerini görebilir.
- Başkasının bildirimlerini göremez.

---

## FR-015 Audit Log Tutulur

Sistem kritik değişiklikleri audit log olarak kaydeder.

Loglanacak işlemler:

- Proje oluşturma
- Proje güncelleme
- Görev oluşturma
- Görev güncelleme
- Görev silme
- Görev status değişikliği
- Kullanıcı daveti
- Rol değişikliği

Acceptance Criteria:

- Audit log sonradan değiştirilemez.
- Hangi kullanıcı, hangi entity, hangi işlem, hangi tarihte bilgileri tutulur.
- Audit log işlemleri RabbitMQ consumer üzerinden asenkron yapılabilir.

---

## FR-016 AI Task Suggestion Alınabilir

Kullanıcı görev başlığı veya açıklamasına göre AI önerisi alabilir.

Örnek input:

```text
Landing page performansını iyileştir.
```

Örnek output:

```json
{
  "priority": "High",
  "labels": ["frontend", "performance"],
  "subtasks": [
    "Run Lighthouse audit",
    "Optimize images",
    "Lazy load images"
  ],
  "suggestedDueDate": "2026-07-01"
}
```

Acceptance Criteria:

- Kullanıcı proje üyesi olmalıdır.
- AI önerisi görev oluştururken istenebilir.
- AI önerisi otomatik olarak göreve uygulanmaz; kullanıcı onaylar.
- AI response parse edilemezse güvenli hata dönülür.
- AI çağrısı uzun sürebileceği için async job/event ile yönetilebilir.

---

# 6. Non-Functional Requirements

## NFR-001 Performans

- Normal CRUD endpointleri p95 altında 300ms hedeflemelidir.
- Dashboard endpointi cache hit durumunda p95 altında 100ms hedeflemelidir.
- AI endpointleri uzun sürebileceği için ayrı timeout stratejisi uygulanmalıdır.

## NFR-002 Güvenlik

- Password hashlenerek saklanmalıdır.
- JWT secret environment variable olarak tutulmalıdır.
- Kullanıcı sadece yetkili olduğu workspace, proje ve görevleri görebilmelidir.
- Broken access control engellenmelidir.
- Input validation her write endpointinde yapılmalıdır.
- Rate limiting auth endpointlerinde uygulanmalıdır.

## NFR-003 Reliability

- RabbitMQ consumer işlemleri idempotent olmalıdır.
- Event iki kere işlense bile duplicate notification veya duplicate audit log oluşmamalıdır.
- Kritik DB işlemleri transaction içinde yapılmalıdır.
- Background job failure durumunda retry stratejisi olmalıdır.

## NFR-004 Maintainability

- API, Application, Domain, Infrastructure ve Persistence ayrımı korunmalıdır.
- Controller içinde business logic yazılmamalıdır.
- Domain kuralları mümkün olduğunca domain/application katmanında tutulmalıdır.
- DTO ve entity birbirine karıştırılmamalıdır.

## NFR-005 Observability

- Request logging yapılmalıdır.
- Error logging yapılmalıdır.
- CorrelationId kullanılmalıdır.
- RabbitMQ consumer logları takip edilebilir olmalıdır.
- Slow query ve yavaş endpointler loglanmalıdır.

## NFR-006 Testability

- Business kuralları unit test ile test edilebilir olmalıdır.
- API endpointleri integration test ile test edilmelidir.
- Repository ve DbContext kullanımı test stratejisine uygun olmalıdır.
- Kritik authorization senaryoları test edilmelidir.

---

# 7. Business Rules

## BR-001

Bir görev yalnızca bağlı olduğu projenin üyesine atanabilir.

## BR-002

Guest kullanıcıya görev atanamaz.

## BR-003

Arşivlenmiş projede yeni görev oluşturulamaz.

## BR-004

Arşivlenmiş projede görev güncellenemez.

## BR-005

Workspace Owner tüm projelerde tam yetkilidir.

## BR-006

Project Manager sadece yönettiği projelerde tam yetkilidir.

## BR-007

Team Member kendi görevlerinin status bilgisini değiştirebilir.

## BR-008

Team Member başkasına görev atayamaz.

## BR-009

Done statüsüne geçen görev için CompletedAt set edilir.

## BR-010

Done dışına alınan görev için CompletedAt null yapılır.

## BR-011

DueDate geçmiş bir tarih olamaz.

## BR-012

Completion Rate şu şekilde hesaplanır:

```text
Done görev sayısı / Cancelled olmayan toplam görev sayısı
```

## BR-013

Cancelled görevler completion rate hesabına dahil edilmez.

## BR-014

Dashboard cache'i şu durumlarda silinir:

- Görev oluşturulduğunda
- Görev güncellendiğinde
- Görev silindiğinde
- Yorum eklendiğinde
- Görev status değiştiğinde
- Göreve label eklendiğinde

## BR-015

AI önerileri kullanıcı onayı olmadan kalıcı veri haline getirilmez.

## BR-016

Audit log kayıtları güncellenemez ve silinemez.

---

# 8. Use Case'ler

## UC-001 Kullanıcı Kayıt Olur

Aktör: Anonymous User

Ana Akış:

1. Kullanıcı email ve şifre girer.
2. Sistem email formatını doğrular.
3. Sistem email'in benzersiz olduğunu kontrol eder.
4. Sistem şifreyi hashler.
5. Sistem kullanıcıyı oluşturur.
6. Sistem başarılı response döner.

Alternatif Akışlar:

- Email zaten varsa 409 Conflict döner.
- Şifre zayıfsa 400 Bad Request döner.

---

## UC-002 Kullanıcı Giriş Yapar

Aktör: Registered User

Ana Akış:

1. Kullanıcı email ve şifre girer.
2. Sistem kullanıcıyı email ile bulur.
3. Sistem şifre hash doğrulaması yapar.
4. Sistem JWT üretir.
5. Sistem token döner.

Alternatif Akışlar:

- Email veya şifre yanlışsa 401 Unauthorized döner.

---

## UC-003 Workspace Oluşturulur

Aktör: Authenticated User

Ana Akış:

1. Kullanıcı workspace adı girer.
2. Sistem workspace'i oluşturur.
3. Kullanıcı Owner rolüyle workspace'e eklenir.
4. Sistem response döner.

---

## UC-004 Proje Oluşturulur

Aktör: Workspace Owner veya Project Manager

Ana Akış:

1. Kullanıcı workspace içinde proje oluşturma isteği gönderir.
2. Sistem kullanıcının workspace yetkisini kontrol eder.
3. Sistem proje adının benzersiz olduğunu kontrol eder.
4. Sistem projeyi oluşturur.
5. Sistem kullanıcıyı Project Manager olarak ilişkilendirir.
6. Sistem audit event yayınlar.

Alternatif Akışlar:

- Kullanıcı workspace üyesi değilse 403 döner.
- Proje adı aynı workspace içinde varsa 409 döner.

---

## UC-005 Görev Oluşturulur

Aktör: Project Manager veya Team Member

Ana Akış:

1. Kullanıcı proje altında görev oluşturma isteği gönderir.
2. Sistem kullanıcının proje üyesi olduğunu doğrular.
3. Sistem title, dueDate ve assignedUser kurallarını doğrular.
4. Sistem görevi oluşturur.
5. Sistem TaskCreated event yayınlar.
6. Sistem dashboard cache'ini invalidate eder.
7. Sistem response döner.

Alternatif Akışlar:

- AssignedUser proje üyesi değilse 400 döner.
- DueDate geçmişse 400 döner.
- Proje arşivlenmişse 409 döner.

---

## UC-006 Görev Atanır

Aktör: Project Manager

Ana Akış:

1. Project Manager görev atama isteği gönderir.
2. Sistem görevin projede olduğunu doğrular.
3. Sistem atanacak kullanıcının proje üyesi olduğunu doğrular.
4. Sistem görevin AssignedUserId değerini günceller.
5. Sistem TaskAssigned event yayınlar.
6. NotificationConsumer kullanıcıya bildirim oluşturur.

---

## UC-007 Göreve Yorum Eklenir

Aktör: Project Member

Ana Akış:

1. Kullanıcı görev detayında yorum yazar.
2. Sistem kullanıcının proje üyesi olduğunu doğrular.
3. Sistem yorum içeriğini doğrular.
4. Sistem yorumu kaydeder.
5. Sistem CommentAdded event yayınlar.
6. Sistem dashboard cache'ini invalidate eder.

---

## UC-008 Dashboard Görüntülenir

Aktör: Project Member

Ana Akış:

1. Kullanıcı dashboard endpointine istek atar.
2. Sistem kullanıcının proje üyesi olduğunu doğrular.
3. Sistem Redis cache kontrolü yapar.
4. Cache varsa response Redis'ten döner.
5. Cache yoksa DB'den hesaplanır.
6. Sonuç Redis'e yazılır.
7. Response döner.

---

## UC-009 AI Önerisi Alınır

Aktör: Project Member

Ana Akış:

1. Kullanıcı görev başlığı/açıklaması girer.
2. Sistem kullanıcının proje üyesi olduğunu doğrular.
3. Sistem AI suggestion request oluşturur.
4. Agent suggestion işlemi başlatılır.
5. AI sonucu hazır olduğunda kullanıcıya öneri olarak gösterilir.
6. Kullanıcı isterse öneriyi göreve uygular.

Alternatif Akışlar:

- AI servis timeout olursa öneri başarısız olarak işaretlenir.
- AI cevabı parse edilemezse kullanıcıya güvenli hata gösterilir.

---

# 9. Domain Model

## 9.1 User

Temsil ettiği kavram:

Sisteme giriş yapabilen kullanıcı.

Alanlar:

- Id
- Email
- PasswordHash
- CreatedAt
- UpdatedAt

İlişkiler:

- WorkspaceMembership
- ProjectMembership
- AssignedTasks
- Comments
- Notifications

---

## 9.2 Workspace

Temsil ettiği kavram:

Bir ekip veya organizasyon alanı.

Alanlar:

- Id
- Name
- CreatedByUserId
- CreatedAt
- UpdatedAt
- IsArchived

İlişkiler:

- Projects
- Members

---

## 9.3 WorkspaceMember

Temsil ettiği kavram:

Bir kullanıcının workspace içindeki üyeliği ve rolü.

Alanlar:

- Id
- WorkspaceId
- UserId
- Role
- JoinedAt

Role enum:

- Owner
- Manager
- Member
- Guest

---

## 9.4 Project

Temsil ettiği kavram:

Workspace altında yönetilen proje.

Alanlar:

- Id
- WorkspaceId
- Name
- Description
- Status
- CreatedByUserId
- CreatedAt
- UpdatedAt

ProjectStatus enum:

- Active
- Completed
- Archived

---

## 9.5 ProjectMember

Temsil ettiği kavram:

Kullanıcının proje içindeki üyeliği.

Alanlar:

- Id
- ProjectId
- UserId
- Role
- JoinedAt

ProjectRole enum:

- ProjectManager
- TeamMember
- Guest

---

## 9.6 TaskItem

Temsil ettiği kavram:

Proje içinde takip edilen iş kalemi.

Alanlar:

- Id
- ProjectId
- Title
- Description
- Status
- Priority
- DueDate
- AssignedUserId
- CreatedByUserId
- CreatedAt
- UpdatedAt
- CompletedAt

TaskStatus enum:

- Todo
- InProgress
- InReview
- Done
- Cancelled

Priority enum:

- Low
- Medium
- High
- Critical

---

## 9.7 Comment

Temsil ettiği kavram:

Görev altına yazılan kullanıcı yorumu.

Alanlar:

- Id
- TaskId
- UserId
- Content
- CreatedAt
- UpdatedAt

---

## 9.8 Label

Temsil ettiği kavram:

Görevleri kategorize etmek için kullanılan etiket.

Alanlar:

- Id
- ProjectId
- Name
- Color
- CreatedAt

---

## 9.9 TaskLabel

Temsil ettiği kavram:

TaskItem ve Label arasındaki many-to-many ilişki.

Alanlar:

- TaskId
- LabelId

---

## 9.10 Notification

Temsil ettiği kavram:

Kullanıcıya gösterilecek sistem bildirimi.

Alanlar:

- Id
- UserId
- Type
- Title
- Message
- IsRead
- CreatedAt
- RelatedEntityId

---

## 9.11 AuditLog

Temsil ettiği kavram:

Sistemdeki kritik değişikliklerin geçmiş kaydı.

Alanlar:

- Id
- UserId
- EntityName
- EntityId
- Action
- OldValues
- NewValues
- CreatedAt
- CorrelationId

---

## 9.12 AiSuggestion

Temsil ettiği kavram:

AI tarafından üretilen ve kullanıcı onayı bekleyen öneri.

Alanlar:

- Id
- TaskId nullable
- ProjectId
- RequestedByUserId
- InputText
- SuggestedPriority
- SuggestedLabels
- SuggestedSubtasks
- SuggestedDueDate
- Status
- ErrorMessage
- CreatedAt
- CompletedAt

AiSuggestionStatus enum:

- Pending
- Completed
- Failed
- Applied

---

# 10. API Tasarımı

## Auth

```http
POST /api/auth/register
POST /api/auth/login
GET  /api/auth/me
```

## Workspaces

```http
GET    /api/workspaces
GET    /api/workspaces/{workspaceId}
POST   /api/workspaces
PUT    /api/workspaces/{workspaceId}
DELETE /api/workspaces/{workspaceId}
```

## Workspace Members

```http
GET    /api/workspaces/{workspaceId}/members
POST   /api/workspaces/{workspaceId}/members
PUT    /api/workspaces/{workspaceId}/members/{userId}/role
DELETE /api/workspaces/{workspaceId}/members/{userId}
```

## Projects

```http
GET    /api/workspaces/{workspaceId}/projects
GET    /api/projects/{projectId}
POST   /api/workspaces/{workspaceId}/projects
PUT    /api/projects/{projectId}
DELETE /api/projects/{projectId}
```

## Project Members

```http
GET    /api/projects/{projectId}/members
POST   /api/projects/{projectId}/members
PUT    /api/projects/{projectId}/members/{userId}/role
DELETE /api/projects/{projectId}/members/{userId}
```

## Tasks

```http
GET    /api/projects/{projectId}/tasks
GET    /api/tasks/{taskId}
POST   /api/projects/{projectId}/tasks
PUT    /api/tasks/{taskId}
DELETE /api/tasks/{taskId}
PATCH  /api/tasks/{taskId}/status
PATCH  /api/tasks/{taskId}/assign
```

## Comments

```http
GET    /api/tasks/{taskId}/comments
POST   /api/tasks/{taskId}/comments
PUT    /api/comments/{commentId}
DELETE /api/comments/{commentId}
```

## Labels

```http
GET    /api/projects/{projectId}/labels
POST   /api/projects/{projectId}/labels
POST   /api/tasks/{taskId}/labels/{labelId}
DELETE /api/tasks/{taskId}/labels/{labelId}
```

## Dashboard

```http
GET /api/projects/{projectId}/dashboard
```

## Notifications

```http
GET   /api/notifications
PATCH /api/notifications/{notificationId}/read
PATCH /api/notifications/read-all
```

## AI Suggestions

```http
POST /api/projects/{projectId}/ai/task-suggestions
GET  /api/ai/suggestions/{suggestionId}
POST /api/ai/suggestions/{suggestionId}/apply
```

---

# 11. DTO Örnekleri

## RegisterRequest

```csharp
public sealed class RegisterRequest
{
    public string Email { get; init; }
    public string Password { get; init; }
}
```

## LoginRequest

```csharp
public sealed class LoginRequest
{
    public string Email { get; init; }
    public string Password { get; init; }
}
```

## CreateProjectRequest

```csharp
public sealed class CreateProjectRequest
{
    public string Name { get; init; }
    public string? Description { get; init; }
}
```

## CreateTaskRequest

```csharp
public sealed class CreateTaskRequest
{
    public string Title { get; init; }
    public string? Description { get; init; }
    public Priority Priority { get; init; } = Priority.Medium;
    public DateTime? DueDate { get; init; }
    public Guid? AssignedUserId { get; init; }
}
```

## UpdateTaskStatusRequest

```csharp
public sealed class UpdateTaskStatusRequest
{
    public TaskStatus Status { get; init; }
}
```

## AssignTaskRequest

```csharp
public sealed class AssignTaskRequest
{
    public Guid AssignedUserId { get; init; }
}
```

---

# 12. Event Tasarımı

## TaskCreatedEvent

```csharp
public sealed record TaskCreatedEvent(
    Guid TaskId,
    Guid ProjectId,
    Guid CreatedByUserId,
    Guid? AssignedUserId,
    DateTime OccurredAt
);
```

## TaskAssignedEvent

```csharp
public sealed record TaskAssignedEvent(
    Guid TaskId,
    Guid ProjectId,
    Guid AssignedUserId,
    Guid AssignedByUserId,
    DateTime OccurredAt
);
```

## CommentAddedEvent

```csharp
public sealed record CommentAddedEvent(
    Guid CommentId,
    Guid TaskId,
    Guid ProjectId,
    Guid AuthorUserId,
    DateTime OccurredAt
);
```

## ProjectUpdatedEvent

```csharp
public sealed record ProjectUpdatedEvent(
    Guid ProjectId,
    Guid UpdatedByUserId,
    DateTime OccurredAt
);
```

---

# 13. RabbitMQ Consumer'ları

## NotificationConsumer

Sorumluluk:

- TaskAssignedEvent yakalar.
- AssignedUser için notification oluşturur.
- CommentAddedEvent yakalar.
- İlgili kullanıcılara notification oluşturur.

Dikkat:

- Aynı event iki kez gelirse duplicate notification oluşmamalıdır.

---

## AuditLogConsumer

Sorumluluk:

- Kritik domain eventleri audit log kaydına dönüştürür.

Dikkat:

- EventId veya CorrelationId ile idempotency sağlanmalıdır.

---

## AgentSuggestionConsumer

Sorumluluk:

- AI suggestion isteğini işler.
- Microsoft Agent Framework ile öneri üretir.
- Sonucu AiSuggestion tablosuna yazar.
- Kullanıcıya notification oluşturur.

---

# 14. Cache Stratejisi

## Cache Edilecek Veri

İlk aşamada sadece proje dashboard'u cache edilecek.

Cache key:

```text
project-dashboard:{projectId}
```

TTL:

```text
5 dakika
```

Invalidation durumları:

- Task oluşturuldu
- Task güncellendi
- Task silindi
- Task status değişti
- Task assignment değişti
- Comment eklendi
- Label eklendi/silindi

Cache-aside pattern kullanılacak.

---

# 15. Authorization Kuralları

## Workspace Seviyesi

| İşlem | Owner | Manager | Member | Guest |
|---|---:|---:|---:|---:|
| Workspace görüntüleme | Evet | Evet | Evet | Evet |
| Workspace güncelleme | Evet | Hayır | Hayır | Hayır |
| Üye davet etme | Evet | Evet | Hayır | Hayır |
| Rol değiştirme | Evet | Hayır | Hayır | Hayır |
| Proje oluşturma | Evet | Evet | Hayır | Hayır |

## Project Seviyesi

| İşlem | Project Manager | Team Member | Guest |
|---|---:|---:|---:|
| Proje görüntüleme | Evet | Evet | Evet |
| Proje güncelleme | Evet | Hayır | Hayır |
| Görev oluşturma | Evet | Evet | Hayır |
| Görev atama | Evet | Hayır | Hayır |
| Görev status değiştirme | Evet | Kendi görevi | Hayır |
| Yorum yazma | Evet | Evet | Evet |
| Label yönetme | Evet | Hayır | Hayır |

---

# 16. Veritabanı Constraint Önerileri

## Users

- Email unique
- Email required
- PasswordHash required

## Workspaces

- Name required

## WorkspaceMembers

- Unique: WorkspaceId + UserId

## Projects

- Unique: WorkspaceId + Name
- Name required

## ProjectMembers

- Unique: ProjectId + UserId

## TaskItems

- Title required
- ProjectId FK required
- AssignedUserId nullable
- DueDate nullable

## Labels

- Unique: ProjectId + Name

## TaskLabels

- Composite primary key: TaskId + LabelId

---

# 17. Geliştirme Fazları

## Faz 1 — In-Memory API

Amaç:

Controller, routing, HTTP methodları, request/response mantığını öğrenmek.

Yapılacaklar:

- Project CRUD
- Task CRUD
- In-memory listeler
- Swagger ile manuel test

Bu fazda öğrenilecekler:

- Controller nedir?
- DTO neden kullanılır?
- HTTP status code nasıl seçilir?
- Entity direkt response olarak dönülmeli mi?

---

## Faz 2 — PostgreSQL ve EF Core

Amaç:

Kalıcı veri saklama ve relation modelleme öğrenmek.

Yapılacaklar:

- DbContext oluştur
- Entity configuration yaz
- Migration al
- PostgreSQL'e bağlan
- Project ve Task tablolarını oluştur
- Repository veya doğrudan DbContext kullanımını değerlendir

Bu fazda öğrenilecekler:

- DbContext lifetime
- Migration
- One-to-many
- Nullable FK
- Unique index
- EF tracking

---

## Faz 3 — DTO, Validation, Error Handling

Amaç:

API input/output kalitesini artırmak.

Yapılacaklar:

- Request DTO
- Response DTO
- FluentValidation
- Global exception middleware
- ProblemDetails formatı

Bu fazda öğrenilecekler:

- Entity ve DTO ayrımı
- Validation nerede yapılır?
- Exception ne zaman kullanılır?
- 400, 401, 403, 404, 409 farkı

---

## Faz 4 — Authentication & Current User

Amaç:

Kullanıcı kimliği ve JWT öğrenmek.

Yapılacaklar:

- Register
- Login
- Password hashing
- JWT üretimi
- CurrentUser service
- [Authorize] kullanımı

Bu fazda öğrenilecekler:

- Authentication vs authorization
- Claim
- Token expiration
- Secret management

---

## Faz 5 — Workspace & Project Membership

Amaç:

Gerçek authorization kuralları yazmak.

Yapılacaklar:

- Workspace entity
- WorkspaceMember entity
- ProjectMember entity
- Rol bazlı kontroller
- Kullanıcı sadece kendi workspace verisini görebilsin

Bu fazda öğrenilecekler:

- Broken access control
- Multi-tenant veri izolasyonu
- Policy-based authorization

---

## Faz 6 — Task Business Logic

Amaç:

Gerçek domain kuralları yazmak.

Yapılacaklar:

- Task oluşturma kuralları
- AssignedUser proje üyesi mi kontrolü
- Status transition kuralları
- CompletedAt yönetimi
- DueDate kontrolü

Bu fazda öğrenilecekler:

- Business rule nerede durmalı?
- Service layer ne yapar?
- Transaction ne zaman gerekir?

---

## Faz 7 — Comment & Label

Amaç:

İlişkili entity yönetimi öğrenmek.

Yapılacaklar:

- Comment CRUD
- Label CRUD
- TaskLabel many-to-many
- Yetki kontrolleri

Bu fazda öğrenilecekler:

- Many-to-many ilişki
- Composite key
- Include vs projection
- N+1 problemi

---

## Faz 8 — Dashboard & Redis Cache

Amaç:

Cache ihtiyacını gerçek endpoint üzerinde öğrenmek.

Yapılacaklar:

- Dashboard query
- Redis kurulumu
- Cache-aside pattern
- Cache invalidation
- TTL

Bu fazda öğrenilecekler:

- Cache neden kullanılır?
- Cache invalidation neden zordur?
- Stale data nedir?
- Redis neyi çözer, neyi çözmez?

---

## Faz 9 — RabbitMQ & MassTransit

Amaç:

Asenkron event-driven yapı öğrenmek.

Yapılacaklar:

- TaskCreatedEvent
- TaskAssignedEvent
- CommentAddedEvent
- Producer
- Consumer
- NotificationConsumer
- AuditLogConsumer

Bu fazda öğrenilecekler:

- Message broker neden kullanılır?
- At-least-once delivery
- Idempotency
- Retry
- Dead-letter queue

---

## Faz 10 — AI Agent

Amaç:

AI'ı oyuncağa değil, gerçek bir product feature'a dönüştürmek.

Yapılacaklar:

- AiSuggestion entity
- AI suggestion request endpoint
- AgentSuggestionConsumer
- Microsoft Agent Framework entegrasyonu
- Structured JSON response
- Kullanıcı onayı ile apply etme

Bu fazda öğrenilecekler:

- AI response validation
- Tool calling mantığı
- Prompt injection farkındalığı
- Long-running task yönetimi

---

## Faz 11 — Logging & Observability

Amaç:

Production davranışını görebilmek.

Yapılacaklar:

- Serilog
- Request logging
- Error logging
- CorrelationId
- Consumer logging
- Slow endpoint logging

Bu fazda öğrenilecekler:

- Log level
- Structured logging
- Traceability
- Debugging in production

---

## Faz 12 — Docker Compose

Amaç:

Tüm sistemi lokal production benzeri çalıştırmak.

Servisler:

- API
- PostgreSQL
- Redis
- RabbitMQ

Yapılacaklar:

- Dockerfile
- docker-compose.yml
- Environment variables
- Healthcheck

---

## Faz 13 — Testing

Amaç:

Projeyi güvenle geliştirebilmek.

Yapılacaklar:

- Unit test
- Integration test
- Testcontainers
- Authorization tests
- Validation tests
- Business rule tests

Test örnekleri:

- DueDate geçmişse task oluşturulamaz.
- Assigned user proje üyesi değilse task atanamaz.
- Guest kullanıcı task oluşturamaz.
- Dashboard cache invalidation çalışır.
- Duplicate event duplicate notification üretmez.

---

# 18. Önerilen Solution Yapısı

```text
src
 ├── TaskPilot.Api
 ├── TaskPilot.Application
 ├── TaskPilot.Domain
 ├── TaskPilot.Infrastructure
 └── TaskPilot.Persistence

tests
 ├── TaskPilot.UnitTests
 └── TaskPilot.IntegrationTests
```

## TaskPilot.Domain

İçerik:

- Entity
- Enum
- Domain exception
- Domain event
- Value object

## TaskPilot.Application

İçerik:

- Use case servisleri
- DTO
- Validation
- Interface'ler
- Authorization kontrollerinin uygulama seviyesi

## TaskPilot.Persistence

İçerik:

- DbContext
- Entity configuration
- Migration
- Repository implementasyonları

## TaskPilot.Infrastructure

İçerik:

- Redis
- RabbitMQ
- Email/Notification provider
- AI Agent adapter
- JWT provider
- Password hasher

## TaskPilot.Api

İçerik:

- Controllers
- Middleware
- Filters
- Authentication configuration
- Swagger
- DI registration

---

# 19. İlk Backlog

## Epic 1 — API Foundation

- Project oluşturma endpointi
- Project listeleme endpointi
- Task oluşturma endpointi
- Task listeleme endpointi
- Swagger testleri

## Epic 2 — Persistence

- PostgreSQL bağlantısı
- DbContext
- Migration
- Project persistence
- Task persistence

## Epic 3 — Validation & Error Handling

- FluentValidation ekleme
- CreateTask validation
- UpdateTask validation
- Global exception middleware
- ProblemDetails response

## Epic 4 — Auth

- Register
- Login
- Password hashing
- JWT
- CurrentUser

## Epic 5 — Workspace & Authorization

- Workspace CRUD
- WorkspaceMember
- ProjectMember
- Role checks
- Forbidden scenarios

## Epic 6 — Task Domain

- Assignment
- Status transition
- DueDate rule
- CompletedAt rule
- Audit event hazırlığı

## Epic 7 — Collaboration

- Comments
- Labels
- Notifications

## Epic 8 — Dashboard

- Dashboard query
- Redis cache
- Cache invalidation

## Epic 9 — Event Driven

- RabbitMQ
- MassTransit
- NotificationConsumer
- AuditLogConsumer

## Epic 10 — AI

- AiSuggestion
- Agent integration
- Apply suggestion flow

---

# 20. Örnek User Story'ler

## US-001

Bir kullanıcı olarak sisteme kayıt olmak istiyorum, böylece kendi workspace'imi oluşturabilirim.

Acceptance Criteria:

- Geçerli email ve şifre ile kayıt olabilirim.
- Aynı email ile ikinci kez kayıt olamam.
- Şifrem veritabanında açık metin olarak saklanmaz.

---

## US-002

Bir workspace owner olarak proje oluşturmak istiyorum, böylece ekibimin işlerini organize edebilirim.

Acceptance Criteria:

- Workspace içinde proje oluşturabilirim.
- Aynı workspace içinde aynı isimli iki proje oluşturamam.
- Oluşturduğum projede Project Manager olurum.

---

## US-003

Bir project manager olarak görev oluşturmak istiyorum, böylece yapılacak işleri takip edebilirim.

Acceptance Criteria:

- Görev başlığı zorunludur.
- Görevi proje üyesine atayabilirim.
- Projede olmayan bir kullanıcıya görev atayamam.
- Görev oluşturulunca dashboard güncellenir.

---

## US-004

Bir team member olarak bana atanan görevin durumunu değiştirmek istiyorum, böylece ilerlememi gösterebilirim.

Acceptance Criteria:

- Kendi görevimi InProgress yapabilirim.
- Kendi görevimi Done yapabilirim.
- Başkasının görevini atayamaz veya silemem.

---

## US-005

Bir project member olarak göreve yorum yazmak istiyorum, böylece ekip içi iletişim sağlayabilirim.

Acceptance Criteria:

- Göreve yorum yazabilirim.
- Boş yorum gönderemem.
- Proje üyesi değilsem yorum yazamam.

---

## US-006

Bir project manager olarak dashboard görmek istiyorum, böylece projenin genel durumunu takip edebilirim.

Acceptance Criteria:

- Toplam görev sayısını görebilirim.
- Tamamlanan görev sayısını görebilirim.
- Geciken görevleri görebilirim.
- Dashboard hızlı dönmelidir.

---

## US-007

Bir kullanıcı olarak AI'dan görev önerisi almak istiyorum, böylece görevleri daha hızlı ve kaliteli planlayabilirim.

Acceptance Criteria:

- Görev metni girerek öneri alabilirim.
- Sistem priority, label, subtask ve due date önerebilir.
- Önerileri onaylamadan göreve uygulamaz.

---

# 21. Dikkat Edilecek Tasarım Kararları

## 21.1 Generic Repository şart değil

EF Core zaten repository/unit of work pattern benzeri davranır. Öğrenme amacıyla repository yazılabilir ama gereksiz abstraction oluşturmamaya dikkat edilmelidir.

Başlangıç için öneri:

- Application service içinde DbContext interface'i veya repository kullanımı değerlendirilebilir.
- Gereksiz generic repository ile EF Core'un Include, projection, tracking gibi güçlü özellikleri boğulmamalıdır.

## 21.2 Controller ince kalmalı

Controller şunları yapmalı:

- Request alır.
- Current user bilgisini geçirir.
- Application service çağırır.
- Response döner.

Controller şunları yapmamalı:

- Business rule yazmamalı.
- DbContext direkt kullanmamalı.
- Password hashlememeli.
- RabbitMQ publish detaylarını bilmemeli.

## 21.3 Entity response olarak dönülmemeli

Neden:

- PasswordHash gibi alanlar sızabilir.
- API contract entity'ye bağlı kalır.
- Navigation property cycle yaratabilir.
- Gereksiz veri dönebilir.

DTO kullanılmalıdır.

## 21.4 Authorization sonradan eklenmez

Authorization projeye sonradan eklenirse kodun yarısı değişir.

Bu yüzden Faz 5'ten itibaren her endpoint için şu soru sorulmalıdır:

```text
Bu kullanıcı bu resource'a erişebilir mi?
```

## 21.5 Event publish transaction problemi

Task DB'ye kaydedilip event publish edilemezse sistem tutarsız olabilir.

İleri seviye çözüm:

- Outbox Pattern

İlk aşamada:

- Basit publish yeterlidir.
- Sonra outbox pattern araştırılıp eklenebilir.

---

# 22. Örnek Geliştirme Döngüsü

Her feature için şu sırayla ilerle:

1. User story yaz.
2. Acceptance criteria yaz.
3. Business rule çıkar.
4. Request DTO tasarla.
5. Response DTO tasarla.
6. Endpoint tasarla.
7. Entity etkisini düşün.
8. Validation yaz.
9. Authorization kontrolünü yaz.
10. Application service yaz.
11. Persistence kodunu yaz.
12. Event gerekiyorsa publish et.
13. Unit test yaz.
14. Integration test yaz.
15. Swagger/manual test yap.
16. Log ve error davranışını kontrol et.

---

# 23. İlk Başlanacak Net Görev

İlk gerçek görev:

```text
In-memory olarak Project CRUD endpointlerini yaz.
```

Ama bunu bile şu kurallarla yaz:

- Entity ve DTO ayrı olsun.
- Controller içinde direkt liste kullanabilirsin ama business logic şişirme.
- HTTP status code doğru dön.
- Create response 201 Created dönsün.
- GetById bulunamazsa 404 dönsün.
- Update bulunamazsa 404 dönsün.
- Delete idempotency davranışını düşün.

---

# 24. Mentor Promptu

Sen kıdemli bir .NET backend mimarı ve teknik mentorsun.

Bana TaskPilot projesini geliştirirken rehberlik et.

Kurallar:

- Çözümü doğrudan verme.
- Önce gereksinimi netleştir.
- Domain modelini sorgulat.
- Yanlış tasarımları açıkça eleştir.
- Best practice öner.
- Junior geliştiriciye öğretir gibi anlat.
- Kod yazmadan önce acceptance criteria iste.
- Controller içine business logic yazdırma.
- DTO/entity ayrımını korut.
- Authorization ve validation sorularını sürekli sordur.
- Redis, RabbitMQ ve AI entegrasyonlarını gerçek ihtiyaç üzerinden anlat.
- Gereksiz abstraction uyarısı yap.
- Production düşüncesi kazandır.

Her feature için şu sırayı uygula:

1. Bu feature hangi problemi çözüyor?
2. Hangi kullanıcı rolü kullanacak?
3. Acceptance criteria ne?
4. Business rules ne?
5. Endpoint ne olmalı?
6. Request/response DTO ne olmalı?
7. Entity etkisi var mı?
8. Authorization nasıl olacak?
9. Validation nasıl olacak?
10. Hangi edge case'ler var?
11. Test senaryoları ne?
12. Sonra implementasyona geç.

---

# 25. Özet

TaskPilot artık sadece teknoloji deneme projesi değildir.

Bu haliyle proje şunları öğretir:

- Gerçek requirement analizi
- Domain modelleme
- API tasarımı
- Authorization
- Validation
- Business rule yazma
- PostgreSQL ilişki tasarımı
- Redis cache
- RabbitMQ event-driven architecture
- AI Agent entegrasyonu
- Test yazma
- Production düşüncesi

En doğru geliştirme yaklaşımı:

```text
Önce küçük çalışan API.
Sonra kalıcı veri.
Sonra validation.
Sonra auth.
Sonra authorization.
Sonra business logic.
Sonra cache.
Sonra event.
Sonra AI.
Sonra test, docker ve logging ile sağlamlaştırma.
```
