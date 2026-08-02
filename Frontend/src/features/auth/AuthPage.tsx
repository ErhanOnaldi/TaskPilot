import { Eye, EyeOff } from 'lucide-react';
import { useCallback, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { authApi, workspaceApi } from '../../api/endpoints';
import { setSession } from '../../api/apiClient';
import type { AuthResponse } from '../../api/contracts';
import { isDemoMode } from '../../api/dataSource';
import { Button } from '../../components/ui/Button';
import { useAppContext } from '../../app/AppProviders';
import { GoogleSignInButton } from './GoogleSignInButton';

const schema = z.object({ email: z.email('Geçerli bir e-posta girin.'), password: z.string().min(8, 'Şifre en az 8 karakter olmalıdır.') });

export function AuthPage() {
  const register = useLocation().pathname.endsWith('/register');
  const navigate = useNavigate();
  const { setAuthenticatedUser, theme } = useAppContext();
  const [email, setEmail] = useState(isDemoMode ? 'elif@northstar.studio' : '');
  const [password, setPassword] = useState(isDemoMode ? 'TaskPilot2026!' : '');
  const [show, setShow] = useState(false);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  const startSession = useCallback(async (auth: AuthResponse) => {
    setSession(auth);
    setAuthenticatedUser(auth.user.id, auth.user.email);
    const workspaces = await workspaceApi.list();
    const first = workspaces.items[0];
    navigate(first ? `/w/${first.id}` : '/onboarding');
  }, [navigate, setAuthenticatedUser]);
  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    const parsed = schema.safeParse({ email, password });
    if (!parsed.success) { setError(parsed.error.issues[0]?.message ?? 'Formu kontrol edin.'); return; }
    setPending(true); setError('');
    try {
      if (isDemoMode) { navigate('/w/1'); return; }
      await startSession(register ? await authApi.register(email, password) : await authApi.login(email, password));
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Giriş yapılamadı.'); }
    finally { setPending(false); }
  };
  const submitGoogle = useCallback(async (idToken: string) => {
    setPending(true); setError('');
    try { await startSession(await authApi.google(idToken)); }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Google ile giriş yapılamadı.'); }
    finally { setPending(false); }
  }, [startSession]);
  return <div className="auth-page"><section className="auth-form"><Link className="auth-brand" to="/"><span>T</span>TaskPilot</Link><form onSubmit={submit}><span className="eyebrow">MIDNIGHT ATELIER</span><h1>{register ? 'TaskPilot hesabı oluşturun' : 'TaskPilot’a giriş yapın'}</h1><p>{register ? 'Görev, bilgi ve AI çalışma alanınızı kurun.' : 'Kaldığınız yerden devam edin.'}</p><label>E-posta<input type="email" autoComplete="email" value={email} onChange={(event) => setEmail(event.target.value)} /></label><label>Şifre<span className="password-field"><input type={show ? 'text' : 'password'} autoComplete={register ? 'new-password' : 'current-password'} value={password} onChange={(event) => setPassword(event.target.value)} /><button type="button" aria-label={show ? 'Şifreyi gizle' : 'Şifreyi göster'} onClick={() => setShow((value) => !value)}>{show ? <EyeOff /> : <Eye />}</button></span></label>{error ? <div className="auth-error" role="alert">{error}</div> : null}<Button variant="primary" disabled={pending}>{pending ? 'Lütfen bekleyin…' : register ? 'Hesap oluştur' : 'Giriş yap'}</Button><GoogleSignInButton text={register ? 'signup_with' : 'signin_with'} theme={theme} pending={pending} onCredential={submitGoogle} onError={setError} /><p className="auth-swap">{register ? 'Zaten hesabınız var mı?' : 'Hesabınız yok mu?'} <Link to={register ? '/login' : '/register'}>{register ? 'Giriş yapın' : 'Hesap oluşturun'}</Link></p></form></section><aside className="auth-preview"><span className="eyebrow">GÖREV · BİLGİ · AI — TEK ÇALIŞMA ALANI</span><h2>Ekibin kararları, işleri ve bağlamı aynı yerde.</h2><div className="auth-demo"><header><span /><span /><small>atlas-web-platform / tasks</small></header>{['Password recovery teslim hatalarını araştır', 'Authentication rate limiting', 'Dashboard cache invalidation'].map((task, index) => <div key={task}><span className={index === 0 ? 'danger' : 'info'}>{index === 0 ? '◐' : '○'}</span><strong>{task}</strong><small>TP-{142 + index * 5}</small></div>)}<footer>✦ Copilot bu projenin görev ve notlarını okuyabilir.</footer></div></aside></div>;
}
