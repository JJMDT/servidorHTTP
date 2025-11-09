# Explicación del servidor HTTP — archivo por archivo

Este documento describe paso a paso qué hace cada función de cada archivo del proyecto "ServidorHTTP". Está pensado para que lo uses en una exposición: incluye responsabilidades, entradas/salidas, efectos secundarios y puntos clave para comentar.

## Resumen corto

- Proyecto: servidor HTTP simple implementado con sockets en C#.
- Configuración: `Config.json` → puerto y carpeta raíz.
- Inicio: `Program.Main` lee la configuración y arranca `HttpServer`.
- Flujo por petición: aceptar socket → leer request → parsear con `HttpRequest` → decidir acción (servir archivo, redirect, 404) → construir respuesta con `HttpResponse.Build` → enviar → registrar en `Logs`.

---

## 1) `Config.json`

- Propiedades: `Port` (int), `RootDirectory` (string).
- Qué hace: define el puerto donde escucha el servidor (por ejemplo 8000) y la carpeta de archivos estáticos (por ejemplo `wwwroot`).
- Uso: leído por `Program.Main` para inicializar `ServerConfig`.

## 2) `Program.cs`

- Función principal: `Main(string[] args)`
  - Entradas: argumentos de línea de comandos (no usados actualmente).
  - Proceso:
    1. Lee `config.json` (File.ReadAllText).
    2. Deserializa JSON a `ServerConfig` (JsonSerializer.Deserialize).
    3. Si la config es nula, muestra error y termina.
    4. Crea `new HttpServer(config.Port, config.RootDirectory)` y ejecuta `await server.StartAsync()`.
  - Efectos: arranca el bucle principal del servidor que acepta conexiones.

> Puntos para la exposición: `Main` es el punto de entrada; todo el trabajo se delega en `HttpServer`.

## 3) `ServerConfig.cs`

- Clase simple con dos propiedades públicas:
  - `int Port`.
  - `string RootDirectory` (por defecto "wwwroot").
- Uso: contenedor de configuración pasado al servidor.

## 4) `HttpRequest.cs`

- Clase: `HttpRequest`
- Propiedades (solo lectura): `Method`, `Url`, `QueryParams`, `Body`.
- Constructor: `HttpRequest(string rawRequest)`
  - Entrada: la petición HTTP completa como texto (lo leído del socket y convertido a UTF-8).
  - Pasos:
    1. Separa `rawRequest` por `\r\n` para obtener líneas.
    2. Valida la primera línea (request line) y la divide por espacios.
    3. `Method` = primer token (ej. "GET" o "POST").
    4. `Url` y `QueryParams`: separa el token de ruta por `?`; `Url` queda con la ruta sin parámetros y `QueryParams` con la parte después de `?` o `-` si no existe.
    5. Si `Method == "POST"`, toma el cuerpo como lo que viene después de `\r\n\r\n` y aplica `WebUtility.UrlDecode`; si no hay cuerpo, asigna "-".
  - Limitaciones:
    - No parsea headers completos (por ejemplo no usa `Content-Length` para leer el body con precisión).
    - Asume que toda la petición cabe en la primera lectura del socket.
    - No maneja multipart/form-data ni transferencias chunked.

> Qué enfatizar: `HttpRequest` es un parseador simple que extrae lo esencial para decisiones básicas del servidor.

## 5) `HttpResponse.cs`

- Propósito: construir la respuesta HTTP completa en bytes (headers + body).
- Métodos importantes:

### `Build(int statusCode, string contentType, byte[] body, bool compress = false, IDictionary<string,string>? extraHeaders = null)`

- Entradas:
  - `statusCode`: código HTTP (200, 302, 404, ...).
  - `contentType`: valor para `Content-Type` (ej. `text/html; charset=UTF-8`).
  - `body`: contenido en bytes a enviar.
  - `compress`: si true, comprime body con gzip antes de calcular `Content-Length`.
  - `extraHeaders`: headers adicionales (ej. `Location` para 302).
- Proceso:
  1. Si `compress` es true, llama a `Compress(body)` y usa los bytes comprimidos como `finalBody`.
  2. Determina texto de estado según `statusCode` (200 -> OK, 404 -> Not Found, ...).
  3. Construye los headers: línea de estado, `Content-Type`, opcional `Content-Encoding: gzip`, headers extra, `Content-Length`.
  4. Convierte los headers a bytes UTF-8 y concatena con `finalBody`.
  5. Devuelve `byte[]` listo para enviar.

### `Compress(byte[] data)` (privado)

- Usa `GZipStream` sobre un `MemoryStream` y devuelve los bytes comprimidos.

> Puntos para la exposición: `Build` implementa la estructura esperada por HTTP/1.1 (línea de estado, headers, doble salto, cuerpo) y se asegura de que `Content-Length` corresponda al cuerpo final.

## 6) `HttpServer.cs`

- Clase central que mantiene el listener y procesa conexiones.
- Campos clave:
  - `_port`: puerto.
  - `_rootDirectory`: carpeta de archivos estáticos.
  - `_listener`: socket que acepta conexiones.
  - `_logLock`: objeto para sincronizar accesos al log.

### Constructor: `HttpServer(int port, string rootDirectory)`

- Asigna las propiedades internas con los valores recibidos.

### `GetContentType(string path)`

- Entrada: ruta o nombre de archivo.
- Proceso: obtiene extensión y mapea a un MIME type simple (ej. `.html` -> `text/html; charset=UTF-8`, `.css` -> `text/css`, `.js` -> `application/javascript`, `.png` -> `image/png`).
- Uso: se usa para el header `Content-Type` al servir archivos.

### `StartAsync()`

- Objetivo: abrir socket, hacer `Bind` y `Listen`, aceptar conexiones en bucle.
- Proceso:
  1. Crea socket TCP y hace `Bind` a `IPAddress.Any` y `_port`.
  2. Llama `Listen`.
  3. En bucle infinito acepta clientes y lanza `HandleClientRequestAsync` para cada uno en tareas separadas (concurrencia).

> En la exposición: explicar `bind/listen/accept` y la idea de atender cada conexión en una tarea para permitir múltiples clientes.

### `HandleClientRequestAsync(Socket clientSocket)`

- Entrada: socket ya aceptado.
- Flujo detallado:
  1. Lee del socket en un buffer (ReceiveAsync) y convierte a texto (`rawRequest`).
  2. Crea `new HttpRequest(rawRequest)` para obtener `Method`, `Url`, `QueryParams`, `Body`.
  3. Normaliza la ruta: si `Url` es `/` se pone `/index.html`.
  4. Construye `filePath` combinando `_rootDirectory` y la ruta solicitada; obtiene `fullPath` con `Path.GetFullPath`.
  5. Seguridad: obtiene `rootPath = Path.GetFullPath(_rootDirectory)` y verifica `fullPath.StartsWith(rootPath)`. Si no, responde 403 (Forbidden) para evitar directory traversal.
  6. Si `Method == "POST"`:
     - Registra la petición (LogRequest) incluyendo el `Body`.
     - Construye y envía respuesta 302 con header `Location: /` (redirección hacia la página principal).
  7. Si `File.Exists(filePath)` (archivo existe):
     - Lee bytes del archivo.
     - Determina `contentType` con `GetContentType`.
     - Decide si comprimir (por ejemplo si `contentType` empieza con `text/` o es JS) y llama a `HttpResponse.Build(200, contentType, bytes, compress)`.
     - Envía la respuesta y registra entrada de log con status 200.
  8. Si el archivo no existe:
     - Intenta servir `wwwroot/404.html` si está disponible; si no, genera un HTML simple.
     - Envía respuesta con status 404 y registra en log.
  9. Cierra y libera el socket cliente.

- Limitaciones para comentar:
  - Asume que la petición entra completa en la primera lectura del socket (no maneja cuerpos grandes ni chunked).
  - No implementa keep-alive ni TLS.

### `LogRequest(...)`

- Propósito: registrar en la carpeta `Logs` información de cada petición.
- Proceso:
  1. Prepara la entrada con fecha, IP, método, recurso, query, body, fichero servido y status.
  2. Asegura la existencia de la carpeta `Logs`.
  3. Apendea la entrada a `Logs/log_YYYY-MM-DD.txt`.
  4. Usa `lock(_logLock)` para evitar escrituras concurrentes corruptas.

> Ejemplo real: ver `Logs/log_2025-11-09.txt` — contiene varias entradas GET a `/`, `style.css`, `script.js` con status 200.

## 7) `wwwroot` (archivos estáticos)

- `index.html`:
  - Página principal con enlaces y dos formularios: uno GET (query params) y uno POST.
  - Incluye `script.js` y `style.css`.

- `script.js`:
  - Añade listeners a los formularios para mostrar un `alert('Formulario enviado')` al enviar, pero permite el envío normal (no llama a `preventDefault`).
  - Propósito: demo visual para el usuario; el servidor recibe la petición normalmente.

- `style.css`, `test.html`, `404.html`:
  - Archivos estéticos o de prueba que el servidor sirve cuando el navegador los solicita.

## 8) `Logs/log_YYYY-MM-DD.txt` (ejemplo `log_2025-11-09.txt`)

- Formato de cada entrada:
  - Fecha: timestamp
  - IP: dirección del cliente
  - Método: GET / POST
  - Recurso: ruta solicitada
  - Query: parámetros de consulta o `-`
  - Body: cuerpo o `-`
  - Servido: ruta del archivo servido (por ejemplo `wwwroot/index.html`)
  - Status: código HTTP devuelto (200, 404, ...)

> Uso en la exposición: muestra entradas reales del log para demostrar cómo se registran las peticiones.

---

## Sugerencia de guion para la exposición (diapositivas)

1. Diapositiva 1 — Diagrama de alto nivel: Cliente ↔ Socket ↔ `HttpServer` ↔ filesystem (`wwwroot`).
2. Diapositiva 2 — `Program.cs` y `Config.json` (punto de entrada y configuración).
3. Diapositiva 3 — `StartAsync()` (bind/listen/accept) y la idea de concurrencia (una tarea por cliente).
4. Diapositiva 4 — `HandleClientRequestAsync()` paso a paso (parseo, seguridad, servir archivo, redirect POST, 404).
5. Diapositiva 5 — `HttpRequest` y `HttpResponse` — parseo básico y construcción de respuesta (headers, content-length, compresión gzip).
6. Diapositiva 6 — Logging y limitaciones (no soporta keep-alive, Content-Length robusto, TLS, lecturas grandes).
7. Diapositiva final — Demo con una entrada real de `Logs/log_2025-11-09.txt`.

## Notas técnicas y puntos para enfatizar

- Seguridad: la verificación `fullPath.StartsWith(rootPath)` evita directory traversal (explica con ejemplo `../`).
- Robustez: idealmente habría que leer `Content-Length` para manejar correctamente POST grandes; en este servidor se asume requests pequeños.
- Rendimiento: la compresión gzip reduce bytes enviados para texto, pero consume CPU; el código usa `CompressionLevel.Fastest`.
- Extensiones sencillas: cómo añadir más MIME types en `GetContentType`, soporte para HEAD, o habilitar TLS (HTTPS).

---

### Cierre

Este `explicacion.md` está listo para copiar su contenido a tus diapositivas. Si quieres, puedo generar automáticamente una versión con texto por diapositiva (`slides.md`) o un PPTX básico con ese contenido.
