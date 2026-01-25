-- Script de inicialización de base de datos Babel
-- Este script se ejecuta automáticamente al iniciar el contenedor de SQL Server

-- Crear base de datos principal si no existe
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'BabelDb')
BEGIN
    CREATE DATABASE BabelDb;
    PRINT 'Base de datos BabelDb creada exitosamente';
END
ELSE
BEGIN
    PRINT 'Base de datos BabelDb ya existe';
END
GO

-- Crear base de datos para Hangfire si no existe
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'BabelHangfire')
BEGIN
    CREATE DATABASE BabelHangfire;
    PRINT 'Base de datos BabelHangfire creada exitosamente';
END
ELSE
BEGIN
    PRINT 'Base de datos BabelHangfire ya existe';
END
GO

-- Usar la base de datos principal
USE BabelDb;
GO

-- Crear usuario de aplicación (opcional, para producción)
-- En desarrollo usamos 'sa' directamente
/*
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = N'babel_app')
BEGIN
    CREATE LOGIN babel_app WITH PASSWORD = 'BabelApp@2024!';
    CREATE USER babel_app FOR LOGIN babel_app;
    ALTER ROLE db_owner ADD MEMBER babel_app;
    PRINT 'Usuario babel_app creado exitosamente';
END
GO
*/

PRINT 'Inicialización de base de datos completada';
GO
