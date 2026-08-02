# Google ile Giriş — Kurulum

Kod tarafı hazır. Çalışması için tek gereken bir Google OAuth client id oluşturup üç yere tanımlamak.

## 1. Google Cloud Console'da client id oluştur

1. https://console.cloud.google.com/apis/credentials adresine gir, projeyi seç (yoksa oluştur).
2. **OAuth consent screen**: User type `External`, uygulama adı `TaskPilot`, destek e-postası kendi adresin. Test aşamasında `Testing` modunda kalabilir; kendi Google hesabını **Test users** listesine ekle. Herkese açık olacaksa `Publish app` de.
3. **Credentials → Create credentials → OAuth client ID → Web application**.
4. **Authorized JavaScript origins** (redirect URI'ye gerek yok, Google Identity Services popup akışı kullanılıyor):
   - `https://taskpilot-web.pages.dev`
   - `http://localhost:5173`
5. Oluşan **Client ID**'yi kopyala (`...apps.googleusercontent.com`). Client secret bu akışta kullanılmıyor.

## 2. Backend (Render)

Render → `taskpilot-api` → Environment:

```
Authentication__Google__ClientId = <client-id>
```

`render.yaml` içinde `sync: false` olarak tanımlı, yani değeri panelden girmen gerekiyor. Değer boş bırakılırsa `/api/auth/google` `503` döner ve özellik kapalı kalır.

## 3. Frontend (GitHub Actions → Cloudflare Pages)

GitHub repo → Settings → Secrets and variables → Actions → **Variables** sekmesi:

```
GOOGLE_OAUTH_CLIENT_ID = <client-id>
```

Client id public bir değerdir (bundle'a gömülür), secret olması gerekmez. Değişken tanımlı değilse Google butonu render edilmez, e-posta/şifre girişi normal çalışır.

## 4. Lokal geliştirme

- Backend: `TaskPilot.API/appsettings.Development.json` içindeki `Authentication:Google:ClientId` alanını doldur ya da `TASKPILOT_GOOGLE_CLIENT_ID` ile docker-compose'a ver.
- Frontend: `Frontend/.env.local` dosyasına `VITE_GOOGLE_CLIENT_ID=<client-id>` yaz. Ayrıca `VITE_DEMO_MODE=false` olmalı, aksi halde form gerçek API'ye gitmez.

## Akış ve davranış

- Tarayıcı Google Identity Services ile bir **ID token** alır, `POST /api/auth/google` gövdesinde `{ "idToken": "..." }` olarak gönderir.
- API token'ı `Google.Apis.Auth` ile doğrular: imza, issuer, süre ve audience (client id) kontrol edilir. Doğrulanmamış e-postalar reddedilir.
- Kullanıcı eşleştirme sırası: önce `GoogleSubject`, sonra e-posta. Aynı e-postaya sahip mevcut hesap varsa Google o hesaba bağlanır (şifresi korunur). Hiçbiri yoksa şifresiz yeni kullanıcı açılır ve `201` döner.
- Yanıt normal login ile aynıdır: access token + refresh token, yani mevcut oturum/rotasyon mantığı değişmedi.
- Sadece Google ile açılmış bir hesap şifreyle giriş denerse `401` ve "Google ile devam edin" mesajı döner.

## Veritabanı

`20260802191923_AddGoogleIdentityToUsers` migration'ı `Users.GoogleSubject` kolonunu (unique, filtered index) ekler ve `PasswordHash` kolonunu nullable yapar. Render'da `Persistence__ApplyMigrationsOnStartup=true` olduğu için deploy sırasında otomatik uygulanır.
