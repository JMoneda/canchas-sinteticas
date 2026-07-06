# Especificación de Funcionalidad: Sistema de Reservas para Canchas de Fútbol Sintético

**Rama de funcionalidad**: `001-reservation-system`

**Creado**: 2026-06-25

**Estado**: Borrador

**Entrada**: Descripción del usuario: "Construir un sistema de reservas para canchas de fútbol sintético. El sistema permite a los usuarios ver las canchas disponibles y sus franjas horarias, crear una reserva para una cancha específica en una fecha y rango de tiempo determinados, ver sus propias reservas existentes, cancelar una reserva y recibir mensajes de error claros cuando una solicitud de reserva viola una regla de negocio."

## Aclaraciones

### Sesión 2026-06-25

- P: ¿Cuál es el estado de una reserva después de que su franja horaria ha pasado? ¿Cuenta para el límite de 2 reservas activas? → R: Las reservas pasan automáticamente a `completed` una vez que su hora de fin ha pasado; las reservas completadas NO cuentan para el límite de 2 reservas.
- P: ¿Cómo funciona el identificador de usuario en la UI — se ingresa una vez por sesión o por acción? → R: Se ingresa una vez cuando el usuario abre la aplicación; la interfaz lo retiene para todas las acciones posteriores durante la sesión.
- P: ¿La vista "mis reservas" debe mostrar solo las reservas activas próximas, o también el historial de completadas/canceladas? → R: Solo las reservas activas próximas — el historial está fuera del alcance del MVP.

## Escenarios de Usuario y Pruebas *(obligatorio)*

### Historia de Usuario 1 - Ver Disponibilidad de Canchas (Prioridad: P1)

Un usuario quiere saber qué canchas de fútbol están disponibles y cuándo pueden reservarse
en una fecha determinada, para elegir una franja que se adapte a su horario antes de
confirmar una reserva.

**Por qué esta prioridad**: Sin conocer la disponibilidad, no se puede tomar ninguna
decisión de reserva. Este es el punto de entrada de todo el recorrido del usuario y un
prerequisito para cada otra historia. Una vista de disponibilidad entregada de forma
independiente ya genera valor como referencia de programación de solo lectura.

**Prueba Independiente**: Se puede probar consultando la disponibilidad para una fecha
con canchas y reservas pre-cargadas, y verificando que las franjas ocupadas y disponibles
se distinguen correctamente. Entrega valor independiente como referencia de programación.

**Escenarios de Aceptación**:

1. **Dado** que se selecciona una fecha, **Cuando** el usuario solicita las franjas horarias disponibles para todas las canchas, **Entonces** cada cancha muestra sus bloques de 30 minutos abiertos dentro del horario operativo (6:00 AM – 11:00 PM), excluyendo los rangos ya reservados.
2. **Dado** que una cancha está completamente reservada para una fecha, **Cuando** el usuario ve la disponibilidad para esa fecha, **Entonces** la cancha no muestra franjas disponibles para ese día.
3. **Dado** que se solicita una fecha en el pasado, **Cuando** el usuario ve la disponibilidad, **Entonces** no se muestran franjas reservables (las franjas pasadas no pueden reservarse).

---

### Historia de Usuario 2 - Crear una Reserva (Prioridad: P2)

Un usuario se identifica con un identificador de usuario, selecciona una cancha, una fecha,
una hora de inicio y una hora de fin, y envía una solicitud de reserva. El sistema confirma
la reserva o devuelve un error específico explicando qué regla fue violada.

**Por qué esta prioridad**: Crear una reserva es el valor principal del sistema. Todas
las reglas de dominio se ejercitan en esta historia. Un flujo de reserva funcional por sí
solo constituye un MVP funcional.

**Prueba Independiente**: Se puede probar de extremo a extremo enviando una solicitud de
reserva válida y verificando que se persiste y aparece en las consultas de disponibilidad
posteriores. Los caminos de error se pueden probar enviando solicitudes que violan cada
regla de forma independiente.

**Escenarios de Aceptación**:

1. **Dado** que un usuario proporciona un identificador válido, cancha, fecha, hora de inicio y hora de fin que satisfacen todas las reglas de dominio, **Cuando** se envía la reserva, **Entonces** la reserva se confirma y se le asigna un identificador único.
2. **Dado** que el rango de tiempo solicitado se superpone con una reserva existente para la misma cancha, **Cuando** se envía la reserva, **Entonces** el sistema la rechaza con un error indicando que la franja no está disponible.
3. **Dado** que la duración solicitada es menor a 1 hora o no está alineada a bloques de 30 minutos, **Cuando** se envía la reserva, **Entonces** el sistema la rechaza con un error que describe la restricción de duración.
4. **Dado** que la hora de inicio o fin cae fuera de 6:00 AM – 11:00 PM, **Cuando** se envía la reserva, **Entonces** el sistema la rechaza con un error indicando la restricción de horario operativo.
5. **Dado** que la hora de inicio de la reserva es menos de 1 hora desde el momento actual, **Cuando** se envía la reserva, **Entonces** el sistema la rechaza con un error de aviso previo.
6. **Dado** que el usuario solicitante ya tiene 2 reservas activas, **Cuando** se envía una nueva reserva, **Entonces** el sistema la rechaza con un error indicando que se ha alcanzado el límite de reservas activas.

---

### Historia de Usuario 3 - Ver Mis Reservas (Prioridad: P3)

Un usuario ve una lista de sus próximas reservas activas (futuras, no canceladas),
mostrando el nombre de la cancha, fecha y rango horario de cada una. Las reservas
completadas y canceladas no se muestran — el historial de reservas está fuera del
alcance del MVP.

**Por qué esta prioridad**: Los usuarios necesitan visibilidad sobre sus reservas
existentes para evitar solicitudes duplicadas y decidir cuáles conservar o cancelar.
Depende de la Historia 2 para datos significativos, pero es verificable de forma
independiente con datos pre-cargados.

**Prueba Independiente**: Se puede probar pre-cargando reservas para un identificador
de usuario específico y verificando que se devuelve la lista correcta al consultarla.

**Escenarios de Aceptación**:

1. **Dado** que un usuario tiene dos reservas activas, **Cuando** ve sus reservas, **Entonces** ambas aparecen con nombre de cancha, fecha, hora de inicio, hora de fin y estado.
2. **Dado** que un usuario no tiene reservas activas, **Cuando** ve sus reservas, **Entonces** se muestra un estado vacío con un mensaje claro.
3. **Dado** que un usuario ha cancelado previamente una reserva, **Cuando** ve sus reservas, **Entonces** las reservas canceladas y completadas no aparecen en la lista activa.

---

### Historia de Usuario 4 - Cancelar una Reserva (Prioridad: P4)

Un usuario selecciona una de sus reservas activas y solicita su cancelación. El sistema
cancela la reserva y, si la cancelación se realiza con menos de 2 horas de aviso previo,
además registra un no-show contra el usuario.

**Por qué esta prioridad**: La cancelación completa el ciclo de vida de la reserva y es
esencial para la gestión de disponibilidad de canchas. Depende de las Historias 2 y 3
para un recorrido de usuario completo, pero es verificable con datos pre-cargados.

**Prueba Independiente**: Se puede probar pre-cargando una reserva activa y enviando una
solicitud de cancelación, luego verificando el cambio de estado y, cuando corresponda,
la creación del registro de no-show.

**Escenarios de Aceptación**:

1. **Dado** que un usuario tiene una reserva activa y la cancela con más de 2 horas de aviso previo, **Cuando** se confirma la cancelación, **Entonces** el estado de la reserva cambia a cancelada y no se registra ningún no-show.
2. **Dado** que un usuario cancela una reserva con menos de 2 horas antes de la hora de inicio, **Cuando** se confirma la cancelación, **Entonces** el estado de la reserva cambia a cancelada Y se crea un registro de no-show.
3. **Dado** que un usuario intenta cancelar una reserva que no le pertenece, **Cuando** se envía la cancelación, **Entonces** el sistema la rechaza con un error apropiado.
4. **Dado** que un usuario intenta cancelar una reserva que ya está cancelada, **Cuando** se envía la cancelación, **Entonces** el sistema la rechaza con un error apropiado.

---

### Casos Límite

- ¿Qué ocurre cuando dos usuarios intentan reservar la misma cancha y franja horaria simultáneamente? El sistema debe garantizar que solo uno tenga éxito; el otro recibe un error de superposición.
- ¿Qué ocurre cuando los horarios de inicio y fin de una reserva cruzan la medianoche? El horario operativo termina a las 11:00 PM — ninguna reserva puede cruzar ese límite.
- ¿Qué ocurre cuando el usuario proporciona un identificador que no tiene reservas existentes? Se devuelve una lista vacía; no se genera ningún error.
- ¿Qué ocurre cuando un usuario solicita una franja de 30 minutos (por debajo del mínimo de 1 hora)? El sistema debe rechazarla con un error de duración mínima.
- ¿Qué ocurre cuando un usuario solicita una franja que comienza exactamente 60 minutos desde ahora? Debe ser aceptada (el límite es inclusivo en el aviso previo de 1 hora).
- ¿Qué ocurre cuando un usuario intenta reservar una cancha que no existe? El sistema debe rechazarlo con un error claro de cancha no encontrada.
- ¿Qué ocurre cuando un usuario tiene 2 reservas activas pero ambas ya han pasado? Ambas pasan a `completed`; el usuario queda inmediatamente libre para hacer nuevas reservas hasta el límite de 2 activas simultáneamente.

## Requisitos *(obligatorio)*

### Requisitos Funcionales

- **RF-001**: El sistema DEBE mostrar todas las canchas de fútbol sintético disponibles y sus franjas horarias abiertas para una fecha especificada por el usuario, mostrando solo los incrementos de 30 minutos reservables dentro del horario operativo.
- **RF-002**: El sistema DEBE presentar una pantalla de ingreso de identificador cuando la aplicación se abre por primera vez; el identificador ingresado se retiene para todas las acciones posteriores dentro de la sesión. Todas las operaciones de reserva (crear, ver, cancelar) DEBEN usar este identificador de sesión sin requerir que el usuario lo ingrese nuevamente.
- **RF-003**: El sistema DEBE hacer cumplir que cada reserva abarque un mínimo de 1 hora y que tanto la hora de inicio como la de fin estén alineadas a incrementos de 30 minutos (p. ej., 10:00, 10:30, 11:00).
- **RF-004**: El sistema DEBE hacer cumplir el horario operativo: la hora de inicio de la reserva DEBE ser las 6:00 AM o posterior, y la hora de fin DEBE ser las 11:00 PM o anterior.
- **RF-005**: El sistema DEBE rechazar cualquier solicitud de reserva donde la hora de inicio sea menos de 1 hora desde el momento en que se envía la solicitud.
- **RF-006**: El sistema DEBE evitar que dos reservas para la misma cancha se superpongan en el tiempo; la segunda solicitud conflictiva DEBE ser rechazada.
- **RF-007**: El sistema DEBE rechazar una solicitud de reserva si el usuario solicitante ya tiene 2 o más reservas activas. Una reserva está activa solo mientras su hora de fin está en el futuro y no ha sido cancelada. Las reservas cuya hora de fin ha pasado pasan automáticamente a `completed` y NO DEBEN contarse para este límite.
- **RF-008**: El sistema DEBE mostrar todas las próximas reservas activas para el usuario de la sesión actual — reservas cuya hora de fin está en el futuro y no han sido canceladas. Las reservas completadas y canceladas NO DEBEN aparecer en esta vista.
- **RF-009**: El sistema DEBE permitir a un usuario cancelar una de sus reservas activas por identificador de reserva.
- **RF-010**: El sistema DEBE registrar un no-show cuando se envía una cancelación con menos de 2 horas de aviso previo antes de la hora de inicio de la reserva.
- **RF-011**: El sistema DEBE devolver un mensaje de error claro, específico y legible por humanos para cada violación de regla de dominio, identificando qué regla fue violada.
- **RF-012**: El sistema DEBE rechazar solicitudes de cancelación para reservas que no pertenecen al identificador de usuario solicitante.

### Entidades Clave

- **Cancha**: Una cancha de fútbol sintético disponible para reserva. Tiene un identificador único y un nombre legible por humanos. Las canchas están preconfiguradas; la creación y eliminación están fuera del alcance.
- **Reserva**: Una reserva de una cancha específica por un usuario para un rango de tiempo continuo en una fecha determinada. Tiene un identificador único, un identificador de usuario, referencia a la cancha, fecha, hora de inicio, hora de fin y estado (`active` / `completed` / `cancelled`). Transiciones de estado: `active` → `completed` automáticamente cuando pasa la hora de fin; `active` → `cancelled` cuando el usuario la cancela explícitamente. Solo las reservas `active` cuentan para el límite de 2 reservas.
- **NoShow**: Un registro de que un usuario canceló una reserva tarde. Vinculado a la reserva cancelada original. Contiene el identificador de usuario, referencia a la reserva y la marca de tiempo de la cancelación.

## Criterios de Éxito *(obligatorio)*

### Resultados Medibles

- **CE-001**: Un usuario puede completar exitosamente una reserva válida en 3 pasos o menos (seleccionar cancha + hora, enviar, recibir confirmación).
- **CE-002**: Cada violación de regla de dominio (superposición, horario operativo, aviso previo, duración, límite de reservas activas) resulta en un mensaje de error legible por humanos y distinto — 0 casos donde una regla es violada silenciosamente o con un error genérico.
- **CE-003**: La vista de disponibilidad refleja con precisión el estado de reservas en tiempo real — 0 casos donde una franja disponible mostrada está realmente ocupada, o viceversa.
- **CE-004**: La cancelación con aviso tardío produce de forma confiable un registro de no-show en el 100% de los casos que califican (cancelación < 2 horas antes del inicio).
- **CE-005**: Un usuario con una reserva activa existente para una cancha no puede crear una segunda reserva superpuesta para esa misma cancha — 0 reservas duplicadas en el sistema en ningún momento.

## Supuestos

- Un usuario ingresa su identificador (p. ej., un nombre o alias) una vez al abrir la aplicación; la interfaz lo retiene durante la sesión y lo usa automáticamente para todas las acciones (crear, ver y cancelar reservas). No se requiere contraseña, token de sesión ni autenticación. El sistema no verifica que el identificador pertenezca a una persona real.
- Las canchas de fútbol están pre-cargadas en el sistema y no pueden ser creadas ni eliminadas a través de la interfaz de usuario (la administración está fuera del alcance del MVP).
- No hay una duración máxima explícita de reserva más allá de la restricción de que la hora de fin debe caer a las 11:00 PM o antes del mismo día.
- El sistema opera en una sola zona horaria; no se requiere conversión ni soporte multi-zona horaria.
- Las solicitudes de reserva concurrentes para la misma franja son posibles; el sistema debe manejarlas correctamente (el primero en confirmarse gana, el segundo recibe un error de superposición).
- Los pagos y la facturación están completamente fuera del alcance de este MVP.
- Las notificaciones (correo electrónico, SMS, push) están completamente fuera del alcance de este MVP.
- Los registros de no-show se almacenan pero no se aplica ninguna consecuencia automatizada (baneo, penalización) en el MVP.
