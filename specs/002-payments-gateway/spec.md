# Feature Specification: Pagos reales, pago dividido y comprobantes

**Feature Branch**: `002-payments-gateway`

**Created**: 2026-07-24

**Status**: Draft

**Input**: User description: "Mejora completa del sistema de pagos: integrar pasarela real de Colombia (Nequi, Bancolombia, PSE, tarjetas), pago dividido entre jugadores, comprobantes, reembolsos integrados."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Pagar una reserva con un método real de Colombia (Priority: P1)

Un cliente selecciona una cancha, fecha y franja horaria, y al confirmar la reserva paga usando un método de pago real y común en Colombia (Nequi, PSE / débito bancario, botón Bancolombia, o tarjeta de crédito/débito). La reserva solo queda confirmada cuando el proveedor de pagos confirma que la transacción fue aprobada; mientras tanto queda en estado pendiente.

**Why this priority**: Es el núcleo del negocio. Sin cobro real y confiable, la plataforma no genera ingresos ni garantiza el cupo. Reemplaza directamente el pago simulado actual y entrega valor por sí solo.

**Independent Test**: Se puede probar completamente creando una reserva, iniciando el pago con un método real (en ambiente de pruebas del proveedor), y verificando que la reserva pasa a confirmada únicamente tras la confirmación aprobada, y a fallida/liberada si es rechazada.

**Acceptance Scenarios**:

1. **Given** una franja disponible y un cliente autenticado, **When** el cliente confirma la reserva y elige un método de pago, **Then** el sistema crea una transacción pendiente y presenta el medio de pago del proveedor.
2. **Given** una transacción pendiente, **When** el proveedor confirma que el pago fue aprobado, **Then** la reserva pasa a confirmada y el pago queda registrado como pagado con la referencia real del proveedor.
3. **Given** una transacción pendiente, **When** el proveedor informa rechazo o expiración, **Then** la reserva no se confirma, la franja se libera y el cliente ve el motivo.
4. **Given** una transacción aprobada, **When** el cliente consulta su reserva, **Then** ve el método usado, el monto, la referencia y la fecha del pago.

---

### User Story 2 - Dividir el pago de un partido entre jugadores (Priority: P2)

El organizador de un partido abierto habilita el pago dividido: cada jugador inscrito paga su parte proporcional a través de un método real. El sistema rastrea cuánto se ha recaudado, quién ya pagó y cuánto falta. Si no se completa el recaudo antes de la fecha límite, se aplica la política de reembolso/cancelación.

**Why this priority**: Es un diferenciador clave para partidos entre desconocidos (pickup games), muy común en canchas sintéticas. Depende de que el pago individual (P1) ya funcione.

**Independent Test**: Se puede probar creando un partido con pago dividido, haciendo que varios jugadores paguen su parte, y verificando el estado de recaudo y el comportamiento cuando el recaudo se completa o cuando expira sin completarse.

**Acceptance Scenarios**:

1. **Given** un partido con pago dividido y precio por jugador definido, **When** un jugador se une y paga su parte, **Then** su parte queda marcada como pagada y el monto recaudado aumenta.
2. **Given** un partido con recaudo parcial, **When** un jugador consulta el partido, **Then** ve cuántos han pagado, cuánto se ha recaudado y cuánto falta.
3. **Given** un partido donde todos los cupos pagaron su parte, **When** se completa el último pago, **Then** el partido y su reserva quedan confirmados.
4. **Given** un partido que llega a la fecha límite sin completar el recaudo, **When** vence el plazo, **Then** el sistema aplica la política de reembolso a quienes ya pagaron y cancela/libera la reserva.
5. **Given** un jugador que ya pagó su parte, **When** abandona el partido antes del cierre, **Then** el sistema aplica la política de reembolso/no-reembolso definida y actualiza el recaudo.

---

### User Story 3 - Ver y descargar el comprobante de pago (Priority: P2)

Después de un pago aprobado, el cliente puede ver y descargar un comprobante con los datos de la transacción: referencia del proveedor, monto, método, fecha, sede y cancha, y —en pago dividido— la parte del jugador. El comprobante sirve como soporte del pago.

**Why this priority**: Genera confianza y sirve de soporte para el cliente y el dueño. Depende de que exista un pago aprobado (P1/P2).

**Independent Test**: Se puede probar realizando un pago aprobado y verificando que se genera un comprobante consultable y descargable con todos los datos requeridos, y que solo el titular (y el dueño de la sede) pueden verlo.

**Acceptance Scenarios**:

1. **Given** un pago aprobado de una reserva, **When** el cliente abre su reserva, **Then** puede ver y descargar el comprobante con referencia, monto, método, fecha, sede y cancha.
2. **Given** un pago dividido, **When** un jugador consulta su participación, **Then** puede descargar el comprobante de su parte con su nombre y monto pagado.
3. **Given** un comprobante de otro cliente, **When** un usuario no autorizado intenta verlo, **Then** el sistema lo impide.

---

### User Story 4 - Reembolso al cancelar según política (Priority: P3)

Cuando un cliente cancela una reserva pagada dentro de la ventana permitida (no tardía), el sistema ejecuta un reembolso real a través del proveedor y refleja el estado reembolsado. Si la cancelación es tardía, no hay reembolso, según la política de la sede.

**Why this priority**: Cierra el ciclo de dinero y evita disputas, pero solo aplica tras existir cobros reales. Ya existe la lógica de decisión de reembolso; falta la ejecución real.

**Independent Test**: Se puede probar cancelando una reserva pagada dentro de la ventana permitida y verificando que se solicita el reembolso al proveedor y el pago queda como reembolsado; y cancelando tarde para verificar que no hay reembolso.

**Acceptance Scenarios**:

1. **Given** una reserva pagada y una cancelación dentro de la ventana permitida, **When** el cliente cancela, **Then** el sistema solicita el reembolso al proveedor y el pago pasa a reembolsado.
2. **Given** una reserva pagada y una cancelación tardía, **When** el cliente cancela, **Then** no se emite reembolso y se registra según la política de la sede.
3. **Given** un reembolso solicitado, **When** el proveedor confirma el reembolso, **Then** el estado del pago refleja reembolso confirmado y el cliente es notificado.

---

### Edge Cases

- **Confirmación duplicada del proveedor**: el sistema debe procesar de forma idempotente varias notificaciones para la misma transacción sin cobrar/confirmar dos veces.
- **Notificación no auténtica**: una notificación de confirmación cuya autenticidad no se pueda verificar debe rechazarse y no cambiar ningún estado.
- **Confirmación tardía tras expirar**: si llega una aprobación después de que la franja se liberó, el sistema debe resolver el conflicto (reembolsar automáticamente o reconfirmar si la franja sigue libre) sin doble reserva.
- **Cliente cierra el checkout sin completar**: la transacción queda pendiente y expira; la franja se libera al vencer el plazo.
- **Monto por jugador no divisible exacto**: la suma de las partes debe igualar el total (se ajusta el redondeo en una de las partes).
- **Pago duplicado del mismo jugador** en un partido dividido: debe evitarse cobrar dos veces la misma parte.
- **Reembolso de un pago dividido parcialmente recaudado**: cada parte pagada se reembolsa individualmente.
- **Reembolso que el proveedor rechaza o deja pendiente**: el sistema debe reflejar el estado real y permitir reintento/seguimiento.
- **Caída del proveedor** al crear la transacción: el cliente ve un error claro y la franja no queda bloqueada indefinidamente.

## Requirements *(mandatory)*

### Functional Requirements

#### Pago real (pasarela)

- **FR-001**: El sistema MUST ofrecer al cliente, al pagar una reserva, los métodos de pago comunes en Colombia: Nequi, PSE (débito desde cuenta bancaria), botón/transferencia Bancolombia y tarjetas de crédito/débito.
- **FR-002**: El sistema MUST crear una transacción de pago en estado pendiente antes de dirigir al cliente al medio de pago, registrando monto, método y la reserva o parte asociada.
- **FR-003**: El sistema MUST confirmar el pago como pagado ÚNICAMENTE cuando el proveedor de pagos confirma que la transacción fue aprobada; nunca antes.
- **FR-004**: El sistema MUST recibir y procesar las confirmaciones de estado del proveedor de forma asíncrona (aprobado, rechazado, en proceso, expirado, reembolsado) y actualizar el estado del pago y de la reserva/partido correspondiente.
- **FR-005**: El sistema MUST verificar la autenticidad de cada notificación del proveedor antes de aplicar cualquier cambio de estado, y rechazar las que no se puedan verificar.
- **FR-006**: El sistema MUST procesar las confirmaciones de forma idempotente: notificaciones repetidas para la misma transacción no deben producir cobros, confirmaciones ni cambios duplicados.
- **FR-007**: El sistema MUST registrar y exponer la referencia real de la transacción del proveedor asociada a cada pago.
- **FR-008**: El sistema MUST liberar la franja reservada cuando una transacción es rechazada o expira sin aprobación, para que otros clientes puedan reservarla.
- **FR-009**: El sistema MUST manejar los errores de comunicación con el proveedor mostrando un mensaje claro al cliente y sin dejar la franja bloqueada de forma indefinida.
- **FR-010**: El sistema MUST mantener la abstracción del proveedor de pagos de modo que se pueda cambiar o añadir otro proveedor sin reescribir la lógica de negocio de reservas, partidos ni comprobantes.
- **FR-011**: El sistema MUST leer las credenciales y secretos del proveedor desde configuración segura, nunca escritos directamente en el código.

#### Pago dividido entre jugadores

- **FR-012**: El sistema MUST permitir que, en un partido con pago dividido habilitado, cada jugador inscrito pague su parte proporcional mediante un método de pago real.
- **FR-013**: El sistema MUST calcular la parte por jugador de forma que la suma de las partes iguale exactamente el precio total (ajustando el redondeo).
- **FR-014**: El sistema MUST rastrear y exponer, para cada partido, cuánto se ha recaudado, qué jugadores han pagado y cuánto falta por recaudar.
- **FR-015**: El sistema MUST confirmar el partido y su reserva cuando todas las partes requeridas han sido pagadas.
- **FR-016**: El sistema MUST evitar que un mismo jugador pague dos veces su parte en el mismo partido.
- **FR-017**: El sistema MUST aplicar una política definida de expiración cuando un partido no completa el recaudo antes de su fecha límite, incluyendo el reembolso de las partes ya pagadas y la liberación de la reserva.
- **FR-018**: El sistema MUST actualizar el recaudo y aplicar la política de reembolso correspondiente cuando un jugador que ya pagó abandona el partido antes del cierre.

#### Comprobantes

- **FR-019**: El sistema MUST generar un comprobante por cada pago aprobado, tanto para reservas individuales como para cada parte de un pago dividido.
- **FR-020**: El comprobante MUST incluir: referencia del proveedor, monto, método de pago, fecha y hora, sede y cancha, y —en pago dividido— el nombre del jugador y su parte.
- **FR-021**: El cliente MUST poder ver y descargar el comprobante de sus propios pagos.
- **FR-022**: El sistema MUST restringir el acceso a cada comprobante al titular del pago y al dueño de la sede correspondiente; ningún otro usuario debe poder verlo.

#### Reembolsos y cancelaciones

- **FR-023**: El sistema MUST ejecutar un reembolso real a través del proveedor cuando una reserva pagada se cancela dentro de la ventana permitida (cancelación no tardía).
- **FR-024**: El sistema MUST NO emitir reembolso cuando la cancelación es tardía, según la política de la sede, y registrar el hecho.
- **FR-025**: El sistema MUST reflejar el estado real del reembolso (solicitado, confirmado, rechazado/pendiente) según lo informe el proveedor y permitir su seguimiento.
- **FR-026**: El sistema MUST notificar al cliente el resultado de su pago (aprobado, rechazado) y de su reembolso (confirmado) por tres canales: dentro de la aplicación, por correo electrónico y por mensajería (WhatsApp/SMS).
- **FR-026a**: La notificación por correo y por mensajería MUST incluir un enlace o adjunto al comprobante cuando el pago sea aprobado.

#### Modelo de recaudo (flujo del dinero)

- **FR-027**: El sistema MUST soportar dos modelos de recaudo, seleccionables por cada dueño/sede: (a) **cuenta directa** — el dueño conecta su propia cuenta del proveedor y recauda directamente; y (b) **marketplace** — la plataforma recauda en una cuenta central y liquida al dueño. El modelo activo se define en la configuración de la sede.
- **FR-028**: El sistema MUST atribuir cada pago a la sede/dueño correspondiente para efectos de reportes de ingresos, independientemente del modelo de recaudo usado.
- **FR-029**: En el modelo marketplace, el sistema MUST liquidar el 100% del monto al dueño (sin comisión de plataforma en el MVP); el cobro de comisión queda documentado como extensión futura fuera de alcance.

### Key Entities *(include if feature involves data)*

- **Pago (Payment)**: Representa el intento y resultado de cobro de una reserva o de una parte de pago dividido. Atributos clave: monto, método, estado (pendiente, pagado, rechazado, expirado, reembolsado), referencia del proveedor, fecha. Se relaciona con una reserva y, en pago dividido, con un jugador del partido.
- **Transacción del proveedor**: El intento de cobro gestionado por el proveedor externo, identificado por su referencia; fuente de verdad del estado del cobro que el sistema refleja.
- **Comprobante**: Documento generado a partir de un pago aprobado, con los datos de la transacción, la reserva/partido y el jugador cuando aplica.
- **Partido (Match) / Jugador del partido (MatchPlayer)**: Un partido con pago dividido tiene un precio por jugador y varios jugadores, cada uno con el estado de su parte (pendiente/pagada) y su comprobante.
- **Reserva (Reservation)**: La reserva de cancha cuyo estado (pendiente/confirmada/cancelada) depende del resultado del pago.
- **Política de cancelación/expiración**: Reglas por sede que determinan si una cancelación es tardía y si genera reembolso, y el plazo límite para completar un pago dividido.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de las reservas confirmadas tienen un pago aprobado con una referencia real verificable del proveedor (0% confirmadas sin cobro real).
- **SC-002**: Ninguna reserva queda confirmada mientras su pago está pendiente, rechazado o expirado (0 confirmaciones prematuras).
- **SC-003**: Una confirmación de pago repetida no produce nunca un cobro ni una confirmación duplicada (0 duplicados ante notificaciones repetidas).
- **SC-004**: El cliente puede completar el pago de una reserva en menos de 3 minutos desde que confirma la franja.
- **SC-005**: Toda franja de una transacción rechazada o expirada vuelve a estar disponible dentro del plazo de expiración configurado (por defecto 15 minutos) sin intervención manual.
- **SC-006**: En un partido con pago dividido, el estado de recaudo (pagado por jugador, total recaudado, faltante) es correcto en el 100% de los casos verificados.
- **SC-007**: El 100% de los pagos aprobados generan un comprobante descargable con todos los datos requeridos.
- **SC-008**: Un usuario no autorizado nunca puede acceder al comprobante de otro (0 accesos indebidos).
- **SC-009**: El 100% de las cancelaciones dentro de la ventana permitida sobre reservas pagadas resultan en un reembolso reflejado con su estado real.

## Assumptions

- Se usará un proveedor de pagos que cubra Nequi, PSE, Bancolombia y tarjetas en Colombia (Wompi es la opción de referencia por cobertura; la abstracción permite alternativas como PayU, ePayco o Mercado Pago). La elección concreta se define en la fase de plan.
- El pago es asíncrono y su verdad la fija el proveedor; la aplicación refleja ese estado, no lo decide.
- "Pago por llaves" (Bre-B) se cubre en la medida en que el proveedor lo soporte; si no está disponible al momento del plan, queda fuera del MVP y se documenta.
- Los montos se manejan en pesos colombianos (COP) y con precisión monetaria exacta (sin errores de redondeo en decimales).
- La lógica de decisión de reembolso por ventana de cancelación ya existe en el dominio; esta feature añade la ejecución real del reembolso.
- La persistencia actual es en memoria; los estados de pago deben sobrevivir al menos durante la sesión de ejecución. La migración a base de datos real (para persistir pagos y transacciones de forma duradera) es una dependencia recomendada pero se trata como decisión de la fase de plan.
- Multi-tenant: cada pago pertenece al ámbito de la sede/dueño y respeta las reglas de autorización existentes (Owner solo ve lo suyo).
- El ambiente de pruebas (sandbox) del proveedor se usa para validar los flujos antes de producción.
- **Modelo de recaudo**: se soportan ambos modelos de forma configurable por sede — cuenta directa del dueño y marketplace de la plataforma. En marketplace se liquida el 100% al dueño; la comisión de plataforma queda explícitamente fuera del MVP.
- **Notificaciones**: el resultado de pagos y reembolsos se comunica por tres canales (app, correo y WhatsApp/SMS); la integración concreta de correo y mensajería se define en la fase de plan.

## Dependencies

- Cuenta y credenciales (sandbox y producción) del proveedor de pagos elegido.
- Un endpoint accesible públicamente para recibir las confirmaciones asíncronas del proveedor (notificaciones/webhook).
- Configuración segura para credenciales y secretos de verificación de notificaciones.
- Entidades y flujos existentes de Reserva, Pago, Partido/Jugador y política de cancelación.
