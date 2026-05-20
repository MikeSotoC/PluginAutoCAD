# Sistema Catastral Institucional Peruano

Plugin para AutoCAD/ZWCAD que implementa la categorización catastral urbana según la **Resolución Ministerial N.° 277-2025-VIVIENDA**.

## Estructura del Proyecto

```
CatastroUrbano/
├── CatastroUrbano.sln          # Solución de Visual Studio
├── CatastroUrbano.csproj       # Proyecto .NET
├── README.md                   # Este archivo
└── src/
    ├── Commands/               # Comandos CAD (UC_CATEGORIZAR, UC_OBRASCOMP)
    │   ├── UCCommand.cs
    │   └── ObrasCompCommand.cs
    ├── Models/                 # Modelos de dominio
    │   ├── CatastroEntityModel.cs
    │   ├── CategoriaEdificacion.cs
    │   ├── ObrasComplementarias.cs
    │   └── TableRowModel.cs
    ├── CAD/                    # Motores de dibujo CAD
    │   ├── CadTransactionManager.cs
    │   ├── CadLineEngine.cs
    │   ├── CadTableDrawer.cs
    │   ├── MTextEngine.cs
    │   ├── LeaderEngine.cs
    │   └── DimensionEngine.cs
    ├── Analysis/               # Análisis geométrico y de áreas
    │   ├── PolylineAnalyzer.cs
    │   └── AreaCalculator.cs
    ├── Classification/         # Clasificación por layers
    │   └── LayerClassifier.cs
    ├── Geometry/               # Utilidades geométricas
    │   └── GeometryHelper.cs
    ├── Styles/                 # Gestión de estilos DWT
    │   └── StyleResolver.cs
    ├── Table/                  # Generación de cuadros técnicos
    │   ├── DynamicTableEngine.cs
    │   └── TableLayoutCalculator.cs
    └── Infrastructure/         # Infraestructura base
        └── ErrorHandler.cs
```

## Requisitos

- **.NET 6.0** o superior
- **AutoCAD 2023** o compatible (ZWCAD con API .NET)
- Referencias a las DLL de AutoCAD:
  - `AcMgd.dll`
  - `AcDbMgd.dll`
  - `AcCoreMgd.dll`

## Instalación

1. Ajustar las rutas de las referencias de AutoCAD en `CatastroUrbano.csproj` según tu instalación.
2. Compilar el proyecto:
   ```bash
   dotnet build
   ```
3. Copiar el ensamblado resultante (`CatastroUrbano.dll`) a la carpeta de plugins de AutoCAD.

## Comandos Disponibles

### UC_CATEGORIZAR
Orquestador principal del sistema. Ejecuta el flujo completo de:
- Análisis de polilíneas cerradas
- Clasificación por layer institucional
- Cálculo de áreas por piso y tipo
- Generación de cuadro técnico dinámico
- Acotado automático de perímetros
- Inserción de textos descriptivos

### UC_OBRASCOMP
Gestión de obras complementarias e instalaciones fijas y permanentes:
- Catálogo completo según Anexo III de RM 277-2025-VIVIENDA
- Inserción de leaders catastrales institucionales
- Descripción libre adicional opcional

## Normativa Aplicable

- **RM 277-2025-VIVIENDA**: Valores unitarios de edificación 2026
  - Anexo I: Categorías de edificación
  - Anexo II: Áreas mínimas de clasificación
  - Anexo III: Obras complementarias

## Licencia

Uso institucional exclusivo para entidades catastrales peruanas.

## Soporte

Para reportes de errores o consultas técnicas, contactar al área de Desarrollo del Sistema Catastral.
