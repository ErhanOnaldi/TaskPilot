export interface GoogleCredentialResponse {
  credential?: string;
}

export interface GoogleButtonOptions {
  type: 'standard';
  theme: 'outline' | 'filled_black';
  size: 'large';
  shape: 'rectangular';
  text: 'signin_with' | 'signup_with' | 'continue_with';
  logo_alignment: 'left' | 'center';
  locale: string;
  width: number;
}

interface GoogleIdentityApi {
  initialize(config: {
    client_id: string;
    callback: (response: GoogleCredentialResponse) => void;
    auto_select?: boolean;
    cancel_on_tap_outside?: boolean;
    use_fedcm_for_prompt?: boolean;
  }): void;
  renderButton(parent: HTMLElement, options: GoogleButtonOptions): void;
  disableAutoSelect(): void;
}

declare global {
  interface Window {
    google?: { accounts: { id: GoogleIdentityApi } };
  }
}

/** Public by design: the OAuth client id ships with the bundle, the API still verifies every token. */
export const googleClientId = (import.meta.env.VITE_GOOGLE_CLIENT_ID ?? '').trim();

const SCRIPT_SRC = 'https://accounts.google.com/gsi/client';
let pendingLoad: Promise<GoogleIdentityApi> | null = null;

export function loadGoogleIdentity(): Promise<GoogleIdentityApi> {
  if (window.google?.accounts.id) return Promise.resolve(window.google.accounts.id);
  if (pendingLoad) return pendingLoad;

  pendingLoad = new Promise<GoogleIdentityApi>((resolve, reject) => {
    // A tag left behind by a blocked or failed attempt never fires load again, so it is replaced.
    document.querySelector<HTMLScriptElement>(`script[src="${SCRIPT_SRC}"]`)?.remove();
    const script = document.createElement('script');
    const fail = () => reject(new Error('Google Identity Services yüklenemedi.'));
    const timeout = window.setTimeout(fail, 10_000);
    script.addEventListener('load', () => {
      window.clearTimeout(timeout);
      const api = window.google?.accounts.id;
      if (api) resolve(api);
      else fail();
    }, { once: true });
    script.addEventListener('error', () => {
      window.clearTimeout(timeout);
      fail();
    }, { once: true });
    script.src = SCRIPT_SRC;
    script.async = true;
    script.defer = true;
    document.head.append(script);
  }).catch((reason: unknown) => {
    pendingLoad = null;
    throw reason;
  });

  return pendingLoad;
}

export function signOutOfGoogle() {
  window.google?.accounts.id.disableAutoSelect();
}
