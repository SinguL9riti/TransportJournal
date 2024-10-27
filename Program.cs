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
                                a { display: block; padding: 10px; margin: 10px; background-color: #007BFF; color: white; text-decoration: none; width: 200px; text-align: center; border-radius: 5px; }
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
                                body { font-family: Arial, sans-serif; }
                                a { display: block; margin: 10px 0; }
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
                var html = "<table border='1' style='border-collapse:collapse'>";

                var type = typeof(T);

                // ��������� ���������� ������� �� ������ ��������� Display
                html += "<tr>";
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

                html += "</table>";
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

                    var html = "<form method='post'>";
                    html += "<label for='RouteName'>Route Name:</label><br/>";
                    html += $"<input type='text' id='RouteName' name='RouteName' value='{routeName}'><br/><br/>";

                    html += "<label for='TransportType'>Transport Type:</label><br/>";
                    html += "<select id='TransportType' name='TransportType'>";
                    html += "<option value=''>All</option>";  // �������� �� ��������� ��� 'All'
                    foreach (var transportTypeItem in transportTypes)
                    {
                        html += $"<option value='{transportTypeItem}' " + (transportType == transportTypeItem ? "selected" : "") + $">{transportTypeItem}</option>";
                    }
                    html += "</select><br/><br/>";

                    html += "<label for='IsExpress'>Is Express:</label><br/>";
                    html += "<select id='IsExpress' name='IsExpress'>";
                    html += "<option value=''>All</option>";  // �������� �� ��������� ��� 'All'
                    html += "<option value='true'" + (isExpressValue == "true" ? " selected" : "") + ">Yes</option>"; // �������� ��� ���������
                    html += "<option value='false'" + (isExpressValue == "false" ? " selected" : "") + ">No</option>"; // �������� ��� �����������
                    html += "</select><br/><br/>";

                    html += "<button type='submit'>Search</button>";
                    html += "</form>";

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

                    var html = "<h1>Route Search Results (Cockies)</h1>";
                    html += "<h3>Search Criteria:</h3>";
                    html += "<table border='1' style='border-collapse:collapse'>";
                    html += "<tr><th>Route Name</th><th>Transport Type</th><th>Is Express</th></tr>";

                    // ���������� ������ ������
                    foreach (var route in routes)
                    {
                        html += $"<tr><td>{route.Name}</td><td>{route.TransportType}</td><td>{route.IsExpress}</td></tr>";
                    }
                    html += "</table>";

                    if (routes.Count == 0)
                    {
                        html += "<p>No routes found matching the search criteria.</p>";
                    }

                    await context.Response.WriteAsync(html);
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

                    var html = "<form method='post'>";
                    html += "<label for='RouteName'>Route Name:</label><br/>";
                    html += $"<input type='text' id='RouteName' name='RouteName' value='{routeName}'><br/><br/>";

                    html += "<label for='TransportType'>Transport Type:</label><br/>";
                    html += "<select id='TransportType' name='TransportType'>";
                    html += "<option value=''>All</option>";  // �������� �� ��������� ��� 'All'
                    foreach (var transportTypeItem in transportTypes)
                    {
                        html += $"<option value='{transportTypeItem}' " + (transportType == transportTypeItem ? "selected" : "") + $">{transportTypeItem}</option>";
                    }
                    html += "</select><br/><br/>";

                    html += "<label for='IsExpress'>Is Express:</label><br/>";
                    html += "<select id='IsExpress' name='IsExpress'>";
                    html += "<option value=''>All</option>";  // �������� �� ��������� ��� 'All'
                    html += "<option value='true'" + (isExpressValue == "true" ? " selected" : "") + ">Yes</option>"; // �������� ��� ���������
                    html += "<option value='false'" + (isExpressValue == "false" ? " selected" : "") + ">No</option>"; // �������� ��� �����������
                    html += "</select><br/><br/>";

                    html += "<button type='submit'>Search</button>";
                    html += "</form>";

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

                    var html = "<h1>Route Search Results (Sessions)</h1>";
                    html += "<h3>Search Criteria:</h3>";
                    html += "<table border='1' style='border-collapse:collapse'>";
                    html += "<tr><th>Route Name</th><th>Transport Type</th><th>Is Express</th></tr>";

                    // ���������� ������ ������
                    foreach (var route in routes)
                    {
                        html += $"<tr><td>{route.Name}</td><td>{route.TransportType}</td><td>{route.IsExpress}</td></tr>";
                    }
                    html += "</table>";

                    if (routes.Count == 0)
                    {
                        html += "<p>No routes found matching the search criteria.</p>";
                    }

                    await context.Response.WriteAsync(html);
                }
            });





            await app.RunAsync();
        }
    }

    
}

