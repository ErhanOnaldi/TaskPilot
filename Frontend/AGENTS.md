# Frontend Agent Instructions

Bu klasörde çalışacak her ajan önce `FRONTEND_IMPLEMENTATION_HANDOFF.md` dosyasını tamamen okumalıdır.

- `TaskPilot.dc.html` onaylanan görsel prototiptir; görsel yönü yeniden tasarlama.
- Prototipi iframe veya HTML string olarak React'e taşıma; reusable React + TypeScript component'lerine dönüştür.
- `support.js` production bağımlılığı değildir.
- Prototype toolbar, rol/viewport simülatörü, `SKELETON` butonu ve `backend dependency` metinleri production UI'a çıkmamalıdır.
- Backend endpoint ve DTO'larını tahmin etme; mevcut OpenAPI/controller sözleşmeleriyle doğrula.
- Yetki, task transition, note concurrency ve AI apply kurallarını handoff dosyasındaki haliyle uygula.
- Eksik read-contract'leri N+1 istekler veya sahte production verisiyle gizleme.
- TypeScript strict, responsive web, WCAG 2.2 AA ve klavye erişimi zorunludur.
- `.env`, token, API key veya secret commit etme.
- Kullanıcı ayrıca istemedikçe backend projelerine dokunma.
