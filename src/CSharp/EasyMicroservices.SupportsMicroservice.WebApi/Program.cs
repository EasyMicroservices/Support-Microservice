using EasyMicroservices.SupportsMicroservice.Database;
using EasyMicroservices.SupportsMicroservice.Database.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using EasyMicroservices.SupportsMicroservice.Database.Entities;
using EasyMicroservices.SupportsMicroservice.Contracts;
using EasyMicroservices.SupportsMicroservice.Interfaces;
using EasyMicroservices.SupportsMicroservice.Database;
using EasyMicroservices.SupportsMicroservice.Interfaces;
using EasyMicroservices.SupportsMicroservice;
using EasyMicroservices.SupportsMicroservice.Contracts.Common;
using EasyMicroservices.SupportsMicroservice.Contracts.Requests;

namespace EasyMicroservices.SupportsMicroservice.WebApi
{
    public class Program
    {

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

            // Add services to the container.
            //builder.Services.AddAuthorization();
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SchemaFilter<GenericFilter>();
                options.SchemaFilter<XEnumNamesSchemaFilter>();
            });

            builder.Services.AddDbContext<SupportContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString(config.GetConnectionString("local")))
            );

            string webRootPath = @Directory.GetCurrentDirectory();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped((serviceProvider) => new DependencyManager().GetContractLogic<TicketEntity, TicketCreateRequestContract, TicketUpdateRequestContract, TicketContract>());
            builder.Services.AddScoped((serviceProvider) => new DependencyManager().GetContractLogic<TicketHistoryEntity, TicketHistoryCreateRequestContract, TicketHistoryUpdateRequestContract, TicketHistoryContract>());
            builder.Services.AddScoped((serviceProvider) => new DependencyManager().GetContractLogic<DepartmentEntity, DepartmentCreateRequestContract, DepartmentUpdateRequestContract, DepartmentContract>());
            builder.Services.AddScoped((serviceProvider) => new DependencyManager().GetContractLogic<TicketDepartmentEntity, TicketDepartmentCreateRequestContract, TicketDepartmentUpdateRequestContract, TicketDepartmentContract>());
            builder.Services.AddScoped((serviceProvider) => new DependencyManager().GetContractLogic<TicketSupportTimeHistoryEntity, TicketSupportTimeHistoryCreateRequestContract, TicketSupportTimeHistoryUpdateRequestContract, TicketSupportTimeHistoryContract>());
            builder.Services.AddScoped((serviceProvider) => new DependencyManager().GetContractLogic<TicketAssignEntity, TicketAssignCreateRequestContract, TicketAssignUpdateRequestContract, TicketAssignContract>());
            builder.Services.AddScoped<IDatabaseBuilder>(serviceProvider => new DatabaseBuilder());
   
            builder.Services.AddScoped<IDependencyManager>(service => new DependencyManager());
            builder.Services.AddScoped(service => new WhiteLabelManager(service, service.GetService<IDependencyManager>()));
            builder.Services.AddTransient(serviceProvider => new SupportContext(serviceProvider.GetService<IDatabaseBuilder>()));
            //builder.Services.AddScoped<IFileManagerProvider>(serviceProvider => new FileManagerProvider());
            //builder.Services.AddScoped<IDirectoryManagerProvider, kc>();

            //builder.Services.AddScoped<IDirectoryManagerProvider>(serviceProvider => new FileManager());
            //builder.Services.AddScoped<IFileManagerProvider>();

            var app = builder.Build();
            app.UseDeveloperExceptionPage();
            // Configure the HTTP request pipeline.
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();


            //CreateDatabase();

            using (var scope = app.Services.CreateScope())
            {
                using var context = scope.ServiceProvider.GetService<SupportContext>();
                //await context.Database.EnsureCreatedAsync();
                //await context.Database.MigrateAsync();
                await context.DisposeAsync();
                var service = scope.ServiceProvider.GetService<WhiteLabelManager>();
                await service.Initialize("Support", config.GetValue<string>("RootAddresses:WhiteLabel"), typeof(SupportContext));
            }

            StartUp startUp = new StartUp();
            await startUp.Run(new DependencyManager());
            app.Run();
        }
        
        static void CreateDatabase()
        {
            using (var context = new SupportContext(new DatabaseBuilder()))
            {
                if (context.Database.EnsureCreated())
                {
                    //auto migration when database created first time

                    //add migration history table

                    string createEFMigrationsHistoryCommand = $@"
USE [{context.Database.GetDbConnection().Database}];
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
CREATE TABLE [dbo].[__EFMigrationsHistory](
    [MigrationId] [nvarchar](150) NOT NULL,
    [ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
    [MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
";
                    context.Database.ExecuteSqlRaw(createEFMigrationsHistoryCommand);

                    //insert all of migrations
                    var dbAssebmly = context.GetType().Assembly;
                    foreach (var item in dbAssebmly.GetTypes())
                    {
                        if (item.BaseType == typeof(Migration))
                        {
                            string migrationName = item.GetCustomAttributes<MigrationAttribute>().First().Id;
                            var version = typeof(Migration).Assembly.GetName().Version;
                            string efVersion = $"{version.Major}.{version.Minor}.{version.Build}";
                            context.Database.ExecuteSqlRaw("INSERT INTO __EFMigrationsHistory(MigrationId,ProductVersion) VALUES ({0},{1})", migrationName, efVersion);
                        }
                    }
                }
                context.Database.Migrate();
            }
        }
    }

    public class GenericFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            var type = context.Type;

            if (type.IsGenericType == false)
                return;

            schema.Title = $"{type.Name[0..^2]}<{type.GenericTypeArguments[0].Name}>";
        }
    }

    public class XEnumNamesSchemaFilter : ISchemaFilter
    {
        private const string NAME = "x-enumNames";
        public void Apply(OpenApiSchema model, SchemaFilterContext context)
        {
            var typeInfo = context.Type;
            // Chances are something in the pipeline might generate this automatically at some point in the future
            // therefore it's best to check if it exists.
            if (typeInfo.IsEnum && !model.Extensions.ContainsKey(NAME))
            {
                var names = Enum.GetNames(context.Type);
                var arr = new OpenApiArray();
                arr.AddRange(names.Select(name => new OpenApiString(name)));
                model.Extensions.Add(NAME, arr);
            }
        }
    }
}