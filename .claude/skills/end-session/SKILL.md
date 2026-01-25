---
name: end-session
description: Finaliza la sesión de trabajo creando documentación de sesión, actualizando el diario de desarrollo y haciendo commit/push a Git
---

# End Session Skill

Gestiona el cierre automático de una sesión de trabajo.

## Uso

```
/end-session
```

## Pasos a Ejecutar

Sigue estos pasos en orden:

### Paso 1: Generar Timestamp

Ejecuta:
```bash
date +"%Y%m%d_%H%M%S"
```

Guarda este timestamp para usarlo en los nombres de archivo.

### Paso 2: Crear Documento de Sesión

Crea el archivo `docs/sessions/{timestamp}_descripcion_breve.md` con:

1. **Título:** "# Sesión YYYY-MM-DD HH:MM - Descripción Breve"
2. **Resumen de la Sesión:** Breve descripción de qué se hizo
3. **Cambios Implementados:** Lista detallada con snippets de código
4. **Problemas Encontrados y Soluciones:** Errores y cómo se resolvieron
5. **Desafíos Técnicos:** Problemas de compatibilidad, decisiones de diseño
6. **Configuración Final:** Estado de archivos de configuración
7. **Próximos Pasos:** Qué falta por hacer
8. **Comandos Útiles:** Comandos relevantes ejecutados
9. **Lecciones Aprendidas:** Aprendizajes clave
10. **Estado Final del Proyecto:** Checklist de lo completado

### Paso 3: Actualizar Diario HTML

Lee el archivo `docs/dev-diary.html` y añade la nueva sesión al array `sessions` en el script de JavaScript.

**Nueva entrada de sesión:**
```javascript
{
    id: '{timestamp}',
    date: '{fecha YYYY-MM-DD}',
    time: '{hora HH:MM}',
    duration: '{X.Xh}',
    title: '{Título breve}',
    summary: '{Resumen de 2-3 líneas}',
    tags: ['{tag1}', '{tag2}'],
    docPath: 'sessions/{timestamp}_archivo.md'
}
```

La nueva sesión debe ser la primera del array (más reciente primero).

También actualiza los valores estáticos en el HTML:
- `id="total-sessions"`: Incrementar en 1
- `id="total-hours"`: Sumar la duración de la nueva sesión
- `id="last-session"`: Actualizar a la fecha de hoy

### Paso 4: Actualizar CLAUDE.md

Añade la nueva sesión a la sección "## Historial de Sesiones" en CLAUDE.md:

```markdown
- **{timestamp}_{descripcion}.md** - Descripción breve de la sesión
```


### Paso 5: Mensaje Final

Informa al usuario con una tabla de resumen:

| Paso | Estado |
|------|--------|
| Documento de sesión creado | ✅ `docs/sessions/{timestamp}_descripcion.md` |
| Diario de desarrollo actualizado | ✅ `docs/dev-diary.html` |
| CLAUDE.md actualizado | ✅ Nueva sesión referenciada |


## Tags Disponibles

- `setup` - Configuración inicial o de entorno
- `feature` - Nueva funcionalidad
- `bugfix` - Corrección de errores
- `refactor` - Refactorización de código
- `documentation` - Documentación
- `test` - Tests
