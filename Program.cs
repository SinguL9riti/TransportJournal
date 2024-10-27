using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TransportJournal.Data;
using TransportJournal.Services;
using Route = TransportJournal.Models.Route;

namespace TransportJournal
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ��������� ������������ ��� ������ � ������� JSON
            builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("secrets.json", optional: false, reloadOnChange: true)
                .AddUserSecrets<Program>()  // ��� ������������� �������� ��������� ���������������� ��������
                .AddEnvironmentVariables();  // �������� ���������� ���������

            // �������� ������ ����������� MsSqlConnection
            string? connectionString = builder.Configuration.GetConnectionString("MsSqlConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("�������� ����������� MsSqlConnection �� ���������.");
            }

            // ����������� ��������� ���� ������
            builder.Services.AddDbContext<TransportDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            // ����������� ����������� � ������
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<CachedDataService>();

            // ����������� ������
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            app.UseSession();

            // ��������� ��������
            app.MapGet("/", async context =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                string strResponse = @"
                    <html>
                        <head>
                            <title>������� ��������</title>
                            <style>
                                body { font-family: Arial, sans-serif; background-color: #f0f0f0; text-align: center; }
                                a { display: block; padding: 10px; margin: 10px; background-color: #007BFF; color: white; text-decoration: none; width: 200px; text-align: center; border-radius: 5px; transition: background-color 0.3s; }
                                a:hover { background-color: #0056b3; }
                            </style>
                        </head>
                        <body>
                            <h1>������� ��������</h1>
                            <a href='/table'>�������</a>
                            <a href='/info'>����������</a>
                            <a href='/searchform1'>SearchForm1</a>
                            <a href='/searchform2'>SearchForm2</a>
                        </body>
                    </html>";
                await context.Response.WriteAsync(strResponse);
            });

            // �������� ����������
            app.MapGet("/info", async context =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                string strResponse = @"
                    <html>
                        <head>
                            <title>����������</title>
                            <style>
                                body { font-family: Arial, sans-serif; background-color: #f0f0f0; text-align: center; }
                            </style>
                        </head>
                        <body>
                            <h1>����������</h1>
                            <p>������: " + context.Request.Host + @"</p>
                            <p>����: " + context.Request.Path + @"</p>
                            <p>��������: " + context.Request.Protocol + @"</p>
                            <a href='/'>�� �������</a>
                        </body>
                    </html>";
                await context.Response.WriteAsync(strResponse);
            });

            // �������� ������
            app.MapGet("/table", async context =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                string strResponse = @"
                    <html>
                        <head>
                            <title>�������</title>
                            <style>
                                body { font-family: Arial, sans-serif; background-color: #f0f0f0; text-align: center; }
                                a { display: block; margin: 10px; padding: 10px; background-color: #007BFF; color: white; text-decoration: none; border-radius: 5px; }
                                a:hover { background-color: #0056b3; }
                            </style>
                        </head>
                        <body>
                            <h1>��������� �������</h1>
                            <a href='/table/Personnel'>Personnel</a>
                            <a href='/table/Route'>Route</a>
                            <a href='/table/Schedule'>Schedule</a>
                            <a href='/table/Stop'>Stop</a>
                        </body>
                    </html>";
                await context.Response.WriteAsync(strResponse);
            });

            app.Use(async (context, next) =>
            {
                // ���������, ���������� �� ������ � '/table', � ���������� ��� �������
                if (context.Request.Path.StartsWithSegments("/table", out var remainingPath) && remainingPath.HasValue && remainingPath.Value.StartsWith("/"))
                {
                    context.Response.ContentType = "text/html; charset=utf-8"; // ��������� Content-Type
                    var tableName = remainingPath.Value.Substring(1); // ������� ��������� ����

                    var cachedService = context.RequestServices.GetService<CachedDataService>();

                    // ������ ������ �������
                    if (tableName == "Personnel")
                    {
                        var list = cachedService.GetPersonnel();
                        await RenderTable(context, list);
                    }
                    else if (tableName == "Route")
                    {
                        var list = cachedService.GetRoutes();
                        await RenderTable(context, list);
                    }
                    else if (tableName == "Schedule")
                    {
                        var list = cachedService.GetSchedules();
                        await RenderTable(context, list);
                    }
                    else if (tableName == "Stop")
                    {
                        var list = cachedService.GetStops();
                        await RenderTable(context, list);
                    }
                    else
                    {
                        // ���� ������� �� �������, ���������� 404
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("������� �� �������");
                    }

                    return; // ��������� ��������� �������
                }
                await next.Invoke();
            });

            async Task RenderTable<T>(HttpContext context, IEnumerable<T> data)
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                var html = @"
                <html>
                    <head>
                        <style>
                            table { width: 80%; margin: 20px auto; border-collapse: collapse; }
                            th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                            th { background-color: #007BFF; color: white; }
                            tr:nth-child(even) { background-color: #f2f2f2; }
                            tr:hover { background-color: #ddd; }
                            h1 { text-align: center; }
                        </style>
                    </head>
                    <body>
                        <h1>������� ������</h1>
                        <table>
                            <tr>";

                var type = typeof(T);

                // ��������� ���������� ������� �� ������ ��������� Display
                foreach (var prop in type.GetProperties())
                {
                    // ���������� ��������, ������� �������� ����������� ��� ���������
                    if (typeof(IEnumerable<object>).IsAssignableFrom(prop.PropertyType) || !IsSimpleType(prop.PropertyType))
                    {
                        continue;
                    }

                    // �������� ������� Display
                    var displayAttribute = prop.GetCustomAttributes(typeof(DisplayAttribute), false)
                        .FirstOrDefault() as DisplayAttribute;

                    // ���� ������� Display �� ������, ���������� ��� ��������
                    var displayName = displayAttribute?.Name ?? prop.Name;

                    html += $"<th>{displayName}</th>";
                }
                html += "</tr>";

                foreach (var item in data)
                {
                    html += "<tr>";
                    foreach (var prop in type.GetProperties())
                    {
                        // ���������� ��������, ������� �������� ����������� ��� ���������
                        if (typeof(IEnumerable<object>).IsAssignableFrom(prop.PropertyType) || !IsSimpleType(prop.PropertyType))
                        {
                            continue;
                        }

                        var value = prop.GetValue(item);

                        if (value is DateTime dateValue)
                        {
                            html += $"<td>{dateValue.ToString("dd.MM.yyyy")}</td>";
                        }
                        else if (value is TimeOnly timeValue)
                        {
                            html += $"<td>{timeValue}</td>";
                        }
                        else
                        {
                            html += $"<td>{value}</td>";
                        }
                    }
                    html += "</tr>";
                }

                html += "</table></body></html>";
                await context.Response.WriteAsync(html);
            }

            bool IsSimpleType(Type type)
            {
                // ����������� ���� � ����, ������� ��������� �������� (string, DateTime � �.�.)
                return type.IsPrimitive ||
                       type.IsValueType ||
                       type == typeof(string) ||
                       type == typeof(DateTime) ||
                       type == typeof(decimal);
            }

            app.Map("/searchform1", async (HttpContext context) =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                var dbContext = context.RequestServices.GetService<TransportDbContext>();

                if (context.Request.Method == "GET")
                {
                    // �������� ������ �� ����, ���� ��� ����������
                    var routeName = context.Request.Cookies["RouteName"] ?? string.Empty;
                    var transportType = context.Request.Cookies["TransportType"] ?? string.Empty;
                    var isExpressValue = context.Request.Cookies["IsExpress"] ?? string.Empty;

                    // �������� ������ ����� ���������� �� ��
                    var transportTypes = await dbContext.Routes
                                                         .Select(r => r.TransportType)
                                                         .Distinct()
                                                         .ToListAsync();

                    var html = @"
<html>
<head>
    <title>Search Routes</title>
    <style>
        body { font-family: Arial, sans-serif; background-color: #f4f4f4; }
        form { background: #fff; padding: 20px; border-radius: 5px; box-shadow: 0 0 10px rgba(0, 0, 0, 0.1); }
        label { display: block; margin: 10px 0 5px; }
        input[type='text'], select { width: 100%; padding: 8px; margin-bottom: 10px; border: 1px solid #ccc; border-radius: 4px; }
        button { background-color: #28a745; color: white; border: none; padding: 10px; border-radius: 5px; cursor: pointer; }
        button:hover { background-color: #218838; }
        table { width: 100%; margin-top: 20px; border-collapse: collapse; }
        th, td { padding: 10px; border: 1px solid #ccc; text-align: left; }
        th { background-color: #f2f2f2; }
    </style>
</head>
<body>
    <form method='post'>
        <label for='RouteName'>�������� ��������:</label>
        <input type='text' id='RouteName' name='RouteName' value='�������'>

        <label for='TransportType'>��� ����������:</label>
        <select id='TransportType' name='TransportType'>
            <option value=''>All</option>";

                    foreach (var transportTypeItem in transportTypes)
                    {
                        html += $"<option value='{transportTypeItem}' " + (transportType == transportTypeItem ? "selected" : "") + $">{transportTypeItem}</option>";
                    }

                    html += @"</select><label for='IsExpress'>��������:</label><select id='IsExpress' name='IsExpress'><option value=''>All</option><option value='true'" + (isExpressValue == "true" ? " selected" : "") + ">Yes</option><option value='false'" + (isExpressValue == "false" ? " selected" : "") + ">No</option></select><button type='submit'>Search</button></form></body></html>";


                    await context.Response.WriteAsync(html);
                }
                else if (context.Request.Method == "POST")
                {
                    var formData = await context.Request.ReadFormAsync();
                    var routeName = formData["RouteName"].ToString();
                    var transportType = formData["TransportType"].ToString();
                    var isExpressValue = formData["IsExpress"].ToString();

                    // ��������� ������ � ����
                    context.Response.Cookies.Append("RouteName", routeName);
                    context.Response.Cookies.Append("TransportType", transportType);
                    context.Response.Cookies.Append("IsExpress", isExpressValue);

                    var query = dbContext.Routes.AsQueryable();

                    // ��������� �������
                    if (!string.IsNullOrEmpty(routeName))
                    {
                        query = query.Where(r => r.Name.Contains(routeName));
                    }
                    if (!string.IsNullOrEmpty(transportType))
                    {
                        query = query.Where(r => r.TransportType == transportType);
                    }

                    // ��������� �������� IsExpress
                    if (bool.TryParse(isExpressValue, out var isExpress))
                    {
                        query = query.Where(r => r.IsExpress == isExpress);
                    }

                    // ��������� ������ � ���������
                    var routes = await query.ToListAsync();

                    var resultHtml = @"
<html>
<head>
    <title>Route Search Results</title>
    <style>
        body { font-family: Arial, sans-serif; background-color: #f4f4f4; }
        table { width: 100%; margin-top: 20px; border-collapse: collapse; }
        th, td { padding: 10px; border: 1px solid #ccc; text-align: left; }
        th { background-color: #f2f2f2; }
        .no-results { margin-top: 20px; color: red; }
    </style>
</head>
<body>
    <h1>���������� ������ �������� (�����)</h1>
    <table>
        <tr><th>�������� ��������</th><th>��� ����������</th><th>��������</th></tr>";

                    // ���������� ������ ������
                    foreach (var route in routes)
                    {
                        resultHtml += $"<tr><td>{route.Name}</td><td>{route.TransportType}</td><td>{route.IsExpress}</td></tr>";
                    }

                    resultHtml += @"
    </table>";

                    if (routes.Count == 0)
                    {
                        resultHtml += "<p class='no-results'>No routes found matching the search criteria.</p>";
                    }

                    resultHtml += "</body></html>";

                    await context.Response.WriteAsync(resultHtml);
                }
            });




            app.Map("/searchform2", async (HttpContext context) =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                var dbContext = context.RequestServices.GetService<TransportDbContext>();

                if (context.Request.Method == "GET")
                {
                    // �������� ������ �� ������, ���� ��� ����������
                    var routeName = context.Session.GetString("RouteName") ?? string.Empty;
                    var transportType = context.Session.GetString("TransportType") ?? string.Empty;
                    var isExpressValue = context.Session.GetString("IsExpress") ?? string.Empty;

                    // �������� ������ ����� ���������� �� ��
                    var transportTypes = await dbContext.Routes
                                                         .Select(r => r.TransportType)
                                                         .Distinct()
                                                         .ToListAsync();

                    var html = @"
<html>
<head>
    <title>Search Routes</title>
    <style>
        body { font-family: Arial, sans-serif; background-color: #f4f4f4; }
        form { background: #fff; padding: 20px; border-radius: 5px; box-shadow: 0 0 10px rgba(0, 0, 0, 0.1); }
        label { display: block; margin: 10px 0 5px; }
        input[type='text'], select { width: 100%; padding: 8px; margin-bottom: 10px; border: 1px solid #ccc; border-radius: 4px; }
        button { background-color: #28a745; color: white; border: none; padding: 10px; border-radius: 5px; cursor: pointer; }
        button:hover { background-color: #218838; }
    </style>
</head>
<body>
    <form method='post'>
        <label for='RouteName'>�������� ��������:</label>
        <input type='text' id='RouteName' name='RouteName' value='�������'>

        <label for='TransportType'>��� ����������:</label>
        <select id='TransportType' name='TransportType'>
            <option value=''>All</option>";

                    foreach (var transportTypeItem in transportTypes)
                    {
                        html += $"<option value='{transportTypeItem}' " + (transportType == transportTypeItem ? "selected" : "") + $">{transportTypeItem}</option>";
                    }

                    html += "</select><label for='��������'>Is Express:</label><select id='IsExpress' name='IsExpress'><option value=''>All</option><option value='true'" + (isExpressValue == "true" ? " selected" : "") + ">Yes</option><option value='false'" + (isExpressValue == "false" ? " selected" : "") + ">No</option></select><button type='submit'>Search</button></form>";


                    await context.Response.WriteAsync(html);
                }
                else if (context.Request.Method == "POST")
                {
                    var formData = await context.Request.ReadFormAsync();
                    var routeName = formData["RouteName"].ToString();
                    var transportType = formData["TransportType"].ToString();
                    var isExpressValue = formData["IsExpress"].ToString();

                    // ��������� ������ � ������
                    context.Session.SetString("RouteName", routeName);
                    context.Session.SetString("TransportType", transportType);
                    context.Session.SetString("IsExpress", isExpressValue);

                    var query = dbContext.Routes.AsQueryable();

                    // ��������� �������
                    if (!string.IsNullOrEmpty(routeName))
                    {
                        query = query.Where(r => r.Name.Contains(routeName));
                    }
                    if (!string.IsNullOrEmpty(transportType))
                    {
                        query = query.Where(r => r.TransportType == transportType);
                    }

                    // ��������� �������� IsExpress
                    if (bool.TryParse(isExpressValue, out var isExpress))
                    {
                        query = query.Where(r => r.IsExpress == isExpress);
                    }

                    // ��������� ������ � ���������
                    var routes = await query.ToListAsync();

                    var resultHtml = @"
<h1>���������� ������ �������� (�����)</h1>
<table border='1' style='border-collapse:collapse'>
    <tr><th>�������� ��������</th><th>��� ����������</th><th>��������</th></tr>";

                    // ���������� ������ ������
                    foreach (var route in routes)
                    {
                        resultHtml += $"<tr><td>{route.Name}</td><td>{route.TransportType}</td><td>{route.IsExpress}</td></tr>";
                    }

                    resultHtml += @"</table>";

                    if (routes.Count == 0)
                    {
                        resultHtml += "<p>No routes found matching the search criteria.</p>";
                    }

                    await context.Response.WriteAsync(resultHtml);
                }
            });






            await app.RunAsync();
        }
    }

    
}

