/**
 * End Session Skill
 *
 * Gestiona el cierre de una sesión de trabajo:
 * - Crea documento de sesión
 * - Actualiza diario HTML
 * - Commit y push a rama Git
 */

module.exports = {
  name: 'end-session',
  description: 'Finaliza la sesión: crea documentación, actualiza diario y sube cambios a Git',

  async execute(context) {
    const { prompt, tools } = context;

    // Este skill se ejecutará en modo conversacional
    // Claude usará los siguientes pasos:

    return {
      instructions: `
Al ejecutar este skill, sigue estos pasos en orden:

## Paso 1: Generar Timestamp

Ejecuta:
\`\`\`bash
date +"%Y%m%d_%H%M%S"
\`\`\`

Guarda este timestamp para usarlo en los nombres de archivo.

## Paso 2: Crear Documento de Sesión

Crea el archivo \`docs/sessions/{timestamp}_descripcion_breve.md\` con:

1. **Título:** "Sesión YYYY-MM-DD HH:MM - Descripción Breve"
2. **Resumen de la Sesión**
3. **Cambios Implementados** (con snippets de código)
4. **Problemas Encontrados y Soluciones**
5. **Desafíos Técnicos**
6. **Configuración Final**
7. **Próximos Pasos**
8. **Comandos Útiles**
9. **Lecciones Aprendidas**
10. **Estado Final del Proyecto**

## Paso 3: Crear/Actualizar Diario HTML

Si \`docs/dev-diary.html\` no existe, créalo con la estructura base.
Si existe, añade la nueva entrada de sesión.

**Información a incluir:**
- Fecha y hora de la sesión
- Duración estimada (calcula basándote en los timestamps de inicio/fin de la conversación si están disponibles)
- Resumen breve (2-3 líneas)
- Enlace al documento de sesión completo
- Tags/categorías (ej: "setup", "bugfix", "feature", "documentation")

**Estructura HTML:**

\`\`\`html
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Babel - Diario de Desarrollo</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            line-height: 1.6;
            color: #333;
            background: #f5f5f5;
        }
        header {
            background: #2c3e50;
            color: white;
            padding: 2rem;
            text-align: center;
        }
        nav {
            background: white;
            padding: 1rem;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        nav ul {
            list-style: none;
            display: flex;
            flex-wrap: wrap;
            gap: 1rem;
            justify-content: center;
        }
        nav a {
            color: #3498db;
            text-decoration: none;
            padding: 0.5rem 1rem;
            border-radius: 4px;
            transition: background 0.3s;
        }
        nav a:hover { background: #ecf0f1; }
        main {
            max-width: 900px;
            margin: 2rem auto;
            padding: 0 1rem;
        }
        .session {
            background: white;
            padding: 2rem;
            margin-bottom: 2rem;
            border-radius: 8px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
        }
        .session-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 1rem;
            padding-bottom: 1rem;
            border-bottom: 2px solid #ecf0f1;
        }
        .session-title {
            font-size: 1.5rem;
            color: #2c3e50;
        }
        .session-meta {
            color: #7f8c8d;
            font-size: 0.9rem;
        }
        .session-tags {
            display: flex;
            gap: 0.5rem;
            margin: 1rem 0;
        }
        .tag {
            background: #3498db;
            color: white;
            padding: 0.25rem 0.75rem;
            border-radius: 12px;
            font-size: 0.85rem;
        }
        .session-summary {
            margin: 1rem 0;
            color: #555;
        }
        .session-link {
            display: inline-block;
            margin-top: 1rem;
            color: #3498db;
            text-decoration: none;
            font-weight: 500;
        }
        .session-link:hover { text-decoration: underline; }
        .stats {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 1rem;
            margin: 2rem 0;
        }
        .stat-card {
            background: white;
            padding: 1.5rem;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            text-align: center;
        }
        .stat-value {
            font-size: 2rem;
            font-weight: bold;
            color: #3498db;
        }
        .stat-label {
            color: #7f8c8d;
            margin-top: 0.5rem;
        }
    </style>
</head>
<body>
    <header>
        <h1>🏗️ Babel - Diario de Desarrollo</h1>
        <p>Sistema de Gestión Documental con IA y OCR</p>
    </header>

    <nav id="session-menu">
        <ul>
            <!-- Sesiones se añadirán aquí -->
        </ul>
    </nav>

    <main>
        <div class="stats">
            <div class="stat-card">
                <div class="stat-value" id="total-sessions">0</div>
                <div class="stat-label">Sesiones Totales</div>
            </div>
            <div class="stat-card">
                <div class="stat-value" id="total-hours">0h</div>
                <div class="stat-label">Horas de Desarrollo</div>
            </div>
            <div class="stat-card">
                <div class="stat-value" id="last-session">-</div>
                <div class="stat-label">Última Sesión</div>
            </div>
        </div>

        <div id="sessions-container">
            <!-- Sesiones se añadirán aquí -->
        </div>
    </main>
</body>
</html>
\`\`\`

**JavaScript para añadir nueva sesión:**

Al final del \`</body>\`, añade:

\`\`\`html
<script>
const sessions = [
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
    // ... sesiones anteriores
];

// Renderizar menú
const menu = document.getElementById('session-menu').querySelector('ul');
sessions.forEach(session => {
    const li = document.createElement('li');
    li.innerHTML = \`<a href="#session-\${session.id}">\${session.date}</a>\`;
    menu.appendChild(li);
});

// Renderizar sesiones
const container = document.getElementById('sessions-container');
sessions.forEach(session => {
    const div = document.createElement('div');
    div.className = 'session';
    div.id = \`session-\${session.id}\`;
    div.innerHTML = \`
        <div class="session-header">
            <h2 class="session-title">\${session.title}</h2>
            <div class="session-meta">
                📅 \${session.date} \${session.time} | ⏱️ \${session.duration}
            </div>
        </div>
        <div class="session-tags">
            \${session.tags.map(tag => \`<span class="tag">\${tag}</span>\`).join('')}
        </div>
        <p class="session-summary">\${session.summary}</p>
        <a href="\${session.docPath}" class="session-link">📄 Ver documentación completa →</a>
    \`;
    container.appendChild(div);
});

// Actualizar estadísticas
document.getElementById('total-sessions').textContent = sessions.length;
const totalHours = sessions.reduce((sum, s) => sum + parseFloat(s.duration), 0);
document.getElementById('total-hours').textContent = totalHours.toFixed(1) + 'h';
document.getElementById('last-session').textContent = sessions[0]?.date || '-';
</script>
\`\`\`

## Paso 4: Actualizar CLAUDE.md

Añade la nueva sesión a la sección "Historial de Sesiones" en CLAUDE.md.

## Paso 5: Solicitar Nombre de Rama

Usa AskUserQuestion para preguntar:

**Pregunta:** "¿En qué rama quieres guardar estos cambios?"

**Opciones:**
1. "main" (rama principal)
2. "feature/{nombre}" (nueva funcionalidad)
3. "bugfix/{nombre}" (corrección de bug)
4. "docs/session-{timestamp}" (rama de documentación - RECOMENDADO)

## Paso 6: Git Add, Commit y Push

\`\`\`bash
# Cambiar a la rama (crear si no existe)
git checkout -b {rama_elegida} || git checkout {rama_elegida}

# Añadir archivos específicos
git add docs/sessions/{timestamp}_*.md
git add docs/dev-diary.html
git add CLAUDE.md
git add .claude/

# Si hay otros archivos modificados, añadirlos también
git add src/

# Crear commit
git commit -m "Session {timestamp}: {descripción_breve}

- Documento de sesión creado
- Diario de desarrollo actualizado
- CLAUDE.md actualizado con nuevas instrucciones

Cambios principales:
- {lista de cambios principales}
"

# Push a la rama
git push -u origin {rama_elegida}
\`\`\`

## Paso 7: Mensaje Final

Informa al usuario:

"✅ Sesión finalizada y documentada

📝 Documento de sesión: docs/sessions/{timestamp}_descripcion.md
📊 Diario actualizado: docs/dev-diary.html
🌿 Rama Git: {rama_elegida}
🚀 Cambios subidos a origin/{rama_elegida}

Para ver el diario, abre: file:///{ruta_absoluta}/docs/dev-diary.html"
      `
    };
  }
};
