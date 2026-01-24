# End Session Skill

Gestiona el final de una sesión de trabajo:
1. Crea/actualiza el documento de sesión en `docs/sessions/`
2. Actualiza el diario de desarrollo HTML
3. Solicita nombre de rama Git
4. Hace commit y push de los cambios

## Uso

```
/end-session
```

## Parámetros

Ninguno - el skill es interactivo y solicitará la información necesaria.

## Flujo de Trabajo

1. **Generar timestamp** para el nombre del archivo de sesión
2. **Crear documento de sesión** con toda la información de la sesión actual
3. **Actualizar diario HTML** (`docs/dev-diary.html`) con resumen de la sesión
4. **Solicitar nombre de rama** al usuario
5. **Crear commit** con mensaje descriptivo
6. **Push a la rama** especificada

## Plantilla del Documento de Sesión

El documento debe incluir:
- Título con fecha y hora
- Resumen de la sesión
- Cambios implementados (con código)
- Problemas encontrados y soluciones
- Desafíos técnicos
- Configuración final
- Próximos pasos
- Comandos útiles
- Lecciones aprendidas
- Estado final del proyecto

## Estructura del Diario HTML

```html
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Babel - Diario de Desarrollo</title>
    <style>
        /* Estilos simples y limpios */
    </style>
</head>
<body>
    <header>
        <h1>Babel - Diario de Desarrollo</h1>
    </header>
    <nav>
        <ul>
            <!-- Lista de sesiones con enlaces -->
        </ul>
    </nav>
    <main>
        <!-- Resúmenes de sesiones -->
    </main>
</body>
</html>
```

## Mensaje de Commit

Formato: `Session {YYYYMMDD_HHMMSS}: {descripción breve}`

Ejemplo: `Session 20260124_112606: Proyecto base y corrección QdrantClient`

## Actualización de CLAUDE.md

Debe actualizar la sección "Historial de Sesiones" en CLAUDE.md con referencia al nuevo documento.
