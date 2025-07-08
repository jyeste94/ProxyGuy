# ProxyGuy.WinForms

ProxyGuy es una aplicación de escritorio basada en Windows Forms que actúa como servidor proxy para inspeccionar el tráfico HTTP y HTTPS. Utiliza [Titanium.Web.Proxy](https://github.com/titaniumsoft/Titanium-Web-Proxy) para interceptar las peticiones y permite activar el proxy del sistema de forma temporal.

## Características principales

- Inicia un servidor proxy en `localhost:9090` y muestra en una tabla todas las peticiones y respuestas interceptadas.
- Guarda las cabeceras de la petición y respuesta, así como los cuerpos cuando están disponibles.
- Permite filtrar por dominio y habilitar o deshabilitar la configuración de proxy de Windows desde la interfaz.
- Instala un certificado raíz auto generado para poder interceptar conexiones HTTPS (se realiza automáticamente al iniciar la aplicación).
- Incluye un botón para configurar las rutas de `php.ini` que se actualizarán al activar el proxy; si se modifican se restauran automáticamente al desactivarlo.

## Estructura del proyecto

El punto de entrada se encuentra en `Program.cs`, donde se abre el formulario principal:

```csharp
Application.Run(new ProxyForm());
```

El servicio de proxy se inicializa en `ProxyService.cs` creando un `ProxyServer` de Titanium.Web.Proxy:

```csharp
var explicitEndPoint = new ExplicitProxyEndPoint(System.Net.IPAddress.Any, 9090, true);
_proxyServer.AddEndPoint(explicitEndPoint);
_proxyServer.Start();
```

Las peticiones capturadas se almacenan en el tipo `RequestInfo`:

```csharp
public class RequestInfo
{
    public DateTime Time { get; set; } = DateTime.Now;
    public string Method { get; set; }
    public string Url { get; set; }
    public string Domain { get; set; }
    public int StatusCode { get; set; }
    public List<KeyValuePair<string,string>> RequestHeaders { get; set; } = new();
    public List<KeyValuePair<string,string>> ResponseHeaders { get; set; } = new();
    public string RequestBody { get; set; }
    public string ResponseBody { get; set; }
}
```

## Compilación del ejecutable

El proyecto está orientado a **.NET 6 para Windows** (`net6.0-windows`) tal y como se indica en `ProxyGuy.WinForms.csproj`. Para generar el ejecutable (`.exe`) puedes usar la CLI de .NET:

```bash
# Restaurar dependencias
dotnet restore

# Publicar en modo Release para Windows 64 bits
# Esto creará `ProxyGuy.WinForms.exe` dentro de `bin/Release/net6.0-windows/win-x64/publish`
dotnet publish -c Release -r win-x64 --self-contained false
```

El ejecutable generado se puede distribuir y ejecutar en cualquier equipo con .NET 6 instalado. Si se desea un paquete auto contenido (que incluya el runtime de .NET), se puede omitir `--self-contained false`.

## Uso

1. Ejecuta `ProxyGuy.WinForms.exe`.
2. Pulsa **Activar Proxy** para que Windows redirija el tráfico a `localhost:9090`.
3. Navega normalmente y observa cómo las peticiones aparecen en la interfaz.
4. Pulsa **Desactivar Proxy** antes de cerrar la aplicación para restaurar la configuración original.

## Requisitos

- Windows 10 o superior.
- .NET 6 SDK para compilar desde el código fuente.

