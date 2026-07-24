import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../lib/useAsync';
import { Button, Card, ErrorBanner, Field, inputClasses } from '../components/ui';

export function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();

  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState<'Client' | 'Owner'>('Client');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const user = await register({ name, email, phone: phone || undefined, password, role });
      navigate(user.role === 'Owner' ? '/panel' : '/', { replace: true });
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto max-w-md">
      <Card className="p-6">
        <h1 className="text-xl font-bold text-slate-800">Crear cuenta</h1>
        <p className="mt-1 text-sm text-slate-500">Regístrate como cliente o como dueño de canchas.</p>

        <div className="mt-4 grid grid-cols-2 gap-2">
          <RoleButton active={role === 'Client'} onClick={() => setRole('Client')} emoji="👤" title="Cliente" subtitle="Quiero reservar" />
          <RoleButton active={role === 'Owner'} onClick={() => setRole('Owner')} emoji="🏢" title="Dueño" subtitle="Tengo canchas" />
        </div>

        <form className="mt-5 space-y-4" onSubmit={handleSubmit}>
          <Field label="Nombre completo">
            <input className={inputClasses} value={name} onChange={(e) => setName(e.target.value)} required />
          </Field>
          <Field label="Correo">
            <input type="email" className={inputClasses} value={email} onChange={(e) => setEmail(e.target.value)} required />
          </Field>
          <Field label="Teléfono (opcional)">
            <input className={inputClasses} value={phone} onChange={(e) => setPhone(e.target.value)} />
          </Field>
          <Field label="Contraseña" hint="Mínimo 6 caracteres">
            <input type="password" className={inputClasses} value={password} onChange={(e) => setPassword(e.target.value)} required minLength={6} />
          </Field>
          {error && <ErrorBanner message={error} />}
          <Button type="submit" className="w-full" disabled={busy}>
            {busy ? 'Creando...' : 'Crear cuenta'}
          </Button>
        </form>

        <p className="mt-4 text-center text-sm text-slate-500">
          ¿Ya tienes cuenta?{' '}
          <Link to="/login" className="font-medium text-brand-700 hover:underline">
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
      className={`rounded-xl border px-3 py-3 text-left transition ${
        active ? 'border-brand-500 bg-brand-50' : 'border-slate-200 hover:border-brand-300'
      }`}
    >
      <span className="text-xl">{emoji}</span>
      <p className="mt-1 font-medium text-slate-800">{title}</p>
      <p className="text-xs text-slate-500">{subtitle}</p>
    </button>
  );
}
