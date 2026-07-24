import type { ButtonHTMLAttributes, InputHTMLAttributes, ReactNode } from 'react';
import { useEffect, useId, useState } from 'react';
import { passwordStrengthLabel } from '../lib/validation';

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger';
type Size = 'sm' | 'md' | 'lg';

const variantClasses: Record<Variant, string> = {
  primary:
    'bg-brand-600 text-white shadow-soft hover:bg-brand-700 active:bg-brand-800 disabled:bg-brand-300 disabled:shadow-none',
  secondary:
    'bg-white text-slate-700 border border-slate-300 hover:border-slate-400 hover:bg-slate-50 disabled:opacity-60',
  ghost: 'bg-transparent text-slate-600 hover:bg-slate-100 hover:text-slate-900',
  danger: 'bg-rose-600 text-white shadow-soft hover:bg-rose-700 active:bg-rose-800 disabled:bg-rose-300',
};

const sizeClasses: Record<Size, string> = {
  sm: 'px-3 py-1.5 text-sm',
  md: 'px-4 py-2.5 text-sm',
  lg: 'px-6 py-3 text-base',
};

const baseButton =
  'inline-flex items-center justify-center gap-2 rounded-[var(--radius-control)] font-semibold transition-all duration-150 focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-500/40 disabled:cursor-not-allowed';

/** Clases de un botón; útil para estilar un `<Link>`/`<a>` como botón. */
export function buttonClasses(variant: Variant = 'primary', size: Size = 'md', extra = ''): string {
  return `${baseButton} ${variantClasses[variant]} ${sizeClasses[size]} ${extra}`;
}

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  size?: Size;
  loading?: boolean;
}

export function Button({ variant = 'primary', size = 'md', loading = false, className = '', children, disabled, ...props }: ButtonProps) {
  return (
    <button className={buttonClasses(variant, size, className)} disabled={disabled || loading} {...props}>
      {loading && <Spinner.Inline />}
      {children}
    </button>
  );
}

/** Overlay + tarjeta centrada para modales. Cierra al hacer clic fuera o con Escape. */
export function ModalShell({
  children,
  onClose,
  size = 'lg',
}: {
  children: ReactNode;
  onClose: () => void;
  size?: 'md' | 'lg';
}) {
  const maxWidth = size === 'md' ? 'max-w-md' : 'max-w-lg';
  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        onClose();
      }
    }
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [onClose]);

  return (
    <div
      className="fixed inset-0 z-30 flex items-center justify-center bg-slate-950/50 p-4 backdrop-blur-sm"
      onClick={onClose}
      role="presentation"
    >
      <Card
        className={`max-h-[90vh] w-full ${maxWidth} overflow-y-auto p-6 shadow-[var(--shadow-lifted)]`}
      >
        <div role="dialog" aria-modal="true" onClick={(e) => e.stopPropagation()}>
          {children}
        </div>
      </Card>
    </div>
  );
}

export function Badge({ children, className = '' }: { children: ReactNode; className?: string }) {
  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ${className}`}>
      {children}
    </span>
  );
}

export function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
  return (
    <div className={`rounded-[var(--radius-card)] border border-slate-200/80 bg-white shadow-[var(--shadow-card)] ${className}`}>
      {children}
    </div>
  );
}

export function Spinner({ label }: { label?: string }) {
  return (
    <div className="flex items-center justify-center gap-3 py-12 text-slate-500">
      <span className="h-5 w-5 animate-spin rounded-full border-2 border-slate-200 border-t-brand-600" />
      {label && <span className="text-sm">{label}</span>}
    </div>
  );
}

/** Spinner en línea (para botones en estado loading). */
Spinner.Inline = function InlineSpinner() {
  return <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-r-transparent opacity-80" aria-hidden />;
};

export function Skeleton({ className = '' }: { className?: string }) {
  return <div className={`animate-pulse rounded-[var(--radius-control)] bg-slate-200/70 ${className}`} />;
}

export function ErrorBanner({ message }: { message: string }) {
  return (
    <div role="alert" className="rounded-[var(--radius-control)] border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
      {message}
    </div>
  );
}

export function EmptyState({
  title,
  subtitle,
  icon,
  action,
}: {
  title: string;
  subtitle?: string;
  icon?: ReactNode;
  action?: ReactNode;
}) {
  return (
    <div className="rounded-[var(--radius-card)] border border-dashed border-slate-300 bg-white/60 px-6 py-14 text-center">
      {icon && <div className="mx-auto mb-3 text-4xl">{icon}</div>}
      <p className="font-semibold text-slate-700">{title}</p>
      {subtitle && <p className="mt-1 text-sm text-slate-500">{subtitle}</p>}
      {action && <div className="mt-5 flex justify-center">{action}</div>}
    </div>
  );
}

/** Encabezado de sección consistente (título + subtítulo + acción opcional). */
export function SectionHeading({
  title,
  subtitle,
  action,
}: {
  title: string;
  subtitle?: string;
  action?: ReactNode;
}) {
  return (
    <div className="mb-4 flex flex-wrap items-end justify-between gap-3">
      <div>
        <h2 className="text-lg font-bold text-slate-900">{title}</h2>
        {subtitle && <p className="mt-0.5 text-sm text-slate-500">{subtitle}</p>}
      </div>
      {action}
    </div>
  );
}

interface FieldProps {
  label: string;
  htmlFor?: string;
  children: ReactNode;
  hint?: string;
  error?: string;
  required?: boolean;
}

export function Field({ label, htmlFor, children, hint, error, required }: FieldProps) {
  return (
    <label htmlFor={htmlFor} className="block">
      <span className="mb-1.5 flex items-center gap-1 text-sm font-medium text-slate-700">
        {label}
        {required && <span className="text-rose-500">*</span>}
      </span>
      {children}
      {error ? (
        <FieldError message={error} />
      ) : (
        hint && <span className="mt-1 block text-xs text-slate-500">{hint}</span>
      )}
    </label>
  );
}

/** Mensaje de error por campo, accesible. No renderiza nada si no hay mensaje. */
export function FieldError({ message }: { message?: string }) {
  if (!message) {
    return null;
  }
  return (
    <span role="alert" className="mt-1 flex items-center gap-1 text-xs font-medium text-rose-600">
      <span aria-hidden>⚠</span>
      {message}
    </span>
  );
}

const inputBase =
  'w-full rounded-[var(--radius-control)] border bg-white px-3 py-2.5 text-sm shadow-sm transition-colors placeholder:text-slate-400 focus:outline-none focus-visible:ring-2';

export const inputClasses = `${inputBase} border-slate-300 focus:border-brand-500 focus-visible:ring-brand-500/30`;
const inputErrorClasses = `${inputBase} border-rose-300 focus:border-rose-500 focus-visible:ring-rose-500/30`;

interface TextInputProps extends InputHTMLAttributes<HTMLInputElement> {
  error?: string;
}

/** Input controlado con estilo de error integrado y atributos ARIA. */
export function TextInput({ error, className = '', id, ...props }: TextInputProps) {
  const generatedId = useId();
  const inputId = id ?? generatedId;
  const errorId = `${inputId}-error`;
  return (
    <>
      <input
        id={inputId}
        className={`${error ? inputErrorClasses : inputClasses} ${className}`}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? errorId : undefined}
        {...props}
      />
      {error && (
        <span id={errorId}>
          <FieldError message={error} />
        </span>
      )}
    </>
  );
}

interface PasswordFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  error?: string;
  showToggle?: boolean;
}

/** Input de contraseña con botón mostrar/ocultar accesible. */
export function PasswordField({ error, showToggle = true, className = '', id, ...props }: PasswordFieldProps) {
  const [visible, setVisible] = useState(false);
  const generatedId = useId();
  const inputId = id ?? generatedId;
  const errorId = `${inputId}-error`;
  return (
    <>
      <div className="relative">
        <input
          id={inputId}
          type={visible ? 'text' : 'password'}
          className={`${error ? inputErrorClasses : inputClasses} pr-11 ${className}`}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : undefined}
          {...props}
        />
        {showToggle && (
          <button
            type="button"
            onClick={() => setVisible((v) => !v)}
            className="absolute inset-y-0 right-0 flex items-center px-3 text-slate-400 hover:text-slate-700 focus-visible:text-slate-700"
            aria-label={visible ? 'Ocultar contraseña' : 'Mostrar contraseña'}
            aria-pressed={visible}
            tabIndex={-1}
          >
            {visible ? '🙈' : '👁️'}
          </button>
        )}
      </div>
      {error && (
        <span id={errorId}>
          <FieldError message={error} />
        </span>
      )}
    </>
  );
}

const strengthColors = ['bg-rose-500', 'bg-rose-400', 'bg-amber-400', 'bg-lime-500', 'bg-brand-600'];

/** Barra + etiqueta del nivel de fortaleza de una contraseña (score 0–4). */
export function PasswordStrengthMeter({ score }: { score: number }) {
  const clamped = Math.max(0, Math.min(4, score));
  const filled = clamped + 1; // 1..5 segmentos
  return (
    <div className="mt-2">
      <div className="flex gap-1" aria-hidden>
        {Array.from({ length: 5 }).map((_, i) => (
          <span
            key={i}
            className={`h-1.5 flex-1 rounded-full transition-colors ${
              i < filled ? strengthColors[clamped] : 'bg-slate-200'
            }`}
          />
        ))}
      </div>
      <p className="mt-1 text-xs text-slate-500">
        Seguridad: <span className="font-medium text-slate-700">{passwordStrengthLabel(clamped)}</span>
      </p>
    </div>
  );
}
