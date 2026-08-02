# TaskPilot — AI Öğrenme Döngüsü ve Doküman Rehberi

## Bu Doküman Ne İçin?

`TaskPilot_AI_Teknik_Tasarim.md` **ne** inşa edileceğini söylüyor. Bu doküman ise **nasıl öğrenerek** inşa edileceğini söylüyor: her AI fazı için okunacak resmi Microsoft dokümanları, çalıştırılacak örnekler ve tekrarlanabilir bir öğrenme döngüsü.

Kodu sen yazacaksın. Bu rehberin işi, her fazda "nereden okuyayım, neyi deneyeyim, ne zaman projeye geçeyim" sorusunu ortadan kaldırmak.

---

# 1. Ana Kaynak Haritası

Öğreneceğin her şey dört resmi kaynakta toplanıyor:

| Kaynak | Ne var | Ne zaman kullan |
| --- | --- | --- |
| **learn.microsoft.com/agent-framework** | Agent Framework'ün tüm kavramsal dokümanları: agents, tools, sessions, context providers, middleware, workflows | Her fazın "kavram" adımı |
| **learn.microsoft.com/dotnet/ai** | Microsoft.Extensions.AI, embeddings, vektör store'lar, RAG kavramları, MCP quickstart'ları | AI-0, AI-4, AI-7 |
| **github.com/microsoft/agent-framework** → `dotnet/samples` | Çalışan, derlenen örnek projeler (01-agents'tan 04-hosting'e klasörlenmiş) | Her fazın "spike" adımı; bir şey patladığında referans |
| **github.com/modelcontextprotocol/csharp-sdk** | MCP C# SDK (Microsoft + Anthropic ortak) + örnekler | AI-7 |

Doküman okuma taktikleri (Learn'e özgü):

1. **Dil seçicisi:** agent-framework sayfalarının çoğu C# / Python çift dillidir; sayfa üstündeki toggle'ın **C#**'ta olduğundan emin ol. (Google'dan gelince bazen Python versiyonuna düşersin.)
2. **Sürüm kayması:** Framework şu an 1.0.0-rc2 (prerelease). Doküman ile NuGet paketi arasında küçük API farkları çıkabilir. Fark görürsen doğruluk sırası: **samples repo (derleniyor, güncel) > API reference > kavram dokümanı**. Repo'nun release notes'unu fazlara başlarken bir kez tara.
3. **İki doküman tipi:** kavram sayfaları (neden/ne zaman) ve API reference (imzalar). Önce kavram, API reference'ı IDE'de IntelliSense'le birlikte kullan.
4. **DevUI:** Framework'ün agent'larını görsel inceleyip test edebileceğin bir preview aracı var — https://learn.microsoft.com/agent-framework/devui/ . AI-1'den itibaren debug'da işini kolaylaştırır.

---

# 2. Öğrenme Döngüsü (Her Faz İçin Aynı 5 Adım)

PRD Bölüm 22'deki feature döngüsünün öğrenme versiyonu. Bir fazı bitirmeden diğerine geçme.

```text
1. KAVRAM     → Fazın doküman listesini oku (aşağıda, faz başına ~2-4 sayfa).
               Okurken 3 soruya yazılı cevap ver:
               a) Bu soyutlama hangi problemi çözüyor?
               b) Bunu framework'süz yazsam ne yazardım? (fark = framework'ün değeri)
               c) TaskPilot'ta hangi dosyaya/katmana oturacak?

2. SPIKE      → Dokümandaki örneği solution'daki PracticeProject içinde (veya yeni bir
               TaskPilot.AiPlayground console'unda) BİREBİR çalıştır. Sonra en az 2 varyasyon
               yap (her fazın tablosunda "spike egzersizi" olarak verildi). Spike kodu çöptür;
               amaç API'nin elini öğrenmek, temiz kod değil.

3. ENTEGRE    → TaskPilot_AI_Teknik_Tasarim.md'deki ilgili fazı uygula. Artık dokümandan
               kopyalamıyorsun; API'yi spike'ta öğrendin, şimdi kendi mimarine uyduruyorsun.
               (Application katmanı framework'ü görmez — bu kural entegrasyonun sınavıdır.)

4. DOĞRULA    → Tasarım dokümanındaki "bitti kriteri"ni çalıştır + logları oku.
               Patlarsa: samples repo'daki en yakın örnekle kendi kodunu diff'le.

5. PEKIŞTIR   → Kurs yazıyorsun: fazın ders notunu KENDİ kelimelerinle yaz (Feynman tekniği).
               "Bunu bir junior'a nasıl anlatırdım?" Yazamadığın yer = anlamadığın yer;
               oraya geri dön. Bu notlar aynı zamanda kursunun ham içeriği olur.
```

Tempo önerisi: AI-0 ve AI-1 birer hafta; AI-4, AI-5, AI-6 birer-buçuk hafta; toplam ~10-12 hafta rahat bir ritim. Acele etme — "yavaş ve emin adımlar" senin şartındı.

İsteğe bağlı 6. adım: PRD Bölüm 24'teki mentor promptunu kullan — fazı bitirince bir AI oturumuna "bu fazda şunu yaptım, tasarım kararlarımı sorgula" diye anlat. Açıklayamadığın karar, anlamadığın karardır.

---

# 3. Faz → Doküman Eşlemesi

## AI-0 — IChatClient Temeli

| Adım | Kaynak |
| --- | --- |
| Kavram | https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai (ana sayfa — IChatClient, pipeline, DI) |
| Kavram | https://learn.microsoft.com/dotnet/ai/iembeddinggenerator (şimdilik göz at, AI-4'te dönersin) |
| Kavram | https://learn.microsoft.com/agent-framework/overview/ (framework'ün kuşbakışı haritası) |
| Spike | Console'da OllamaSharp ile IChatClient kur, tek soru sor. |

Spike egzersizleri: (1) `UseLogging()` ekle, log çıktısındaki request/response gövdesini incele. (2) Aynı kodu config değişikliğiyle OpenAI'a yönlendir — kaç satır değişti? (3) `response.Usage`'ı yazdır; aynı soruyu uzun/kısa promptla sor, token farkını gör.

## AI-1 — İlk Agent (Task Suggestion)

| Adım | Kaynak |
| --- | --- |
| Kavram | https://learn.microsoft.com/agent-framework/get-started/ → Step 1 "Your First Agent" |
| Kavram | https://learn.microsoft.com/agent-framework/agents/ (agent kavramı, runtime döngüsü) |
| Kavram | https://learn.microsoft.com/agent-framework/agents/providers/openai (ChatCompletion vs Responses ayrımı burada) |
| Spike | Samples repo → `dotnet/samples` başlangıç örnekleri |

Spike egzersizleri: (1) Aynı agent'a farklı `instructions` ver, davranış farkını gözle. (2) `RunAsync<T>` ile typed structured output al; record'a alan ekle/çıkar, ne oluyor? (3) Kasıtlı bozuk şema iste (imkansız alan), hata davranışını gör — TaskPilot'taki `Status=Failed` yolunun provası.

Entegrasyonda dikkat: sistem promptu dosyada, `{today}` enjeksiyonu, consumer'da timeout + idempotency.

## AI-2 — Function Tools

| Adım | Kaynak |
| --- | --- |
| Kavram | https://learn.microsoft.com/agent-framework/get-started/ → Step 2 "Add Tools" |
| Kavram | https://learn.microsoft.com/agent-framework/agents/tools/ (tool kavramı, AIFunctionFactory, onay mekanizması) |
| Spike | Console'da 2 tool'lu agent: `GetWeather(city)` + `GetTime()` gibi sahte tool'lar |

Spike egzersizleri: (1) Tool'un `[Description]`'ını sil — agent tool'u ne kadar isabetli seçiyor? (Şema kalitesinin önemi.) (2) Tool'a log koy: agent bir soruda kaç kez, hangi sırayla çağırıyor? (3) Tool'dan exception fırlat — agent nasıl toparlıyor?

Entegrasyonda dikkat: tool'ların projectId ile scope'lanması — dokümanda yok, senin mimarinin kuralı. Doküman sana API'yi öğretir; güvenlik sınırını tasarım dokümanın çizer.

## AI-3 — Middleware ve Gözlemlenebilirlik

| Adım | Kaynak |
| --- | --- |
| Kavram | https://learn.microsoft.com/agent-framework/agents/middleware/ (üç middleware tipi) |
| Kavram | https://learn.microsoft.com/agent-framework/journey/adding-middleware (neden/ne zaman anlatımı — "journey" serisi kavramsal olarak en iyi yazılmış bölüm) |
| Kavram | https://learn.microsoft.com/agent-framework/agents/agent-pipeline (middleware + context provider + chat client'ın büyük resmi — bu sayfayı AI-5'te tekrar okuyacaksın) |
| Spike | Bir `DelegatingChatClient` yaz: her çağrının süresini ve token'ını console'a bas |

Spike egzersizleri: (1) İki middleware'i farklı sırada kaydet, log sırasından zinciri çıkar. (2) Middleware'den `next()`'i çağırmadan dön — ne olur? (terminate mantığı). (3) Function middleware ile tool argümanlarını logla.

## AI-4 — Embeddings, pgvector, Semantic Search

| Adım | Kaynak |
| --- | --- |
| Kavram | https://learn.microsoft.com/dotnet/ai/conceptual/embeddings (embedding sezgisi) |
| Kavram | https://learn.microsoft.com/dotnet/ai/conceptual/rag (RAG mimarisi) |
| Kavram | https://learn.microsoft.com/dotnet/ai/vector-stores/overview (vektör DB'ler, MEVD soyutlaması) |
| Kavram | https://github.com/pgvector/pgvector-dotnet → README'nin "EF Core" bölümü (Pgvector.EntityFrameworkCore — Learn'de değil, resmi pgvector repo'sunda) |
| Uygulamalı | https://learn.microsoft.com/dotnet/ai/vector-stores/tutorial-vector-search (uçtan uca tutorial — spike olarak birebir yap) |
| Spike | Tutorial + kendi mini deneyin |

Spike egzersizleri: (1) "login hatası" ile "kullanıcı giriş yapamıyor" cümlelerini embed et, cosine distance'ı yazdır; sonra alakasız bir cümleyle karşılaştır — eşik sezgisi buradan gelir. (2) Aynı metni iki farklı embedding modeliyle embed et, dimension farkını gör. (3) 20-30 örnek görev başlığını embed edip top-3 aramayı elle doğrula.

Entegrasyonda dikkat: metadata filtresi (ProjectId) her zaman vektör aramadan önce; ingestion'ın mevcut MassTransit consumer desenine binmesi.

## AI-5 — Session, Context Provider, RAG'li Copilot

| Adım | Kaynak |
| --- | --- |
| Kavram | https://learn.microsoft.com/agent-framework/get-started/ → Step 3 "Multi-Turn" + Step 4 "Memory" |
| Kavram | https://learn.microsoft.com/agent-framework/agents/conversations/session (AgentSession, serialize/deserialize) |
| Kavram | https://learn.microsoft.com/agent-framework/journey/adding-context-providers (kavramsal — önce bunu oku) |
| Kavram | https://learn.microsoft.com/agent-framework/agents/conversations/context-providers (custom provider yazımı — sonra bunu) |
| Kavram | https://learn.microsoft.com/agent-framework/agents/conversations/compaction (context window yönetimi) |
| Referans | https://learn.microsoft.com/agent-framework/integrations/ (hazır chat history / memory provider listesi — InMemoryChatHistoryProvider kaynak koduna bak, Postgres versiyonunu ona bakarak yazacaksın) |
| Spike | Console'da: session'lı çok turlu sohbet + tek bir custom AIContextProvider |

Spike egzersizleri: (1) Provider'ın `ProvideAIContextAsync`'inde sabit bir cümle enjekte et ("kullanıcının adı Erhan"), agent'ın hatırladığını doğrula. (2) `Serialize()` çıktısını dosyaya yaz, programı kapat-aç, session'ı geri yükle. (3) 30-40 turluk yapay sohbet üret, token büyümesini logla — compaction ihtiyacını kendi gözünle gör.

## AI-6 — Workflows

| Adım | Kaynak |
| --- | --- |
| Kavram | https://learn.microsoft.com/agent-framework/workflows/ (genel bakış + agent vs workflow kararı) |
| Kavram | https://learn.microsoft.com/agent-framework/workflows/executors ve .../edges (fan-out/fan-in, switch-case buradadır) |
| Kavram | https://learn.microsoft.com/agent-framework/workflows/events (ilerleme gözlemi) |
| Kavram | https://learn.microsoft.com/agent-framework/workflows/checkpoints (checkpoint/resume) |
| Spike | Samples repo → `dotnet/samples/03-workflows` (başlangıç örnekleri + ConditionalEdges/SwitchCase) |

Spike egzersizleri: (1) 2 executor'lu sıralı workflow — biri saf fonksiyon, biri agent. (2) Samples'taki spam-detection switch-case örneğini çalıştır, üçüncü bir case ekle. (3) Fan-out/fan-in: aynı soruya 2 farklı promptlu agent paralel cevap versin, üçüncüsü birleştirsin — Weekly Report'un minyatürü.

## AI-7 — MCP Server

| Adım | Kaynak |
| --- | --- |
| Kavram | https://learn.microsoft.com/dotnet/ai/get-started-mcp (MCP + C# SDK genel bakış) |
| Uygulamalı | https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server (minimal server — spike olarak birebir yap) |
| Uygulamalı | https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-client (client tarafı) |
| Referans | https://learn.microsoft.com/dotnet/ai/resources/mcp-servers (derlenmiş kaynak listesi; mcp-for-beginners repo'su dahil) |
| Referans | https://modelcontextprotocol.github.io/csharp-sdk/ (SDK API dokümanı) |
| Spike | Quickstart'taki stdio server'ı yap, Claude Code/VS Code'a bağla |

Spike egzersizleri: (1) Server'ına ikinci bir tool ekle, istemcinin discovery ile otomatik görmesini izle. (2) MCP Inspector ile JSON-RPC trafiğini incele — "şema tabanlı I/O" lafı somutlaşır. (3) Quickstart stdio kullanır; TaskPilot entegrasyonu HTTP (ASP.NET Core) olacak — `ModelContextProtocol.AspNetCore` README'sindeki farkı not et.

## AI-8 — A2A

| Adım | Kaynak |
| --- | --- |
| Kavram (tüketme) | https://learn.microsoft.com/agent-framework/agents/providers/agent-to-agent (A2AAgent — uzak agent'ı AIAgent olarak sarmalamak) |
| Kavram (yayınlama) | https://learn.microsoft.com/agent-framework/integrations/a2a (kendi agent'ını A2A endpoint'i yapmak — paketler: `Microsoft.Agents.AI.Hosting.A2A` + `.AspNetCore`) |
| Kavram | https://a2a-protocol.org/latest/ (protokolün kendisi, agent card şeması) |
| Spike | Samples repo → `dotnet/samples/04-hosting` altındaki A2A örnekleri |

Spike egzersizi: iki console app — biri basit agent'ı A2A ile host etsin, diğeri `/.well-known/agent-card.json`'dan keşfedip sorsun.

---

# 4. Kapsam Dışı Kalanlar İçin Not

Kurs içeriğinde olup bu döngüde bilinçli atlananlar (Azure aboneliği gerektirenler): Foundry provider, DefaultAzureCredential/ManagedIdentity, hosted araçlar (code interpreter, file search, web search). Bunların dokümanları aynı sitede (`/agent-framework/agents/providers/microsoft-foundry`); mimarin provider-bağımsız olduğu için gün gelir gerekirse okuyup config'le eklersin — yeni kavram öğrenmen gerekmez, sadece yeni provider.

---

# 5. Özet Kontrol Listesi

Her faz için işaretle:

```text
[ ] Kavram dokümanlarını okudum, 3 soruya yazılı cevap verdim
[ ] Spike'ı çalıştırdım + en az 2 varyasyon denedim
[ ] TaskPilot'a tasarım dokümanına uygun entegre ettim (Application katmanı framework'süz)
[ ] Bitti kriterini çalıştırdım, logları okudum
[ ] Ders notumu kendi kelimelerimle yazdım
```
