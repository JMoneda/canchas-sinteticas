import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../lib/useAsync';
import { Button, Card, ErrorBanner, Field, inputClasses } from '../components/ui';

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from;

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const user = await login(email, password);
      navigate(from ?? (user.role === 'Owner' ? '/panel' : '/'), { replace: true });
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  function fill(demoEmail: string) {
    setEmail(demoEmail);
    setPassword('password123');
  }

  return (
    <div className="mx-auto max-w-md">
      <Card className="p-6">
        <h1 className="text-xl font-bold text-slate-800">Iniciar sesión</h1>
        <p className="mt-1 text-sm text-slate-500">Ingresa a tu cuenta para reservar o gestionar tus canchas.</p>

        <form className="mt-5 space-y-4" onSubmit={handleSubmit}>
          <Field label="Correo">
            <input
              type="email"
              className={inputClasses}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </Field>
          <Field label="Contraseña">
            <input
              type="password"
              className={inputClasses}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </Field>
          {error && <ErrorBanner message={error} />}
          <Button type="submit" className="w-full" disabled={busy}>
            {busy ? 'Ingresando...' : 'Ingresar'}
          </Button>
        </form>

        <p className="mt-4 text-center text-sm text-slate-500">
          ¿No tienes cuenta?{' '}
          <Link to="/registro" className="font-medium text-brand-700 hover:underline">
            Crear cuenta
          </Link>
        </p>
      </Card>

      <Card className="mt-4 p-4">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Cuentas de demostración</p>
        <div className="mt-2 space-y-1.5 text-sm">
          <button onClick={() => fill('cliente@canchas.co')} className="block w-full text-left text-slate-600 hover:text-brand-700">
            👤 Cliente · <span className="font-mono">cliente@canchas.co</span>
          </button>
          <button onClick={() => fill('owner1@canchas.co')} className="block w-full text-left text-slate-600 hover:text-brand-700">
            🏢 Dueño · <span className="font-mono">owner1@canchas.co</span>
          </button>
          <p className="text-xs text-slate-400">Contraseña para todas: password123</p>
        </div>
      </Card>
    </div>
  );
}
