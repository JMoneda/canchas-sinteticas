import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../lib/useAsync';
import { useFormValidation } from '../hooks/useFormValidation';
import {
  validateRequired,
  validateEmail,
  validatePhone,
  validatePasswordPolicy,
  validateMatch,
  passwordStrength,
} from '../lib/validation';
import {
  Button,
  Card,
  ErrorBanner,
  Field,
  TextInput,
  PasswordField,
  PasswordStrengthMeter,
} from '../components/ui';

interface RegisterValues {
  name: string;
  email: string;
  phone: string;
  password: string;
  confirmPassword: string;
  [key: string]: string;
}

export function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();

  const [role, setRole] = useState<'Client' | 'Owner'>('Client');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const form = useFormValidation<RegisterValues>({
    initial: { name: '', email: '', phone: '', password: '', confirmPassword: '' },
    validators: {
      name: (v) => validateRequired(v, 'El nombre'),
      email: (v) => validateEmail(v),
      phone: (v) => validatePhone(v),
      password: (v) => validatePasswordPolicy(v),
      confirmPassword: (v, all) => validateMatch(all.password, v),
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
      const user = await register({
        name: values.name,
        email: values.email,
        phone: values.phone || undefined,
        password: values.password,
        role,
      });
      navigate(user.role === 'Owner' ? '/panel' : '/', { replace: true });
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto max-w-md">
      <Card className="p-6 sm:p-8">
        <h1 className="text-2xl font-bold text-slate-900">Crear cuenta</h1>
        <p className="mt-1 text-sm text-slate-500">Regístrate como cliente o como dueño de canchas.</p>

        <div className="mt-5 grid grid-cols-2 gap-2">
          <RoleButton active={role === 'Client'} onClick={() => setRole('Client')} emoji="👤" title="Cliente" subtitle="Quiero reservar" />
          <RoleButton active={role === 'Owner'} onClick={() => setRole('Owner')} emoji="🏢" title="Dueño" subtitle="Tengo canchas" />
        </div>

        <form className="mt-6 space-y-4" onSubmit={handleSubmit} noValidate>
          <Field label="Nombre completo" required error={errors.name}>
            <TextInput
              value={values.name}
              onChange={(e) => setValue('name', e.target.value)}
              onBlur={() => setTouched('name')}
              error={errors.name}
              autoComplete="name"
            />
          </Field>

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

          <Field label="Teléfono" hint="Opcional" error={errors.phone}>
            <TextInput
              value={values.phone}
              onChange={(e) => setValue('phone', e.target.value)}
              onBlur={() => setTouched('phone')}
              error={errors.phone}
              autoComplete="tel"
              placeholder="Ej. +57 300 123 4567"
            />
          </Field>

          <div>
            <Field label="Contraseña" required error={errors.password} hint="Mínimo 8 caracteres, con letras y números">
              <PasswordField
                value={values.password}
                onChange={(e) => setValue('password', e.target.value)}
                onBlur={() => setTouched('password')}
                error={errors.password}
                autoComplete="new-password"
              />
            </Field>
            {values.password.length > 0 && <PasswordStrengthMeter score={passwordStrength(values.password)} />}
          </div>

          <Field label="Confirmar contraseña" required error={errors.confirmPassword}>
            <PasswordField
              value={values.confirmPassword}
              onChange={(e) => setValue('confirmPassword', e.target.value)}
              onBlur={() => setTouched('confirmPassword')}
              error={errors.confirmPassword}
              autoComplete="new-password"
            />
          </Field>

          {error && <ErrorBanner message={error} />}

          <Button type="submit" size="lg" className="w-full" loading={busy} disabled={!isValid}>
            {busy ? 'Creando...' : 'Crear cuenta'}
          </Button>
        </form>

        <p className="mt-5 text-center text-sm text-slate-500">
          ¿Ya tienes cuenta?{' '}
          <Link to="/login" className="font-semibold text-brand-700 hover:underline">
            Ingresar
          </Link>
        </p>
      </Card>
    </div>
  );
}

function RoleButton({
  active,
  onClick,
  emoji,
  title,
  subtitle,
}: {
  active: boolean;
  onClick: () => void;
  emoji: string;
  title: string;
  subtitle: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={active}
      className={`rounded-[var(--radius-control)] border px-3 py-3 text-left transition-all ${
        active
          ? 'border-brand-500 bg-brand-50 ring-2 ring-brand-500/20'
          : 'border-slate-200 hover:border-brand-300 hover:bg-slate-50'
      }`}
    >
      <span className="text-xl">{emoji}</span>
      <p className="mt-1 font-semibold text-slate-800">{title}</p>
      <p className="text-xs text-slate-500">{subtitle}</p>
    </button>
  );
}
