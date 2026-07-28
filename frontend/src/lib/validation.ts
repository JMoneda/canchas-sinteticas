/**
 * Validadores puros reutilizables para formularios.
 * Cada función devuelve `null` si el valor es válido, o un mensaje en español si no.
 * Sin dependencias de React → testeables de forma aislada.
 */

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Campo de texto obligatorio (se ignoran espacios en blanco). */
export function validateRequired(value: string, label = 'Este campo'): string | null {
  return value.trim().length === 0 ? `${label} es obligatorio.` : null;
}

/** Correo con formato válido y no vacío. */
export function validateEmail(value: string): string | null {
  if (value.trim().length === 0) {
    return 'El correo es obligatorio.';
  }
  return EMAIL_RE.test(value.trim()) ? null : 'Correo no válido.';
}

/**
 * Teléfono opcional (Colombia). Vacío es válido; si se ingresa, admite dígitos
 * con `+`, espacios, guiones y paréntesis, y un indicativo `+57` opcional.
 * Debe quedar en 10 dígitos (celular `3XX…` o fijo `60X…`) y no puede ser una
 * secuencia de un mismo dígito repetido (número basura, ej. 32222222222).
 */
export function validatePhone(value: string): string | null {
  const trimmed = value.trim();
  if (trimmed.length === 0) {
    return null;
  }
  if (!/^[+\d\s()-]+$/.test(trimmed)) {
    return 'Teléfono no válido.';
  }
  let digits = trimmed.replace(/\D/g, '');
  // Quitar indicativo de país Colombia (+57) si viene antepuesto a 10 dígitos.
  if (digits.length === 12 && digits.startsWith('57')) {
    digits = digits.slice(2);
  }
  if (digits.length !== 10) {
    return 'El teléfono debe tener 10 dígitos (ej. 300 123 4567).';
  }
  if (!/^[36]/.test(digits)) {
    return 'Debe ser un celular (3XX) o un fijo (60X).';
  }
  if (/(\d)\1{6,}/.test(digits)) {
    return 'Ingresa un número de teléfono real.';
  }
  return null;
}

/**
 * Política de contraseña: mínimo 8 caracteres, con al menos una letra y un
 * número. Rechaza contraseñas débiles como `123456`.
 */
export function validatePasswordPolicy(value: string): string | null {
  if (value.length < 8) {
    return 'Mínimo 8 caracteres.';
  }
  const hasLetter = /[a-zA-Z]/.test(value);
  const hasNumber = /\d/.test(value);
  if (!hasLetter || !hasNumber) {
    return 'Debe incluir letras y números.';
  }
  return null;
}

/** Dos valores deben coincidir (p. ej. confirmación de contraseña). */
export function validateMatch(a: string, b: string, msg = 'Las contraseñas no coinciden.'): string | null {
  return a === b ? null : msg;
}

/**
 * Fortaleza de la contraseña en una escala 0–4, según longitud y variedad de
 * clases de carácter (minúscula, mayúscula, dígito, símbolo). Heurística local.
 */
export function passwordStrength(value: string): number {
  if (!value) {
    return 0;
  }
  let score = 0;
  if (value.length >= 8) score++;
  if (value.length >= 12) score++;

  let classes = 0;
  if (/[a-z]/.test(value)) classes++;
  if (/[A-Z]/.test(value)) classes++;
  if (/\d/.test(value)) classes++;
  if (/[^a-zA-Z0-9]/.test(value)) classes++;
  if (classes >= 2) score++;
  if (classes >= 3) score++;

  return Math.min(4, score);
}

const STRENGTH_LABELS = ['Muy débil', 'Débil', 'Aceptable', 'Fuerte', 'Excelente'];

/** Etiqueta legible para un puntaje de fortaleza 0–4. */
export function passwordStrengthLabel(score: number): string {
  return STRENGTH_LABELS[Math.max(0, Math.min(4, score))];
}
