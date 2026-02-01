window.babel = window.babel || {};

window.babel.applyTheme = function(themeClass, isDarkMode) {
    const body = document.body;
    
    body.classList.remove(
        'theme-light',
        'theme-dark',
        'theme-midnight',
        'theme-forest',
        'theme-ocean',
        'theme-sunset',
        'theme-aurora',
        'theme-hacker'
    );
    
    body.classList.add(themeClass);
    
    if (isDarkMode) {
        body.classList.add('dark-mode');
    } else {
        body.classList.remove('dark-mode');
    }
    
    console.log(`Tema aplicado: ${themeClass}, Modo oscuro: ${isDarkMode}`);
};
