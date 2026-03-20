# Proyecto de Reconocimiento Facial

Aplicación de escritorio WPF desarrollada en **.NET 10** enfocada en la detección y reconocimiento facial de forma autónoma y altamente eficiente. Utiliza un enfoque MVVM estricto, procesamiento de video en segundo plano y almacenamiento local ligero.

## 🚀 Características Principales

* **Registro de Empleados (Enrollment):** Captura de rostros en tiempo real mediante cámara web, realizando promedios de múltiples tomas (embeddings) para asegurar máxima precisión. Prevención automática de colisiones (similitud alta con usuarios existentes).
* **Análisis de Video (Analysis):** Procesamiento asíncrono de archivos `.mp4` con listas de espera. Incorpora una estrategia de *frame skipping* (procesando solo 2-3 FPS) para maximizar el rendimiento sin comprometer las detecciones.
* **Alta Eficiencia:** Usa comparaciones de similitud del coseno directamente en la memoria RAM (caché) para evitar cuellos de botella por accesos constantes a la base de datos fotograma a fotograma.
* **100% Portable:** El proyecto está diseñado para funcionar de manera autocontenida usando rutas relativas automáticas.

## 🛠️ Tecnologías y Librerías

* **Framework:** .NET 10 (WPF - Windows Presentation Foundation)
* **Arquitectura:** MVVM (vía `CommunityToolkit.Mvvm`)
* **Base de Datos:** SQLite (`Microsoft.Data.Sqlite` + micro-ORM `Dapper`)
* **Procesamiento de Visión:** `OpenCvSharp4` (Manejo de cámara y lectura de video)
* **Motor de IA:** `Microsoft.ML.OnnxRuntime`
* **Modelos Base:** YOLO (Detección de rostro) e InsightFace (Extracción de características/Embeddings).

## ⚙️ Cómo empezar (Instalación local)

1. **Clonar el repositorio:**
   ```bash
   git clone <tu-url-del-repo>
   cd ReconocimientoFacial
   ```

2. **Requisitos previos:** 
   - Tener instalado Visual Studio con la carga de trabajo de desarrollo de escritorio de .NET.
   - Tener instalado el SDK de **.NET 10**.

3. **Descargar los Modelos ONNX (¡Paso Crítico!):**
   Debido a cuestiones de tamaño, los modelos `.onnx` fueron ignorados del repositorio. **Debes obtener/generar tus propios modelos YOLO e InsightFace en formato ONNX** y colocarlos en la carpeta `Models/` en la ruta base donde se compilará el ejecutable (o configurar tu `.csproj` para copiarlos).

4. **Compilar y Ejecutar:**
   Inicia la solución en Visual Studio. En su primera ejecución, la aplicación creará automáticamente la carpeta `Data/` y el archivo de base de datos `.db` local de SQLite tal como fue instruido.

## 📂 Organización de Carpetas

* `Models/`: Modelos de negocio y entidades de la base de datos (Ej. `Employee.cs`, `DetectionLog.cs`).
* `ViewModels/`: Lógica de presentación y enlace a la interfaz, aislando la lógica de la UI.
* `Views/`: Ventanas y controles de usuario (Pantallas de registro y análisis).
* `Services/`: Servicios y operaciones de repositorios para leer/escribir base de datos.
* `Data/`: Inicialización de SQLite y administración de esquemas (`DatabaseInitializer.cs`).
* `Core/`: Clases críticas (Manejo de extensiones y, posteriormente, los motores de IA `FaceDetector` y `FaceRecognizer`).
