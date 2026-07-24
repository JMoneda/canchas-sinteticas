# Research: Rediseño del frontend + validación de formularios

Fase 0 — decisiones técnicas y de diseño. No quedan `NEEDS CLARIFICATION`.

## D1 — Enfoque del sistema de diseño

- **Decisión**: Ampliar el sistema propio en `components/ui.tsx` + tokens de Tailwind v4 vía
  `@theme` en `index.css`. Sin librerías de UI externas.
- **Rationale**: El usuario delegó la decisión pidiendo "mejorar"; la constitución exige
  Simplicidad (sin dependencias injustificadas) y ya existe un `ui.tsx` consumido por todas las
  páginas. Restilar el núcleo propaga el cambio a toda la app con mínimo riesgo.
- **Alternativas**: shadcn/ui + Radix (rechazada: agrega dependencias y peso, y la constitución
  penaliza sobre-ingeniería); CSS-in-JS (rechazada: el proyecto ya usa Tailwind).

## D2 — Identidad visual "deportiva, premium y elegante"

- **Decisión**:
  - **Color**: base verde césped de marca (emerald) refinada + un neutro cálido (slate/stone)
    para superficies, y un acento de contraste (lima/teal) para CTAs y datos destacados.
  - **Tipografía**: display fuerte para titulares (peso 700–800, tracking ajustado) y sans
    legible para cuerpo. Se usan fuentes del sistema para no añadir dependencias/red (rendimiento);
    escala tipográfica modular.
  - **Superficies**: cards con radio grande (rounded-2xl/3xl), sombras suaves multicapa,
    bordes sutiles (1px slate/alpha). "Premium" = mucho aire, jerarquía y sombras discretas.
  - **Motion**: transiciones cortas (150–200ms) en hover/focus/press; `hover:-translate-y-0.5`
    en cards; respetar `prefers-reduced-motion`.
- **Rationale**: Fusiona lo "deportivo" (verde energético, hero con imagen/gradiente) con lo
  "premium/elegante" (aire, sombras suaves, tipografía fuerte, neutros cálidos), evitando
  saturación. Fuentes de sistema = cero costo de red y buen rendimiento.
- **Alternativas**: paleta oscura/neón (rechazada: se aleja de "elegante"); fuentes web de Google
  (rechazada por ahora: añade red/latencia; puede reconsiderarse fuera de alcance).

## D3 — Modo oscuro

- **Decisión**: Fuera de alcance en esta feature (se ofreció como opción y el usuario eligió el
  enfoque "premium y elegante" sin pedir modo oscuro).
- **Rationale**: Acota el alcance (Principio V). Los tokens se definirán de forma que un modo
  oscuro futuro sea viable sin reescritura.

## D4 — Política de contraseña

- **Decisión**: mínimo **8 caracteres**, con **al menos una letra y al menos un número**.
  Rechaza `123456` (solo dígitos) y `abcdefgh` (solo letras).
- **Rationale**: Corrige el defecto reportado (aceptaba `123456`) con una regla clara, común y
  fácil de comunicar; endurece el `minLength=6` actual.
- **Alternativas**: exigir símbolos y mayúsculas (rechazada por ahora: fricción alta para un MVP;
  documentado como ajuste posible en `/speckit-clarify`). El backend permanece como autoridad final.

## D5 — Medidor de fortaleza

- **Decisión**: Puntaje heurístico (0–4) basado en longitud y variedad de clases de caracteres
  (minúsculas, mayúsculas, dígitos, símbolos), mostrado como barra + etiqueta
  (Muy débil/Débil/Aceptable/Fuerte/Excelente). Cálculo local, sin dependencias.
- **Rationale**: Comunica calidad sin librerías (zxcvbn agregaría ~400KB → viola Simplicidad).
- **Alternativas**: zxcvbn (rechazada por peso).

## D6 — Arquitectura de validación

- **Decisión**: Validadores **puros** en `lib/validation.ts` (`validateEmail`, `validatePhone`,
  `validatePasswordPolicy`, `passwordStrength`, `validateRequired`, `validateMatch`) + hook
  genérico `useFormValidation` (maneja valores, `touched`, errores derivados, `isValid`).
  Validación **en vivo** al cambiar/blur; el botón de envío se deshabilita mientras `!isValid`.
- **Rationale**: SRP y reutilización entre registro, login y panel. Funciones puras = testeables
  aisladamente. Sin dependencias de formularios (react-hook-form/zod) → Simplicidad.
- **Alternativas**: react-hook-form + zod (rechazada: dependencias nuevas para un set pequeño de
  formularios); validación ad-hoc por página (rechazada: duplicación e inconsistencia).

## D7 — Teléfono (opcional) y formato

- **Decisión**: Opcional; cuando se ingresa, aceptar formato flexible de Colombia/internacional:
  dígitos con espacios/guiones/paréntesis y `+` inicial opcional, longitud 7–15 dígitos.
- **Rationale**: Evita rechazar teléfonos válidos por formato estricto; coherente con público
  colombiano.
- **Alternativas**: validación E.164 estricta (rechazada: fricción innecesaria en MVP).

## D8 — Accesibilidad

- **Decisión**: Anillos de foco visibles (`focus-visible`), asociación `label`/`aria-describedby`
  para errores (`role="alert"` en mensajes), contraste AA verificado en tokens, navegación por
  teclado en menús/diálogos, respeto de `prefers-reduced-motion`.
- **Rationale**: Requisitos FR-007..FR-010 y SC-003/SC-004.
- **Alternativas**: librería de a11y (innecesaria para el alcance).

## D9 — Verificación sin framework de tests

- **Decisión**: `tsc -b` + `vite build` como puertas automáticas; verificación funcional y visual
  guiada por `quickstart.md`. Validadores aislados como funciones puras para test futuro.
- **Rationale**: No hay runner de tests de frontend instalado; añadir uno excede el alcance.
- **Alternativas**: instalar Vitest ahora (aplazado; documentado como mejora futura).
