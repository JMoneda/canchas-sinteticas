# Guía de StyleCop.Analyzers

## Resumen de Implementación

StyleCop.Analyzers ha sido implementado en la solución para garantizar la consistencia de estilo de código en todos los proyectos:
- CanchasSinteticas.Api
- CanchasSinteticas.Infrastructure
- CanchasSinteticas.Application
- CanchasSinteticas.Domain

## Archivos de Configuración

### .editorconfig
Define las preferencias de formato del código:
- **Charset**: UTF-8
- **Indentación**: 4 espacios
- **Nueva línea al final del archivo**: Requerida
- **Espacios en blanco finales**: Se eliminan automáticamente

#### Reglas C# principales:

**Nuevas líneas:**
- Apertura de llaves en línea nueva
- `else`, `catch`, `finally` en línea nueva
- Miembros de inicializadores de objetos en línea nueva

**Indentación:**
- Case contents: con indentación
- Switch labels: con indentación
- Etiquetas: un nivel menos que el contenido

**Espacios:**
- Sin espacio después del casting
- Espacios alrededor de operadores binarios
- Sin espacio entre paréntesis

**Bloques de código:**
- Se prefieren llaves en todos los casos
- Se prefieren instrucciones using simplificadas

### stylecop.json
Configuración específica de StyleCop:

**Documentación:**
- Se requiere documentación para miembros públicos expuestos
- Se requiere documentación para interfaces
- Compañía: "Canchas Sintéticas"

**Convenciones de nombres:**
- El nombre del archivo debe coincidir con el tipo principal declarado
- Se prefieren nombres sin guión bajo inicial en minúsculas

**Orden de elementos:**
- OuterType → InnerType → InnerEnum → Field → Constructor → Finalizer → Property → Indexer → Method → Operator → Event → Delegate

**Espacio:**
- Indentación: 4 espacios
- Sin tabulaciones

## Reglas Comunes de StyleCop

### SA10xx - Espaciados
- **SA1000**: Las palabras clave no deben ir precedidas de un espacio
- **SA1001**: Las comas deben ser seguidas de un espacio
- **SA1008**: Las aberturas de paréntesis no deben ir precedidas de un espacio

### SA11xx - Nomenclatura
- **SA1101**: No use prefijos o sufijos para variables de campo
- **SA1102**: Los nombres de queryables deben ser descriptivos
- **SA1106**: El código no debe contener espacios múltiples
- **SA1121**: Use nombres incorporados de tipos integrados

### SA12xx - Documentación
- **SA1600**: Los elementos públicos deben estar documentados
- **SA1601**: Los parámetros deben estar documentados
- **SA1602**: Los atributos enumerados deben estar documentados

### SA13xx - Desorden
- **SA1301**: El nombre del elemento debe comenzar con una letra o símbolo de subrayado
- **SA1302**: Los elementos de interfaz deben ser prefijados correctamente
- **SA1303**: Las constantes deben comenzar con mayúsculas

### SA14xx - Líneas de acceso
- **SA1401**: Los campos deben ser privados

### SA15xx - Sentencias
- **SA1500**: Las llaves para construcciones de múltiples líneas deben estar en nuevas líneas
- **SA1501**: Las sentencias de declaración deben ocupar su propia línea
- **SA1502**: El elemento no debe estar en una sola línea

### SA16xx - Comparación
- **SA1600**: Los elementos públicos deben estar documentados

## Reglas Deshabilitadas Comúnmente

Si necesitas deshabilitar alguna regla específica en tu código, puedes usar:

```csharp
#pragma warning disable SA1600
// Tu código aquí
#pragma warning restore SA1600
```

O en el archivo entero al inicio:

```csharp
#pragma warning disable SA1600
```

## Cómo Ejecutar StyleCop

1. **Compilación automática**: StyleCop se ejecuta automáticamente durante la compilación
2. **Análisis en Visual Studio**: Los errores aparecerán en la ventana de errores y en el editor
3. **Línea de comandos**: 
   ```bash
   dotnet build /p:EnforceCodeStyleInBuild=true
   ```

## Configuración Adicional

Si necesitas ajustar las reglas en el futuro:

1. Edita `stylecop.json` para cambiar reglas de StyleCop
2. Edita `.editorconfig` para cambiar preferencias de formato
3. Cualquier cambio se aplicará automáticamente en la siguiente compilación

## Referencias Útiles

- [Documentación oficial de StyleCop.Analyzers](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)
- [Documentación de EditorConfig](https://editorconfig.org)
- [Reglas de StyleCop completas](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/StyleCopRules.md)

---

**Versión**: 1.0  
**Fecha de implementación**: 2024  
**Responsable**: Equipo de desarrollo  
