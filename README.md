# Servidor HTTP en C#

Servidor HTTP educativo implementado desde cero en C# usando sockets TCP, sin ASP.NET ni otro framework web. El proyecto sirve archivos estáticos desde `wwwroot`, procesa solicitudes GET y POST, devuelve errores 404, aplica compresión GZIP a algunos archivos de texto y registra las solicitudes en la carpeta `logs`.

## Requisitos

- Windows
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Una terminal, PowerShell o Visual Studio Code

Para comprobar que .NET está instalado:

```powershell
dotnet --version
```

## Estructura del proyecto

```text
servidorHTTP/
├── HttpRequest.cs          # Interpreta solicitudes HTTP
├── HttpResponse.cs         # Construye respuestas HTTP y comprime contenido
├── HttpServer.cs           # Socket TCP, enrutamiento y atención de clientes
├── Program.cs              # Punto de entrada de la aplicación
├── ServerConfig.cs         # Modelo de configuración
├── ServidorHTTP.csproj     # Configuración del proyecto .NET
├── ServidorHTTP.sln        # Solución de Visual Studio
├── config.json             # Puerto y carpeta pública
├── logs/                   # Logs diarios de solicitudes
└── wwwroot/                # Archivos públicos del sitio web
    ├── 404.html
    ├── imagen.png
    ├── index.html
    ├── script.js
    ├── style.css
    └── test.html
```

## Configuración

La configuración se encuentra en `config.json`:

```json
{
    "Port": 8000,
    "RootDirectory": "wwwroot"
}
```

- `Port`: puerto TCP en el que escucha el servidor.
- `RootDirectory`: carpeta desde la que se sirven los archivos estáticos.

Con esta configuración, el servidor queda disponible en:

```text
http://localhost:8000
```

## Ejecutar el servidor

Abrí PowerShell en la carpeta raíz del proyecto:

```powershell
cd C:\Users\juanj\Desktop\servidorHTTP
```

### Restaurar dependencias

El proyecto no utiliza paquetes externos, pero se puede ejecutar:

```powershell
dotnet restore
```

### Compilar

```powershell
dotnet build
```

### Ejecutar

```powershell
dotnet run
```

La consola debería mostrar:

```text
Servidor escuchando en puerto 8000... http://localhost:8000
```

Después, abrí en el navegador:

```text
http://localhost:8000
```

Para detener el servidor, presioná `Ctrl + C` en la terminal donde está ejecutándose.

También se puede ejecutar la versión ya compilada:

```powershell
dotnet run --no-build
```

## Cómo funciona

El flujo general es:

```text
Ejecutar el programa
        ↓
Program.cs lee config.json
        ↓
Se crea HttpServer
        ↓
HttpServer abre el puerto configurado
        ↓
El navegador envía una solicitud HTTP
        ↓
HttpRequest interpreta la solicitud
        ↓
HttpServer busca el archivo dentro de wwwroot
        ↓
HttpResponse construye la respuesta
        ↓
El navegador recibe el contenido
        ↓
La solicitud se registra en logs/
```

### `Program.cs`

Es el punto de entrada. Lee `config.json`, convierte la configuración a un objeto `ServerConfig`, crea el servidor y llama a `StartAsync`.

### `HttpServer.cs`

Se ocupa de:

1. Crear el socket TCP.
2. Asociarlo al puerto configurado.
3. Escuchar conexiones entrantes.
4. Aceptar clientes.
5. Leer e interpretar sus solicitudes.
6. Buscar archivos dentro de `wwwroot`.
7. Enviar respuestas 200, 302, 403 o 404.
8. Registrar las solicitudes.

La ruta `/` se transforma en `/index.html`:

```text
/ → wwwroot/index.html
/test.html → wwwroot/test.html
/style.css → wwwroot/style.css
/imagen.png → wwwroot/imagen.png
```

### `HttpRequest.cs`

Interpreta la primera línea de la solicitud, por ejemplo:

```text
GET /test.html?usuario=juan HTTP/1.1
```

Y extrae:

```text
Method: GET
Url: /test.html
QueryParams: usuario=juan
```

Para las solicitudes POST también obtiene el cuerpo de la petición.

### `HttpResponse.cs`

Construye respuestas HTTP con:

- Línea de estado.
- `Content-Type`.
- `Content-Length`.
- `Content-Encoding: gzip`, cuando corresponde.
- Cuerpo de la respuesta.

Algunos tipos de contenido reconocidos son:

```text
.html → text/html
.css  → text/css
.js   → application/javascript
.json → application/json
.png  → image/png
.jpg  → image/jpeg
.svg  → image/svg+xml
```

## Pruebas disponibles

La página principal funciona como un panel de pruebas.

### GET a otra página

Abrí:

```text
http://localhost:8000/test.html
```

Esto verifica que el servidor pueda entregar otra página estática.

### Query parameters

El formulario de la página principal genera una URL similar a:

```text
http://localhost:8000/?param1=user_id&param2=12345
```

Los parámetros se muestran en el log de la solicitud.

### Solicitar archivos estáticos

También podés probar directamente:

```text
http://localhost:8000/style.css
http://localhost:8000/script.js
http://localhost:8000/imagen.png
```

### POST

El formulario POST envía datos similares a:

```text
nombre=Juan&mensaje=Hola
```

El servidor:

1. Lee el cuerpo.
2. Lo muestra en la consola.
3. Lo registra en el log.
4. Responde con `302 Found`.
5. Redirecciona al recurso `/`.

También se puede probar con `curl`:

```powershell
curl.exe -i -X POST -d "nombre=Juan&mensaje=Hola" http://localhost:8000/
```

### Error 404

Solicitá una ruta inexistente:

```text
http://localhost:8000/no-existe.html
```

El servidor responde:

```text
HTTP/1.1 404 Not Found
```

y entrega `wwwroot/404.html`.

## Logs

Los logs se guardan en la carpeta `logs` usando un archivo por día:

```text
logs/log_YYYY-MM-DD.txt
```

Cada solicitud registra información similar a:

```text
Fecha:       2025-11-11 00:22:25
IP:          127.0.0.1
Método:      GET
Recurso:     /
Query:       -
Body:        -
Servido:     wwwroot/index.html
Status:      200
------------------------------
```

Se registra:

- Fecha y hora.
- Dirección IP del cliente.
- Método HTTP.
- Recurso solicitado.
- Query string.
- Cuerpo del POST.
- Archivo servido.
- Código de estado HTTP.

## Cómo crear una aplicación similar desde cero

Si hubiera que rehacer el proyecto, conviene avanzar en etapas pequeñas y comprobar cada una antes de continuar.

### 1. Crear el proyecto

```powershell
mkdir servidorHTTP
cd servidorHTTP
dotnet new console --framework net9.0
code .
```

### 2. Crear un servidor TCP mínimo

Crear una clase `HttpServer` que:

1. Reciba un puerto.
2. Cree un `Socket`.
3. Lo asocie a una dirección y puerto.
4. Empiece a escuchar.
5. Acepte conexiones con `AcceptAsync`.

La primera meta es ver:

```text
Servidor escuchando en puerto 8000
```

### 3. Aceptar una conexión

Leer los bytes que envía el navegador:

```csharp
Socket clientSocket = await listener.AcceptAsync();
byte[] buffer = new byte[8192];
int received = await clientSocket.ReceiveAsync(buffer);
string requestText = Encoding.UTF8.GetString(buffer, 0, received);
```

Al imprimir el contenido se puede observar una solicitud como:

```text
GET / HTTP/1.1
Host: localhost:8000
...
```

### 4. Crear el parser `HttpRequest`

Separar la interpretación de la solicitud en una clase independiente. Primero conviene soportar solamente:

```text
GET /ruta HTTP/1.1
```

Después se pueden agregar query strings, headers, body y POST.

### 5. Crear una respuesta HTTP fija

Antes de servir archivos, enviar una respuesta simple:

```text
HTTP/1.1 200 OK\r\n
Content-Type: text/plain\r\n
Content-Length: 16\r\n
\r\n
Hola desde C#
```

Una respuesta HTTP tiene:

1. Línea de estado.
2. Headers.
3. Línea vacía.
4. Cuerpo.

### 6. Servir archivos estáticos

Crear la carpeta `wwwroot` y colocar dentro:

```text
wwwroot/index.html
wwwroot/style.css
wwwroot/script.js
wwwroot/imagen.png
```

Después:

1. Convertir la URL en una ruta.
2. Convertir `/` en `index.html`.
3. Verificar que el archivo exista.
4. Leerlo como bytes.
5. Detectar su `Content-Type`.
6. Enviarlo al cliente.

### 7. Agregar respuestas 404

Cuando no existe el archivo solicitado:

```text
HTTP/1.1 404 Not Found
```

Se puede servir una página personalizada desde `wwwroot/404.html`.

### 8. Proteger las rutas

Hay que impedir que una solicitud pueda salir de `wwwroot`, por ejemplo:

```text
/../../archivo-secreto.txt
```

Para esto hay que calcular las rutas absolutas y comprobar que la ruta final permanezca dentro de la carpeta pública.

### 9. Agregar POST

Una implementación más completa debe:

1. Detectar el método POST.
2. Leer los headers.
3. Leer `Content-Length`.
4. Esperar hasta recibir el body completo.
5. Interpretar los datos.
6. Enviar una respuesta apropiada.

### 10. Agregar logs

Una vez que GET, archivos y errores funcionan, agregar el registro de:

- IP.
- Fecha.
- Método.
- Ruta.
- Query.
- Estado.
- Archivo servido.
- Tiempo de respuesta.

### 11. Agregar mejoras

Como último paso se pueden incorporar compresión, más tipos MIME, manejo de errores, cierre ordenado y pruebas automáticas.

## Orden recomendado de desarrollo

```text
1. Crear el proyecto Console .NET
2. Crear un servidor TCP
3. Escuchar en el puerto 8000
4. Aceptar conexiones
5. Leer la solicitud del navegador
6. Crear HttpRequest
7. Enviar una respuesta fija 200 OK
8. Crear wwwroot
9. Servir index.html
10. Detectar Content-Type
11. Agregar CSS, JavaScript e imágenes
12. Agregar respuestas 404
13. Validar rutas fuera de wwwroot
14. Agregar query parameters
15. Agregar POST
16. Agregar redirecciones
17. Agregar compresión
18. Agregar logs
19. Agregar manejo de errores y cierre ordenado
20. Agregar pruebas automáticas
```

El circuito mínimo que hay que lograr primero es:

```text
navegador → socket → leer GET → responder HTML
```

## Mejoras pendientes

El proyecto funciona como servidor HTTP educativo, pero para hacerlo más robusto convendría:

- Leer solicitudes completas, no solamente una llamada a `ReceiveAsync`.
- Procesar correctamente `Content-Length`.
- Soportar más métodos HTTP.
- Validar los métodos no soportados.
- Manejar excepciones de cada cliente sin detener todo el servidor.
- Permitir un apagado ordenado con `Ctrl + C`.
- Comprobar `Accept-Encoding` antes de enviar GZIP.
- Agregar `Vary: Accept-Encoding`.
- Controlar los datos sensibles almacenados en los logs.
- Validar las rutas con una comparación robusta.
- Separar configuración, enrutamiento, archivos estáticos y logging.
- Agregar pruebas para GET, POST, 404, rutas inválidas y query strings.

## Resumen rápido

Para usar el proyecto actual:

```powershell
cd C:\Users\juanj\Desktop\servidorHTTP
dotnet build
dotnet run
```

Después abrí:

```text
http://localhost:8000
```

Para detenerlo:

```text
Ctrl + C
```
