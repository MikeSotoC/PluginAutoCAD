# Catastro Urbano - Plugin CAD para Gestión Catastral

## Descripción
Plugin profesional para AutoCAD 2021 y ZWCAD 2021 que permite la gestión catastral urbana según normativa peruana RM 277-2025-VIVIENDA.

## Características

### 🎨 Interfaz Gráfica (WinForms)
- **Diseño compacto y moderno** estilo AutoCAD
- 4 pestañas principales: Dibujo, Análisis, Clasificación y Tablas
- Botones coloridos con íconos emoji para identificación rápida
- Barra de estado con indicador de progreso
- Tooltips descriptivos en cada botón

### 📐 Funcionalidades

#### Pestaña Dibujo
- 📍 **Lote**: Dibujar polígonos de lotes catastrales
- 🏘️ **Manzana**: Crear manzanas urbanas
- 🏢 **Construcción**: Delimitar edificaciones
- ✏️ **Editar**: Modificar elementos existentes
- 🗑️ **Eliminar**: Remover elementos del dibujo

#### Pestaña Análisis
- 📐 **Área**: Calcular áreas de polígonos
- 📏 **Perímetro**: Medir perímetros
- ✓ **Validar**: Verificar geometrías
- 🔍 **Superposiciones**: Detectar overlaps

#### Pestaña Clasificación
- 🏷️ **Clasificar**: Asignar usos de suelo (Residencial, Comercial, Industrial, etc.)
- Listado de resultados con historial

#### Pestaña Tablas
- 📊 **Tabla Catastral**: Generar tablas automáticas
- 📈 **Exportar Excel**: Exportar datos a hojas de cálculo
- 📄 **Reporte**: Generar reportes PDF

## Requisitos

### Software
- **AutoCAD 2021** o **ZWCAD 2021**
- **.NET Framework 4.7** o superior
- **Visual Studio 2019** (para desarrollo/compilación)

### Hardware
- Windows 10/11 (64-bit)
- 4 GB RAM mínimo
- Espacio en disco: 100 MB

## Instalación

### Opción 1: Usando el instalador (Recomendado)
1. Ejecutar `CatastroUrbano_Setup.exe`
2. Seguir el asistente de instalación
3. Reiniciar AutoCAD/ZWCAD

### Opción 2: Manual
1. Copiar `CatastroUrbano.dll` a:
   - AutoCAD: `%APPDATA%\Autodesk\ApplicationPlugins\CatastroUrbano.bundle\Contents\`
   - ZWCAD: `%APPDATA%\ZWSOFT\ZWCAD\2021\Applications\`
2. Copiar el archivo `.bundle` correspondiente
3. Reiniciar el CAD

### Opción 3: Carga temporal (desarrollo)
```
(en AutoCAD/ZWCAD)
Command: NETLOAD
Seleccionar: CatastroUrbano.dll
```

## Uso

### Comandos disponibles

| Comando | Alias | Descripción |
|---------|-------|-------------|
| `CATASTRO_UI` | `CU` | Abrir interfaz gráfica principal |
| `UC_CATEGORIZAR` | `UC` | Ejecutar categorización automática |
| `OBRAS_COMP` | `OC` | Gestionar obras complementarias |

### Flujo de trabajo típico

1. **Iniciar plugin**: Escribir `CU` en la línea de comandos
2. **Dibujar elementos**: Usar pestaña "Dibujo" para crear lotes, manzanas, construcciones
3. **Analizar**: Calcular áreas, validar geometrías
4. **Clasificar**: Asignar uso de suelo a cada elemento
5. **Generar tablas**: Exportar información catastral

## Estructura del Proyecto

```
CatastroUrbano/
├── UI/                      # Interfaz gráfica WinForms
│   ├── MainForm.cs          # Formulario principal
│   ├── MainForm.resx        # Recursos del formulario
│   └── UIInitializer.cs     # Inicializador WinForms
├── src/
│   ├── Commands/            # Comandos de AutoCAD
│   │   ├── UCCommand.cs     # Comando principal
│   │   └── ObrasCompCommand.cs
│   ├── Models/              # Modelos de datos
│   ├── CAD/                 # Motores de dibujo CAD
│   ├── Analysis/            # Análisis geométrico
│   ├── Classification/      # Clasificación de elementos
│   ├── Geometry/            # Utilidades geométricas
│   ├── Styles/              # Estilos y capas
│   ├── Table/               # Generación de tablas
│   └── Infrastructure/      # Error handling, logs
├── Properties/
│   └── AssemblyInfo.cs      # Información del ensamblado
├── CatastroUrbano.csproj    # Proyecto .NET
├── CatastroUrbano.sln       # Solución VS 2019
└── README.md                # Este archivo
```

## Compilación

### Desde Visual Studio 2019
1. Abrir `CatastroUrbano.sln`
2. Seleccionar configuración `Release`
3. Compilar (`Ctrl+Shift+B`)

### Desde línea de comandos
```bash
# Para AutoCAD
dotnet build -c Release

# Para ZWCAD
dotnet build -c Release -p:UseZwcad=true
```

## Desarrollo

### Agregar nuevos comandos
1. Crear clase en `src/Commands/`
2. Decorar con `[CommandMethod("NOMBRE_COMANDO")]`
3. Registrar en `CatastroPlugin.Initialize()`

### Personalizar la UI
- Editar `UI/MainForm.cs`
- Los colores usan formato `Color.FromArgb(R, G, B)`
- Los íconos son emojis Unicode

## Soporte

- **Documentación**: Ver comentarios XML en el código fuente
- **Logs**: `%APPDATA%\CatastroUrbano\Logs\`
- **Versión**: 1.0.0

## Licencia

Software propietario - Uso institucional

---

**Sistema Catastral Institucional Peruano**  
Compatible con normativa RM 277-2025-VIVIENDA
