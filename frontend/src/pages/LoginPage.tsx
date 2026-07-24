import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../lib/useAsync';
import { useFormValidation } from '../hooks/useFormValidation';
import { validateEmail, validateRequired } from '../lib/validation';
import { Button, Card, ErrorBanner, Field, TextInput, PasswordField } from '../components/ui';

interface LoginValues {
  email: string;
  password: string;
  [key: string]: string;
}

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from;

  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const form = useFormValidation<LoginValues>({
    initial: { email: '', password: '' },
    validators: {
      email: (v) => validateEmail(v),
      password: (v) => validateRequired(v, 'La contraseña'),
    },
  });
  const { values, errors, isValid, setValue, setTouched } = form;

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.validateAll()) {
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const user = await login(values.email, values.password);
      navigate(from ?? (user.role === 'Owner' ? '/panel' : '/'), { replace: true });
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  function fill(demoEmail: string) {
    setValue('email', demoEmail);
    setValue('password', 'password123');
  }

  return (
    <div className="mx-auto max-w-md">
      <Card className="p-6 sm:p-8">
        <h1 className="text-2xl font-bold text-slate-900">Iniciar sesión</h1>
        <p className="mt-1 text-sm text-slate-500">Ingresa a tu cuenta para reservar o gestionar tus canchas.</p>

        <form className="mt-6 space-y-4" onSubmit={handleSubmit} noValidate>
          <Field label="Correo" required error={errors.email}>
            <TextInput
              type="email"
              value={values.email}
              onChange={(e) => setValue('email', e.target.value)}
              onBlur={() => setTouched('email')}
              error={errors.email}
              autoComplete="email"
            />
          </Field>
          <Field label="Contraseña" required error={errors.password}>
            <PasswordField
              value={values.password}
              onChange={(e) => setValue('password', e.target.value)}
              onBlur={() => setTouched('password')}
              error={errors.password}
              autoComplete="current-password"
            />
          </Field>
          {error && <ErrorBanner message={error} />}
          <Button type="submit" size="lg" className="w-full" loading={busy} disabled={!isValid}>
            {busy ? 'Ingresando...' : 'Ingresar'}
          </Button>
        </form>

        <p className="mt-5 text-center text-sm text-slate-500">
          ¿No tienes cuenta?{' '}
          <Link to="/registro" className="font-semibold text-brand-700 hover:underline">
            Crear cuenta
          </Link>
        </p>
      </Card>

      <Card className="mt-4 p-4">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Cuentas de demostración</p>
        <div className="mt-2 space-y-1.5 text-sm">
          <button onClick={() => fill('cliente@canchas.co')} className="block w-full rounded-md text-left text-slate-600 hover:text-brand-700">
            👤 Cliente · <span className="font-mono">cliente@canchas.co</span>
          </button>
          <button onClick={() => fill('owner1@canchas.co')} className="block w-full rounded-md text-left text-slate-600 hover:text-brand-700">
            🏢 Dueño · <span className="font-mono">owner1@canchas.co</span>
          </button>
          <p className="text-xs text-slate-400">Contraseña para todas: password123</p>
        </div>
      </Card>
    </div>
  );
}
