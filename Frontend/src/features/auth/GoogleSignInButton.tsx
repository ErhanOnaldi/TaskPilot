import { useEffect, useRef, useState } from 'react';
import { googleClientId, loadGoogleIdentity, type GoogleButtonOptions } from './googleIdentity';

interface GoogleSignInButtonProps {
  text: GoogleButtonOptions['text'];
  theme: 'dark' | 'light';
  pending: boolean;
  onCredential: (idToken: string) => void;
  onError: (message: string) => void;
}

export function GoogleSignInButton({ text, theme, pending, onCredential, onError }: GoogleSignInButtonProps) {
  const hostRef = useRef<HTMLDivElement>(null);
  const credentialRef = useRef(onCredential);
  const errorRef = useRef(onError);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    credentialRef.current = onCredential;
    errorRef.current = onError;
  }, [onCredential, onError]);

  useEffect(() => {
    if (!googleClientId) return;
    let cancelled = false;

    loadGoogleIdentity()
      .then((identity) => {
        const host = hostRef.current;
        if (cancelled || !host) return;
        identity.initialize({
          client_id: googleClientId,
          callback: (response) => {
            if (response.credential) credentialRef.current(response.credential);
            else errorRef.current('Google oturumu doğrulanamadı.');
          },
          cancel_on_tap_outside: true,
        });
        // Re-rendering appends a second iframe, so the host is cleared on every pass.
        host.replaceChildren();
        // Google clamps the button between 200 and 400px; CSS below stretches it to the form width.
        const measured = Math.round(host.parentElement?.getBoundingClientRect().width ?? 0);
        identity.renderButton(host, {
          type: 'standard',
          theme: theme === 'dark' ? 'filled_black' : 'outline',
          size: 'large',
          shape: 'rectangular',
          text,
          logo_alignment: 'left',
          locale: 'tr',
          width: Math.min(400, Math.max(200, measured || 370)),
        });
        setReady(true);
      })
      .catch(() => {
        if (!cancelled) errorRef.current('Google giriş servisi yüklenemedi. Bağlantınızı kontrol edin.');
      });

    return () => {
      cancelled = true;
    };
  }, [text, theme]);

  if (!googleClientId) return null;

  return (
    <div className="auth-google">
      <div className="auth-divider"><span>veya</span></div>
      <div className="auth-google__button" data-pending={pending || !ready ? 'true' : undefined}>
        <div ref={hostRef} />
        {ready ? null : <span className="auth-google__placeholder">Google yükleniyor…</span>}
      </div>
    </div>
  );
}
