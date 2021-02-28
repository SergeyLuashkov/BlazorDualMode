# Создание проекта
Создаем проект blazor WebAssembly с Аутентификацией и Asp.net хостингом

# Добавляем prerender в blazor WebAssembly
## Основанно на инструкции [microsoft](https://docs.microsoft.com/ru-ru/aspnet/core/blazor/components/prerendering-and-integration?view=aspnetcore-5.0&pivots=webassembly)
1. Добавьте файл Pages/_Host.cshtml (Смотри подробнее в инструкции [microsoft](https://docs.microsoft.com/ru-ru/aspnet/core/blazor/security/webassembly/additional-scenarios?view=aspnetcore-5.0#support-prerendering-with-authentication))
- Добавьте @page "_Host" в начало файла.
- Замените тег <div id="app">Loading...</div> следующим:
```razor
<div id="app">
    @if (!HttpContext.Request.Path.StartsWithSegments("/authentication"))
    {
        <component type="typeof({CLIENT APP ASSEMBLY NAME}.App)" 
            render-mode="WebAssemblyPrerendered" />
    }
    else
    {
        <text>Loading...</text>
    }
</div>
```
2. Удалите стандартный статический файл wwwroot/index.html из проекта клиента Blazor WebAssembly.
3. Удалите следующую строку в Program.Main в клиентском проекте:
```C#
builder.RootComponents.Add<App>("#app");
```
4. В Startup.Configure (Startup.cs) серверного проекта:
- Измените значение отката со страницы index.html (endpoints.MapFallbackToFile("index.html");) на страницу _Host.cshtml.
```C#
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseMigrationsEndPoint();
        app.UseWebAssemblyDebugging();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseIdentityServer();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapRazorPages();
        endpoints.MapControllers();
        endpoints.MapFallbackToPage("/_Host");
    });
}
```
5. Добавьте в Startup.ConfigureServices (Startup.cs) серверного проекта (основанно на инструкции [Microsoft](https://docs.microsoft.com/ru-ru/aspnet/core/blazor/security/webassembly/additional-scenarios?view=aspnetcore-5.0#support-prerendering-with-authentication))
```C#
services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
services.AddScoped<SignOutSessionStateManager>();
```

# Добавляем Blazor Server side
1. Добавьте в Startup.ConfigureServices (Startup.cs) серверного проекта:
```C#
services.AddServerSideBlazor();
```
2. Добавьте в Startup.ConfigureServices (Startup.cs) серверного проекта:
```C#
endpoints.MapBlazorHub()
    .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "Identity.Application" })
    .AllowAnonymous();
```
3. Измените в Startup.ConfigureServices (Startup.cs) серверного проекта:
```C#
endpoints.MapRazorPages()
```
на
```C#
endpoints.MapRazorPages()
    .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "Identity.Application" })
    .AllowAnonymous();
```
4. Добавьте в /Areas/Identity/Pages/Account страницу Logout.cshtml со следующим содержимым:
```razor
@page
@using Microsoft.AspNetCore.Identity
@using BlazorDualMode.Server.Models
@attribute [IgnoreAntiforgeryToken]
@inject SignInManager<ApplicationUser> SignInManager
@functions {
    public async Task<IActionResult> OnPost()
    {
        if (SignInManager.IsSignedIn(User))
        {
            await SignInManager.SignOutAsync();
        }

        return Redirect("~/");
    }
}
```
5. Замените содержимое /Shared/LoginDisplay.razor клентского проекта на следующее:
```razor
<AuthorizeView>
    <Authorized>
        <a href="Identity/Account/Manage">Hello, @context.User.Identity.Name!</a>
        <form method="post" action="Identity/Account/LogOut">
            <button type="submit" class="nav-link btn btn-link">Log out</button>
        </form>
    </Authorized>
    <NotAuthorized>
        <a href="Identity/Account/Register">Register</a>
        <a href="Identity/Account/Login">Log in</a>
    </NotAuthorized>
</AuthorizeView>
```
6. Замените содержимое /Shared/RedirectToLogin.razor в серверном проекте
```razor
@inject NavigationManager Navigation
@code{
    protected override void OnInitialized()
    {
        Navigation.NavigateTo("/Identity/Account/Login", true);
    }
}
```
# Чиним WeatherForecast
1. Добавляем интерфейс IWeatherForecastRepository в Shared проект
```С#
public interface IWeatherForecastRepository
{
    Task<WeatherForecast[]> GetWeatherForecast();
}
```
2. Добавляем сервис WeatherForecastService в серверный проект
```C#
public class WeatherForecastService: IWeatherForecastRepository
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };
    public Task<WeatherForecast[]> GetWeatherForecast()
    {
        var rng = new Random();
        return Task.FromResult(Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = rng.Next(-20, 55),
            Summary = Summaries[rng.Next(Summaries.Length)]
        })
        .ToArray());
    }
}
```
3. Добавляем сервис IWeatherForecastRepository в Startup.ConfigureServices (Startup.cs) серверного проекта:
```C#
services.AddScoped<IWeatherForecastRepository, WeatherForecastService>()
```
4. Правим WeatherForecastController
```C#
[Authorize]
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IWeatherForecastRepository weatherForecastRepository;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IWeatherForecastRepository weatherForecastRepository)
    {
        _logger = logger;
        this.weatherForecastRepository = weatherForecastRepository;
    }

    [HttpGet]
    public async Task<WeatherForecast[]> Get()
    {
        return await weatherForecastRepository.GetWeatherForecast();
    }
}
```
5. Добавляем WeatherForecastRepository в клиентский проект
```C#
public class WeatherForecastRepository : IWeatherForecastRepository
{
    private readonly HttpClient httpClient;

    public WeatherForecastRepository(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }
    public async Task<WeatherForecast[]> GetWeatherForecast()
    {
        return await httpClient.GetFromJsonAsync<WeatherForecast[]>("WeatherForecast");
    }
}
```
6. Добавляем сервис IWeatherForecastRepository в Program.Main(Program.cs) Клиентского проекта:
```C#
builder.Services.AddScoped<IWeatherForecastRepository, WeatherForecastRepository>();
```
7. Изменяем /Pages/FetchData.razor в клентском проекте
```razor
@page "/fetchdata"
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.WebAssembly.Authentication
@using BlazorDualMode.Shared
@attribute [Authorize]
@inject IWeatherForecastRepository weatherForecastRepository

<h1>Weather forecast</h1>

<p>This component demonstrates fetching data from the server.</p>

@if (forecasts == null)
{
    <p><em>Loading...</em></p>
}
else
{
    <table class="table">
        <thead>
            <tr>
                <th>Date</th>
                <th>Temp. (C)</th>
                <th>Temp. (F)</th>
                <th>Summary</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var forecast in forecasts)
            {
                <tr>
                    <td>@forecast.Date.ToShortDateString()</td>
                    <td>@forecast.TemperatureC</td>
                    <td>@forecast.TemperatureF</td>
                    <td>@forecast.Summary</td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private WeatherForecast[] forecasts;

    protected override async Task OnInitializedAsync()
    {
        forecasts = await weatherForecastRepository.GetWeatherForecast();
    }
}
```
# Добавляем переключатель между архитектурами
1. Добавляем контроллер BlazorModeController.cs в /Contollers в серверном проекте
```C#
[Route("_blazorMode")]
public class BlazorModeController:ControllerBase
{
    public static string CookieName { get; set; } = "_ssb_";

    [HttpGet("{IsServerSideBlazor}")]
    public IActionResult Switch(bool isServerSideBlazor, string redirectTo = "")
    {
        if(isServerSideBlazor != IsServerSideBlazor(HttpContext))
        {
            var response = HttpContext.Response;
            response.Cookies.Append(CookieName, Convert.ToInt32(isServerSideBlazor).ToString());
        }
        if (string.IsNullOrEmpty(redirectTo))
        {
            redirectTo = "~/";
        }
        return Redirect(redirectTo);
    }

    public static bool IsServerSideBlazor(HttpContext httpContext)
    {
        var cookies = httpContext.Request.Cookies;
        var isSsb = cookies.TryGetValue(CookieName, out var v) ? v : "";
        if(!int.TryParse(isSsb, out var isSsbInt))
        {
            return true;
        }
        return isSsbInt != 0;
    }
}
```
2. Изменяем _Host.cshtml 
```razor
@page "/"
@using BlazorDualMode.Client
@using BlazorDualMode.Server.Controllers 
@namespace BlazorDualMode.Server.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@{

    var isServerSideBlazor = BlazorModeController.IsServerSideBlazor(HttpContext);
    Layout = null;
}
<!DOCTYPE html>
<html>

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>BlazorDualMode</title>
    <base href="/" />
    <link href="css/bootstrap/bootstrap.min.css" rel="stylesheet" />
    <link href="css/app.css" rel="stylesheet" />
    <link href="BlazorDualMode.Client.styles.css" rel="stylesheet" />
</head>

<body>
    <div id="app">
        @if (!isServerSideBlazor)
        {
            <component type="typeof(App)" render-mode="ServerPrerendered" />
        }
        else
        {
            @if (!HttpContext.Request.Path.StartsWithSegments("/authentication"))
            {
                <component type="typeof(App)" render-mode="WebAssemblyPrerendered" />
            }
            else
            {
                <text>Loading...</text>
            }
        }
    </div>
    <div id="blazor-error-ui">
        <environment include="Staging,Production">
            An error has occurred. This application may no longer respond until reloaded.
        </environment>
        <environment include="Development">
            An unhandled exception has occurred. See browser dev tools for details.
        </environment>
        <a href="" class="reload">Reload</a>
        <a class="dismiss">🗙</a>
    </div>
    @if (!isServerSideBlazor)
    {
        <script src="_framework/blazor.server.js"></script>
    }
    else
    {
        <script src="_content/Microsoft.AspNetCore.Components.WebAssembly.Authentication/AuthenticationService.js"></script>
        <script src="_framework/blazor.webassembly.js"></script>
    }
</body>

</html>
```
3. Добавляем BlazorMode.razor в /Shared клиентского проекта со следующим содержимым
```razor
@using System.Web;
@using Microsoft.AspNetCore.Components;
@inject NavigationManager navigationManager

<select class="form-select" @onchange="ChangeMode">
    <option>--Select--</option>
    <option value="true">WebAssembly</option>
    <option value="false">ServerSide</option>
</select>

@code {
    private void ChangeMode(ChangeEventArgs eventArgs)
    {
        var switchUrl = GetModeChangeUrl(bool.Parse(eventArgs.Value.ToString()), navigationManager.Uri);
        navigationManager.NavigateTo(switchUrl, true);
    }
    private string GetModeChangeUrl(bool isServerSideBlazor, string redirectTo = "/")
    {
        var isSsbString = isServerSideBlazor.ToString().ToLowerInvariant();
        return $"/_blazorMode/{isSsbString}?redirectTo={HttpUtility.UrlEncode(redirectTo)}";
    }
}
```
4. Изменяем /Shared/MainLayout.razor в клиентском проекте
```razor
@inherits LayoutComponentBase

<div class="page">
    <div class="sidebar">
        <NavMenu />
    </div>

    <div class="main">
        <div class="top-row px-4 auth">
            <BlazorMode />
            <LoginDisplay />
            <a href="https://docs.microsoft.com/aspnet/" target="_blank">About</a>
        </div>

        <div class="content px-4">
            @Body
        </div>
    </div>
</div>
```

# Заключение
Не уверен, в том, что это самый лучший вариант объединить Blazor Wasm с Server side в одном проекте, но по крайне мере он работает.