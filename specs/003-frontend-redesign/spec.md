# Feature Specification: Rediseño del frontend + validación de formularios

**Feature Branch**: `003-frontend-redesign`

**Created**: 2026-07-24

**Status**: Draft

**Input**: User description: "Rediseño completo del frontend con una identidad visual 'deportiva, premium y elegante', más el endurecimiento de las validaciones de formularios. Alcance: todas las páginas. Se mantiene el stack actual sin librerías de UI externas. El registro NO debe permitir contraseñas débiles como '123456'; validar todos los campos en vivo, con confirmación de contraseña y medidor de fortaleza. No cambia la lógica de negocio del backend ni los contratos de API."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registro seguro con validación en vivo (Priority: P1)

Una persona quiere crear una cuenta (cliente o dueño). Mientras completa el formulario, cada campo le confirma si es válido o le explica qué corregir. No puede enviar el formulario hasta que todo sea correcto, y el sistema rechaza contraseñas débiles como `123456`.

**Why this priority**: Es la falla concreta reportada (se aceptan contraseñas débiles) y la puerta de entrada de todo usuario nuevo. Sin esto, la plataforma admite credenciales inseguras y frustra a quien se registra por errores poco claros.

**Independent Test**: Se puede probar de forma aislada abriendo el registro, intentando enviar con datos inválidos (contraseña `123456`, correo mal formado, contraseñas que no coinciden) y verificando que el envío se bloquea con mensajes claros por campo; luego con datos válidos la cuenta se crea.

**Acceptance Scenarios**:

1. **Given** el formulario de registro vacío, **When** el usuario escribe `123456` como contraseña, **Then** el campo muestra un error de contraseña débil, el medidor de fortaleza indica "débil" y el botón de crear cuenta permanece deshabilitado.
2. **Given** una contraseña válida, **When** el usuario escribe una confirmación distinta, **Then** aparece un error "las contraseñas no coinciden" y no se permite enviar.
3. **Given** un correo con formato inválido (ej. `juan@`), **When** el campo pierde el foco, **Then** se muestra un mensaje de formato de correo inválido.
4. **Given** todos los campos válidos, **When** el usuario envía, **Then** la cuenta se crea y es redirigido según su rol (cliente al inicio, dueño al panel).
5. **Given** un teléfono opcional dejado vacío, **When** el usuario envía, **Then** el formulario se acepta sin marcar error en teléfono.
6. **Given** un teléfono con caracteres no válidos, **When** el usuario lo escribe, **Then** se muestra un error de formato de teléfono.

---

### User Story 2 - Experiencia visual deportiva, premium y elegante (Priority: P1)

Cualquier visitante (cliente o dueño) navega por la aplicación y percibe una identidad visual coherente, moderna y de alta calidad: jerarquía clara, tarjetas atractivas, navegación pulida y estados consistentes de carga, vacío y error en todas las páginas.

**Why this priority**: Es el objetivo central pedido ("el front está muy feo"). Impacta la percepción de confianza para reservar y pagar, y es transversal a todas las pantallas.

**Independent Test**: Recorrer cada página (marketplace, detalle de sede, mis reservas, partidos, login, registro y todo el panel de dueño) y verificar que comparten el mismo sistema visual: tipografía, colores, espaciado, sombras, botones, tarjetas y estados de carga/vacío/error consistentes.

**Acceptance Scenarios**:

1. **Given** el marketplace, **When** carga, **Then** se muestra un hero deportivo con buscador y una cuadrícula de tarjetas de sede visualmente atractivas y consistentes.
2. **Given** cualquier página que carga datos, **When** los datos están cargando, **Then** se muestra un indicador de carga con el mismo estilo en toda la app.
3. **Given** una búsqueda sin resultados, **When** no hay datos, **Then** se muestra un estado vacío con el mismo estilo en toda la app.
4. **Given** un error al cargar datos, **When** ocurre, **Then** se muestra un mensaje de error con el mismo estilo en toda la app.
5. **Given** un dispositivo móvil, **When** se abre cualquier página, **Then** el contenido se adapta sin desbordes ni solapamientos.

---

### User Story 3 - Accesibilidad y feedback consistente en todos los formularios (Priority: P2)

Un usuario que navega por teclado o con contraste reducido puede usar toda la aplicación: el foco es siempre visible, el contraste cumple AA, y todos los formularios (login, registro, formularios del panel de dueño) dan feedback de validación y error con el mismo patrón que el registro.

**Why this priority**: Amplía la calidad y consistencia al resto de la app y garantiza usabilidad; depende de que primero exista el sistema visual (US2) y el patrón de validación (US1).

**Independent Test**: Navegar la app usando solo el teclado verificando foco visible en cada control, y completar login y formularios del panel con datos inválidos comprobando que muestran errores por campo con el mismo estilo.

**Acceptance Scenarios**:

1. **Given** cualquier página, **When** el usuario navega con Tab, **Then** cada elemento interactivo muestra un indicador de foco visible.
2. **Given** el login, **When** el usuario intenta enviar con correo vacío o inválido, **Then** se muestra un error por campo con el mismo patrón visual del registro.
3. **Given** un formulario del panel de dueño con un campo requerido vacío, **When** el usuario intenta guardar, **Then** se muestra el error por campo y se bloquea el guardado.

---

### Edge Cases

- ¿Qué pasa si el backend rechaza el registro (ej. correo ya existe) aunque el formulario sea válido en el cliente? → Se muestra el mensaje de error del backend sin perder los datos ya ingresados.
- ¿Qué pasa con una contraseña larga pero solo de letras o solo de números? → Se considera que no cumple la política y se marca como inválida.
- ¿Qué pasa si el usuario pega contenido en un campo (sin evento de tecla)? → La validación se ejecuta igualmente al cambiar el valor.
- ¿Qué pasa con nombres con espacios al inicio/fin? → Se recortan y, si queda vacío, se marca como requerido.
- ¿Cómo se ven las tarjetas de sede sin precio o sin servicios? → Se degradan con elegancia sin romper el diseño.
- ¿Cómo se comporta el hero y la cuadrícula en pantallas muy angostas o muy anchas? → Mantiene márgenes máximos y refluye sin desbordes.

## Requirements *(mandatory)*

### Functional Requirements

#### Identidad visual y sistema de diseño

- **FR-001**: La aplicación DEBE presentar una identidad visual coherente ("deportiva, premium y elegante") aplicada de forma consistente en todas las páginas del alcance.
- **FR-002**: La aplicación DEBE usar un sistema de diseño unificado (tokens de color, tipografía, espaciado, radios y sombras) reutilizado por todos los componentes.
- **FR-003**: Los componentes base (botones, tarjetas, campos de formulario, insignias, indicadores de carga, estados vacíos y de error, encabezado, pie y navegación) DEBEN compartir un estilo consistente.
- **FR-004**: Cada página que carga datos DEBE mostrar estados consistentes de carga, vacío y error.
- **FR-005**: La interfaz DEBE ser responsive y usable desde móvil hasta escritorio, sin desbordes ni solapamientos.
- **FR-006**: Los elementos interactivos DEBEN incluir micro-interacciones sobrias (hover, foco, transición) coherentes en toda la app.

#### Accesibilidad

- **FR-007**: Todos los controles interactivos DEBEN tener un indicador de foco visible.
- **FR-008**: El contraste de texto y elementos DEBE cumplir el nivel AA.
- **FR-009**: La aplicación DEBE ser navegable completamente por teclado.
- **FR-010**: Los campos de formulario DEBEN asociar su etiqueta y sus mensajes de error de forma accesible.

#### Validación de formularios

- **FR-011**: El registro DEBE rechazar contraseñas que no cumplan la política mínima (mínimo 8 caracteres, con al menos una letra y al menos un número); contraseñas como `123456` DEBEN ser rechazadas.
- **FR-012**: El registro DEBE mostrar un medidor de fortaleza de contraseña que refleje la calidad de la contraseña ingresada.
- **FR-013**: El registro DEBE incluir un campo de confirmación de contraseña y rechazar el envío si no coincide con la contraseña.
- **FR-014**: Todos los campos del registro DEBEN validarse en vivo (al cambiar/perder el foco), mostrando un mensaje de error específico por campo cuando corresponda.
- **FR-015**: El nombre DEBE ser requerido y no vacío (ignorando espacios en blanco).
- **FR-016**: El correo DEBE validarse con un formato válido.
- **FR-017**: El teléfono DEBE ser opcional; cuando se ingresa, DEBE validarse con un formato aceptable.
- **FR-018**: El botón de envío DEBE permanecer deshabilitado hasta que el formulario sea válido.
- **FR-019**: El login DEBE validar que el correo tenga formato válido y que la contraseña no esté vacía, mostrando errores por campo con el mismo patrón del registro.
- **FR-020**: Los formularios del panel de dueño DEBEN aplicar validación por campo y bloquear el guardado cuando haya campos inválidos, con el mismo patrón de feedback.
- **FR-021**: Cuando el backend devuelva un error de validación o de negocio, la interfaz DEBE mostrarlo sin descartar los datos ya ingresados por el usuario.

#### Alcance y no-regresión

- **FR-022**: El rediseño DEBE cubrir todas las páginas: marketplace, detalle de sede, mis reservas, partidos abiertos, login, registro, y el panel de dueño (dashboard, sedes, detalle de sede, agenda).
- **FR-023**: El rediseño NO DEBE alterar los contratos de API existentes (rutas, formas de request/response en snake_case). Se permite endurecer las reglas de validación de entrada del backend sin cambiar dichos contratos.
- **FR-025**: El backend DEBE aplicar la misma política de contraseña en el registro (mínimo 8 caracteres, con al menos una letra y un número); una llamada directa a la API con `123456` DEBE ser rechazada con un error de validación.
- **FR-026**: El backend DEBE validar el formato del correo en el registro y rechazar correos con formato inválido.
- **FR-024**: Todos los flujos funcionales actuales (buscar sedes, ver detalle, reservar, pagar, gestionar como dueño) DEBEN seguir funcionando tras el rediseño.

### Key Entities *(include if feature involves data)*

No introduce nuevas entidades de datos. Reutiliza las existentes (usuario, sede, cancha, reserva, partido, pago) sin cambiar sus contratos.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de los intentos de registrar una contraseña que no cumple la política (incluido `123456`) son rechazados, tanto en el cliente (antes de enviar) como en el backend (aunque se llame la API directamente).
- **SC-002**: El 100% de las páginas del alcance usan el mismo sistema visual (mismos componentes de botón, tarjeta, campo, carga, vacío y error).
- **SC-003**: Toda la aplicación es operable únicamente con teclado, con foco visible en el 100% de los controles interactivos.
- **SC-004**: El contraste de texto principal y controles cumple el nivel AA en todas las páginas.
- **SC-005**: La interfaz se muestra sin desbordes horizontales ni solapamientos en anchos desde 360px hasta 1440px.
- **SC-006**: Un usuario nuevo completa un registro válido en menos de 2 minutos, con cero mensajes de error ambiguos. (Verificable con el escenario cronometrado de `quickstart.md`.)
- **SC-007**: El 100% de los formularios de la app muestran errores de validación por campo con un patrón consistente.
- **SC-008**: Cero regresiones funcionales: todos los flujos existentes siguen completándose tras el rediseño.

## Assumptions

- Se mantiene el stack actual (React + TypeScript + Vite + Tailwind) sin añadir librerías de UI externas; el sistema de diseño se amplía sobre los componentes propios existentes.
- La política de contraseña acordada es: mínimo 8 caracteres con al menos una letra y al menos un número. (Endurece el mínimo actual de 6.)
- La paleta base parte del verde de marca existente (identidad deportiva) y se refina para lograr el acabado premium.
- Las cuentas y contraseñas de demostración existentes seguirán funcionando para efectos de prueba, ya que la política nueva aplica a nuevos registros en el cliente y no cambia credenciales ya almacenadas.
- El idioma de la interfaz es español, como en la versión actual.
- La validación en el cliente complementa —no reemplaza— la validación del backend; el backend es la autoridad final y aplica la misma política de contraseña (FR-025).
- El soporte de modo oscuro no forma parte de este alcance salvo que se solicite explícitamente.
